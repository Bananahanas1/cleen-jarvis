using NAudio.Wave;

namespace JarvisClean;

internal enum WakeWordStateV1
{
    Idle,
    Active
}

internal sealed class WakeWordListenerV1 : IDisposable
{
    // Audio config matchar VoiceCaptureV1: 16 kHz mono 16-bit PCM (Whisper-format).
    private const int SampleRate = 16000;
    private const int BitsPerSample = 16;
    private const int Channels = 1;
    private const int BytesPerSample = 2;

    // VAD-tröskel: RMS-amplitud för 16-bit PCM. ~600 är normalt tal, < 200 är tystnad.
    // Sänkt till 300 för att fånga även lite tystare yttranden.
    private const short VadThreshold = 300;

    // Whisper hör ofta dessa istället för "jarvis" / "hej jarvis" på svenska.
    // Accepterar dem som wake-aliases. Ordningen viktig — längre fraser först.
    private static readonly string[] WakeAliases = new[]
    {
        "hej jarvis", "hej hjärvis", "hej järvis", "hej joris",
        "jarvis", "joris", "charles", "jervis", "yarvis", "járvis", "garvis", "jarves", "darvis",
        "hjärvis", "hervis", "järvis", "herves", "hjarvis", "ärvis", "hjälpis", "harvis",
        "hjarvits", "hjärtvis", "kärvis", "jänsis"
    };

    private static readonly string[] DefaultStopWords = new[]
    {
        "vänta", "hejdå", "hej då", "hejda", "hej d&aring;"
    };

    // Tystnad efter tal innan vi anser segmentet slut.
    private const int SilenceEndMs = 800;

    // Max segmentlängd — om någon pratar i en evighet, klipp då vid 12 s så Whisper inte hänger.
    private const int MaxSegmentMs = 12000;

    // Min segmentlängd — undvik att skicka 100 ms-burst till Whisper (bara hallucinationer).
    private const int MinSegmentMs = 400;

    // Whisper-segment körs på dedikerad task — håll en kö så snabba yttranden inte tappas.
    private const int MaxPendingSegments = 4;

    public delegate Task UserSaidAsync(string transcript);
    public delegate Task WakeEventAsync();
    public delegate Task StopEventAsync();

    private readonly string _projectRoot;
    private readonly string _modelPath;
    private readonly string _language;
    private readonly VoiceConfigV1 _config;
    private readonly UserSaidAsync _onUserSaid;
    private readonly WakeEventAsync _onWake;
    private readonly StopEventAsync _onStop;

    private WaveInEvent? _mic;
    private MemoryStream? _segment;
    private bool _voiceActive;
    private DateTime _voiceStartedAtUtc = DateTime.MinValue;
    private DateTime _lastVoiceAtUtc = DateTime.MinValue;
    private WakeWordStateV1 _state = WakeWordStateV1.Idle;
    private int _pendingSegments;
    private bool _disposed;

    public WakeWordListenerV1(
        string projectRoot,
        string modelPath,
        VoiceConfigV1 config,
        UserSaidAsync onUserSaid,
        WakeEventAsync onWake,
        StopEventAsync onStop)
    {
        _projectRoot = projectRoot;
        _modelPath = modelPath;
        _language = string.IsNullOrWhiteSpace(config.Language) ? "sv" : config.Language;
        _config = config;
        _onUserSaid = onUserSaid;
        _onWake = onWake;
        _onStop = onStop;
    }

    public WakeWordStateV1 State => _state;

    public void Start(int deviceId)
    {
        if (_mic is not null) return;

        Log("Start deviceId=" + deviceId + " model=" + Path.GetFileName(_modelPath) + " wake='" + _config.WakeWord + "'");

        _mic = new WaveInEvent
        {
            DeviceNumber = deviceId < 0 ? 0 : deviceId,
            WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
            BufferMilliseconds = 100
        };
        _mic.DataAvailable += OnAudioFrame;
        _mic.RecordingStopped += (_, _) => Log("RecordingStopped");

        try
        {
            _mic.StartRecording();
            Log("StartRecording OK");
        }
        catch (Exception ex)
        {
            Log("StartRecording FAILED: " + ex.GetType().Name + ": " + ex.Message);
            try { _mic.Dispose(); } catch { }
            _mic = null;
            throw;
        }
    }

