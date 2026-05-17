using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace JarvisClean;

internal static class VoiceTtsServiceV1
{
    private static readonly object Lock = new();
    private static CancellationTokenSource? _currentCts;
    private static Process? _currentPiper;
    private static IWavePlayer? _currentOutput;
    private static WaveFileReader? _currentReader;
    private static string? _currentTempPath;
    private static Task? _currentTask;

    public static void SpeakIfEnabled(string text, string projectRoot, VoiceConfigV1 config)
    {
        if (!VoiceStateV1.ShouldSpeak) { Log(projectRoot, "skip: ShouldSpeak=false"); return; }
        if (!config.TtsAutoSpeak) { Log(projectRoot, "skip: TtsAutoSpeak=false"); return; }
        if (string.IsNullOrWhiteSpace(text)) { Log(projectRoot, "skip: empty text"); return; }

        Speak(text, projectRoot, config);
    }

    public static void Speak(string text, string projectRoot, VoiceConfigV1 config)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        // Sanera text innan TTS — tar bort URLs, JSON, emoji, markdown osv. sa Sofie
        // inte laser bokstavligt "asterisk hash" eller "skraestrek skraestrek".
        text = TtsTextSanitizerV1.Sanitize(text);
        if (string.IsNullOrWhiteSpace(text)) return;

        // Routing: backend "elevenlabs" -> REST + MP3 via VoiceTtsElevenLabsV1.
        //          backend "edge"       -> Microsoft Edge TTS via WebSocket (native svenska).
        // Default backend "piper" (eller okand) -> existerande lokala Piper-flode.
        if (string.Equals(config.TtsBackend, "elevenlabs", StringComparison.OrdinalIgnoreCase))
        {
            SpeakElevenLabs(text, projectRoot);
            return;
        }

        if (string.Equals(config.TtsBackend, "edge", StringComparison.OrdinalIgnoreCase))
        {
            SpeakEdge(text, projectRoot, config);
            return;
        }

        if (string.Equals(config.TtsBackend, "sapi", StringComparison.OrdinalIgnoreCase))
        {
            SpeakSapi(text, projectRoot, config);
            return;
        }

        var report = VoiceAssetManagerV1.BuildReport(projectRoot, config);
        if (!report.TtsReady)
        {
            Log(projectRoot, "skip: TtsReady=false (assets saknas)");
            return;
        }

        CancellationTokenSource ctsLocal;
        lock (Lock)
        {
            StopInternal();
            ctsLocal = new CancellationTokenSource();
            _currentCts = ctsLocal;
        }

        VoiceStateV1.TransitionTo(VoiceModeV1.Speaking);