    public void Stop()
    {
        if (_mic is null) return;
        Log("Stop");
        try { _mic.StopRecording(); } catch { }
        try { _mic.Dispose(); } catch { }
        _mic = null;
        try { _segment?.Dispose(); } catch { }
        _segment = null;
        _voiceActive = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // Kort grace-period efter att Jarvis börjat tala — undviker att TTS-init plockas upp
    // som om det vore user-tal (feedback-loop med fri mic). Efter denna period accepterar
    // vi user-input för barge-in (avbryta Sofie mitt i meningen).
    private DateTime _ttsStartedAtUtc = DateTime.MinValue;
    private const int TtsBargeGraceMs = 400;

    private void OnAudioFrame(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded <= 0) return;

        // Barge-in: vi tillåter mic att fortsätta lyssna även under speaking,
        // men med kort grace-period direkt vid TTS-start för att undvika TTS-eko-loop.
        var speaking = VoiceStateV1.Snapshot.Mode == VoiceModeV1.Speaking;
        if (speaking)
        {
            if (_ttsStartedAtUtc == DateTime.MinValue) _ttsStartedAtUtc = DateTime.UtcNow;
            var sinceStart = (DateTime.UtcNow - _ttsStartedAtUtc).TotalMilliseconds;
            if (sinceStart < TtsBargeGraceMs)
            {
                // Grace-period: ignorera input (TTS startar precis och kan ge mic-spik).
                if (_voiceActive)
                {
                    _voiceActive = false;
                    try { _segment?.Dispose(); } catch { }
                    _segment = null;
                }
                return;
            }
        }
        else
        {
            _ttsStartedAtUtc = DateTime.MinValue;
        }

        var rms = ComputeRms(e.Buffer, e.BytesRecorded);
        var now = DateTime.UtcNow;
        var isVoice = rms >= VadThreshold;

        if (isVoice)
        {
            if (!_voiceActive)
            {
                _voiceActive = true;
                _voiceStartedAtUtc = now;
                _segment = new MemoryStream();
            }
            _lastVoiceAtUtc = now;
            try { _segment!.Write(e.Buffer, 0, e.BytesRecorded); } catch { }
        }
        else if (_voiceActive)
        {
            // Tystnad efter tal — fortsätt buffra en kort stund så slutljud (-stavelser) kommer med.
            try { _segment!.Write(e.Buffer, 0, e.BytesRecorded); } catch { }
            var silenceMs = (now - _lastVoiceAtUtc).TotalMilliseconds;
            var totalMs = (now - _voiceStartedAtUtc).TotalMilliseconds;

            if (silenceMs >= SilenceEndMs || totalMs >= MaxSegmentMs)
            {
                FinishSegment(totalMs);
            }
        }
        // else: helt tyst frame, ignorera.
    }

    private void FinishSegment(double totalMs)
    {
        var pcm = _segment?.ToArray() ?? Array.Empty<byte>();
        try { _segment?.Dispose(); } catch { }
        _segment = null;
        _voiceActive = false;

        if (totalMs < MinSegmentMs)
        {
            Log("segment dropped: too short (" + totalMs.ToString("0") + " ms)");
            return;
        }

        if (_pendingSegments >= MaxPendingSegments)
        {
            Log("segment dropped: queue full (" + _pendingSegments + " pending)");
            return;
        }

        Interlocked.Increment(ref _pendingSegments);
        _ = Task.Run(() => ProcessSegmentAsync(pcm, totalMs));
    }

    private async Task ProcessSegmentAsync(byte[] pcm, double totalMs)
    {
        try
        {
            Log("transcribe start: " + pcm.Length + " bytes (" + totalMs.ToString("0") + " ms) backend=" + (_config.SttBackend ?? "whisper"));
            VoiceSttResultV1 result;
            if (string.Equals(_config.SttBackend, "groq", StringComparison.OrdinalIgnoreCase))
            {
                result = await VoiceSttGroqV1.TranscribeAsync(pcm, _language, _projectRoot, CancellationToken.None);
            }
            else
            {
                result = await VoiceSttServiceV1.TranscribeAsync(pcm, _modelPath, _language);
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                Log("transcribe error: " + result.Error);
                return;
            }
            var transcript = (result.Transcript ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(transcript))
            {
                Log("transcribe empty");
                return;
            }
            Log("transcribe: state=" + _state + " text='" + transcript + "' (" + result.ElapsedMs + " ms)");

            await DispatchAsync(transcript);
        }
        catch (Exception ex)
        {
            Log("EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            Interlocked.Decrement(ref _pendingSegments);
        }
    }

    private async Task DispatchAsync(string transcript)
    {
        var lower = transcript.ToLowerInvariant();
        var stopWords = (_config.StopWords is { Count: > 0 } sw1) ? sw1.ToArray() : DefaultStopWords;

        // Bygg lista av wake-aliases: config-värdet + alla kända Whisper-misshörningar.
        var wakeCandidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(_config.WakeWord))
            wakeCandidates.Add(_config.WakeWord.ToLowerInvariant());
        foreach (var alias in WakeAliases)
            if (!wakeCandidates.Contains(alias)) wakeCandidates.Add(alias);

        if (_state == WakeWordStateV1.Idle)
        {
            var matched = wakeCandidates.FirstOrDefault(w => lower.Contains(w));
            if (matched is null)
            {
                Log("ignore (idle, no wake alias matched in: '" + transcript + "')");
                return;
            }

            _state = WakeWordStateV1.Active;
            Log("WAKE -> Active (matched '" + matched + "')");
            await _onWake();

            // Om användaren redan sa något efter wake-ordet i samma yttrande, processera resten direkt.
            var afterWake = StripFirstOccurrence(transcript, matched).Trim();
            if (!string.IsNullOrWhiteSpace(afterWake))
            {
                Log("immediate command after wake: '" + afterWake + "'");
                await _onUserSaid(afterWake);
            }
            return;
        }

        // Active-state: leta stop-ord
        foreach (var s in stopWords)
        {
            if (string.IsNullOrWhiteSpace(s)) continue;
            if (lower.Contains(s.ToLowerInvariant()))
            {
                _state = WakeWordStateV1.Idle;
                Log("STOP -> Idle (matched '" + s + "')");
                await _onStop();
                return;
            }
        }

        // Strip ev. wake-alias om det upprepas i mitten av samtalet.
        var cleaned = transcript;
        var hitAlias = wakeCandidates.FirstOrDefault(w => lower.Contains(w));
        if (hitAlias is not null)
        {
            cleaned = StripFirstOccurrence(transcript, hitAlias).Trim();
            if (string.IsNullOrWhiteSpace(cleaned)) cleaned = transcript;
        }

        // Barge-in: om Sofie just nu pratar — avbryt henne sa vi kan ge nytt svar baserat pa det user sa.
        if (VoiceStateV1.Snapshot.Mode == VoiceModeV1.Speaking)
        {
            Log("BARGE-IN: stopping TTS to handle new user input");
            try { VoiceTtsServiceV1.Stop(); } catch { }
            _ttsStartedAtUtc = DateTime.MinValue;
        }

        await _onUserSaid(cleaned);
    }

    private static string StripFirstOccurrence(string source, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return source;
        var idx = source.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return source;
        return source.Remove(idx, token.Length).Trim().TrimStart(',', ':', '.', '!', '?');
    }

    private static short ComputeRms(byte[] buffer, int byteCount)
    {
        if (byteCount < 2) return 0;
        long sumSquares = 0;
        int sampleCount = byteCount / BytesPerSample;
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(buffer[i * 2] | (buffer[i * 2 + 1] << 8));
            sumSquares += sample * sample;
        }
        var rms = Math.Sqrt(sumSquares / (double)sampleCount);
        if (rms > short.MaxValue) rms = short.MaxValue;
        return (short)rms;
    }

    private void Log(string message)
    {
        try
        {
            var dir = Path.Combine(_projectRoot, "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [wake] " + message + Environment.NewLine;
            File.AppendAllText(Path.Combine(dir, "stt-debug.log"), line);
        }
        catch { }
    }
}