        _currentTask = Task.Run(() => RunPiperAndPlayAsync(text, report, projectRoot, ctsLocal.Token), ctsLocal.Token);
    }

    private static void SpeakSapi(string text, string projectRoot, VoiceConfigV1 config)
    {
        CancellationTokenSource ctsLocal;
        lock (Lock)
        {
            StopInternal();
            ctsLocal = new CancellationTokenSource();
            _currentCts = ctsLocal;
        }

        VoiceStateV1.TransitionTo(VoiceModeV1.Speaking);

        _currentTask = Task.Run(async () =>
        {
            try
            {
                await VoiceTtsSapiV1.SpeakAsync(text, projectRoot, config.SapiVoice, ctsLocal.Token);
            }
            finally
            {
                if (VoiceStateV1.IsEnabled)
                    VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
            }
        }, ctsLocal.Token);
    }

    private static void SpeakEdge(string text, string projectRoot, VoiceConfigV1 config)
    {
        CancellationTokenSource ctsLocal;
        lock (Lock)
        {
            StopInternal();
            ctsLocal = new CancellationTokenSource();
            _currentCts = ctsLocal;
        }

        VoiceStateV1.TransitionTo(VoiceModeV1.Speaking);

        _currentTask = Task.Run(async () =>
        {
            try
            {
                await VoiceTtsEdgeV1.SpeakAsync(text, projectRoot, config.EdgeVoice, ctsLocal.Token);
            }
            finally
            {
                if (VoiceStateV1.IsEnabled)
                    VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
            }
        }, ctsLocal.Token);
    }

    private static void SpeakElevenLabs(string text, string projectRoot)
    {
        var settings = ElevenLabsSettingsV1.Load(Path.Combine(projectRoot, "config"));
        if (!settings.IsReady)
        {
            Log(projectRoot, "skip: elevenlabs settings not ready (apiKey/voiceId saknas i config/elevenlabs.json eller ELEVENLABS_API_KEY env var)");
            return;
        }

        CancellationTokenSource ctsLocal;
        lock (Lock)
        {
            StopInternal();
            ctsLocal = new CancellationTokenSource();
            _currentCts = ctsLocal;
        }

        VoiceStateV1.TransitionTo(VoiceModeV1.Speaking);

        _currentTask = Task.Run(async () =>
        {
            try
            {
                await VoiceTtsElevenLabsV1.SpeakAsync(text, projectRoot, settings, ctsLocal.Token);
            }
            finally
            {
                if (VoiceStateV1.IsEnabled)
                    VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
            }
        }, ctsLocal.Token);
    }

    public static void Stop()
    {
        lock (Lock)
            StopInternal();
        if (VoiceStateV1.IsEnabled)
            VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
    }

    private static void StopInternal()
    {
        try { _currentCts?.Cancel(); } catch { }

        try { _currentOutput?.Stop(); } catch { }
        try { _currentOutput?.Dispose(); } catch { }
        _currentOutput = null;

        try { _currentReader?.Dispose(); } catch { }
        _currentReader = null;

        if (_currentPiper is not null)
        {
            try
            {
                if (!_currentPiper.HasExited)
                    _currentPiper.Kill(entireProcessTree: true);
            }
            catch { }
            try { _currentPiper.Dispose(); } catch { }
            _currentPiper = null;
        }

        if (_currentTempPath is not null)
        {
            try { if (File.Exists(_currentTempPath)) File.Delete(_currentTempPath); } catch { }
            _currentTempPath = null;
        }

        try { _currentCts?.Dispose(); } catch { }
        _currentCts = null;
    }

    private static async Task RunPiperAndPlayAsync(string text, VoiceAssetReportV1 report, string projectRoot, CancellationToken token)
    {
        Process? piper = null;
        IWavePlayer? output = null;
        WaveFileReader? reader = null;
        string tempPath = Path.Combine(Path.GetTempPath(), "jarvis_tts_" + Guid.NewGuid().ToString("N") + ".wav");

        try
        {
            Log(projectRoot, "speak start: chars=" + text.Length + " voice=" + Path.GetFileNameWithoutExtension(report.PiperVoicePath));

            piper = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = report.PiperExecutablePath,
                    Arguments = "--model \"" + report.PiperVoicePath + "\" --output_file \"" + tempPath + "\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(report.PiperExecutablePath) ?? Environment.CurrentDirectory
                },
                EnableRaisingEvents = false
            };

            if (!piper.Start())
            {
                Log(projectRoot, "piper.Start returned false");
                return;
            }

            lock (Lock)
            {
                _currentPiper = piper;
                _currentTempPath = tempPath;
            }

            await piper.StandardInput.WriteAsync(text);
            piper.StandardInput.Close();

            // Konsumera stdout + stderr så processen inte blockerar på fulla pipes.
            var stderrTask = piper.StandardError.ReadToEndAsync();
            var stdoutTask = piper.StandardOutput.ReadToEndAsync();

            await piper.WaitForExitAsync(token);
            var stderr = await stderrTask;
            await stdoutTask;

            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length < 100)
            {
                Log(projectRoot, "piper generated no audio. exit=" + piper.ExitCode + " stderr=" + (stderr ?? "").Replace("\n", " | "));
                return;
            }

            Log(projectRoot, "piper wav ok: " + new FileInfo(tempPath).Length + " bytes, exit=" + piper.ExitCode);

            reader = new WaveFileReader(tempPath);

            // WasapiOut (modern Windows Audio Session API) löser BadDeviceId-felet med WaveOutEvent.
            // Fallback till DirectSoundOut → WaveOutEvent om Wasapi inte tillgängligt.
            try
            {
                output = new WasapiOut(AudioClientShareMode.Shared, 100);
                Log(projectRoot, "using WasapiOut");
            }
            catch (Exception wasapiEx)
            {
                Log(projectRoot, "WasapiOut failed: " + wasapiEx.Message + " — trying DirectSoundOut");
                try
                {
                    output = new DirectSoundOut();
                    Log(projectRoot, "using DirectSoundOut");
                }
                catch (Exception dsEx)
                {
                    Log(projectRoot, "DirectSoundOut failed: " + dsEx.Message + " — falling back to WaveOutEvent");
                    output = new WaveOutEvent();
                    Log(projectRoot, "using WaveOutEvent (fallback)");
                }
            }

            output.Init(reader);
            output.Play();

            lock (Lock)
            {
                _currentReader = reader;
                _currentOutput = output;
            }

            while (output.PlaybackState == PlaybackState.Playing && !token.IsCancellationRequested)
                await Task.Delay(80, token);

            Log(projectRoot, "playback done");
        }
        catch (OperationCanceledException)
        {
            Log(projectRoot, "cancelled");
        }
        catch (Exception ex)
        {
            Log(projectRoot, "EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            try { output?.Stop(); } catch { }
            try { output?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
            try
            {
                if (piper is not null && !piper.HasExited)
                    piper.Kill(entireProcessTree: true);
            }
            catch { }
            try { piper?.Dispose(); } catch { }
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }

            lock (Lock)
            {
                if (ReferenceEquals(_currentOutput, output)) _currentOutput = null;
                if (ReferenceEquals(_currentReader, reader)) _currentReader = null;
                if (ReferenceEquals(_currentPiper, piper)) _currentPiper = null;
                if (ReferenceEquals(_currentTempPath, tempPath)) _currentTempPath = null;
            }

            if (VoiceStateV1.IsEnabled)
                VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
        }
    }

    private static void Log(string projectRoot, string message)
    {
        try
        {
            var dir = Path.Combine(projectRoot, "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "tts-debug.log");
            var line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message + Environment.NewLine;
            File.AppendAllText(path, line);
        }
        catch { }
    }
}
