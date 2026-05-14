using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace JarvisClean;

internal static class VoiceCaptureV1
{
    public const int CaptureSampleRate = 16000;
    public const int CaptureBitsPerSample = 16;
    public const int CaptureChannels = 1;

    private static readonly object Lock = new();
    private static WaveInEvent? _currentInput;
    private static MemoryStream? _currentBuffer;
    private static bool _writingActive;
    private static TaskCompletionSource? _stopTcs;

    public static bool IsRecording
    {
        get
        {
            lock (Lock)
                return _currentInput is not null && _writingActive;
        }
    }

    private static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(@"F:\Jarvis-clean", "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "capture-debug.log"),
                "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + message + Environment.NewLine);
        }
        catch { }
    }

    public static void StartRecording(int deviceId)
    {
        Log("StartRecording deviceId=" + deviceId + " (default if -1)");
        WaveInEvent? newInput = null;
        MemoryStream? newMemory = null;

        lock (Lock)
        {
            StopInternalNoLock();

            newMemory = new MemoryStream();
            var stopTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            newInput = new WaveInEvent
            {
                DeviceNumber = deviceId < 0 ? 0 : deviceId,
                WaveFormat = new WaveFormat(CaptureSampleRate, CaptureBitsPerSample, CaptureChannels),
                BufferMilliseconds = 50
            };

            // Capture lokala referenser så closures inte ser nullade fält.
            var memoryRef = newMemory;
            newInput.DataAvailable += (_, e) =>
            {
                lock (Lock)
                {
                    if (!_writingActive) return;
                    if (!ReferenceEquals(_currentBuffer, memoryRef)) return;
                    try { memoryRef.Write(e.Buffer, 0, e.BytesRecorded); } catch { }
                }
            };

            newInput.RecordingStopped += (_, _) =>
            {
                try { stopTcs.TrySetResult(); } catch { }
            };

            try
            {
                newInput.StartRecording();
                Log("WaveInEvent.StartRecording OK");
            }
            catch (Exception ex)
            {
                Log("StartRecording FAILED: " + ex.GetType().Name + ": " + ex.Message);
                try { newInput.Dispose(); } catch { }
                try { newMemory.Dispose(); } catch { }
                throw;
            }

            _currentInput = newInput;
            _currentBuffer = newMemory;
            _stopTcs = stopTcs;
            _writingActive = true;
        }

        VoiceStateV1.TransitionTo(VoiceModeV1.Listening);
    }

    public static async Task<byte[]> StopAndGetPcmAsync()
    {
        WaveInEvent? input;
        MemoryStream? memory;
        TaskCompletionSource? stopTcs;

        lock (Lock)
        {
            input = _currentInput;
            memory = _currentBuffer;
            stopTcs = _stopTcs;
            _writingActive = false; // sluta acceptera nya samples direkt
        }

        if (input is null || memory is null)
            return Array.Empty<byte>();

        try { input.StopRecording(); } catch { }

        // Vänta på native callback chain att stänga ner ordentligt (max 2s).
        if (stopTcs is not null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await stopTcs.Task.WaitAsync(cts.Token);
            }
            catch { }
        }

        byte[] pcm;
        lock (Lock)
        {
            try { pcm = memory.ToArray(); } catch { pcm = Array.Empty<byte>(); }
            try { memory.Dispose(); } catch { }
            try { input.Dispose(); } catch { }
            if (ReferenceEquals(_currentInput, input)) _currentInput = null;
            if (ReferenceEquals(_currentBuffer, memory)) _currentBuffer = null;
            if (ReferenceEquals(_stopTcs, stopTcs)) _stopTcs = null;
        }

        Log("StopAndGetPcmAsync: PCM bytes=" + pcm.Length + " (" + (pcm.Length / 32000.0).ToString("0.0") + " s @ 16kHz)");
        return pcm;
    }

    public static byte[] StopAndGetPcm()
    {
        // Bakåtkompat: synkron wrapper. Föredra StopAndGetPcmAsync.
        return StopAndGetPcmAsync().GetAwaiter().GetResult();
    }

    public static void Cancel()
    {
        lock (Lock)
            StopInternalNoLock();
        if (VoiceStateV1.IsEnabled)
            VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
    }

    private static void StopInternalNoLock()
    {
        _writingActive = false;
        if (_currentInput is not null)
        {
            try { _currentInput.StopRecording(); } catch { }
            try { _currentInput.Dispose(); } catch { }
            _currentInput = null;
        }
        if (_currentBuffer is not null)
        {
            try { _currentBuffer.Dispose(); } catch { }
            _currentBuffer = null;
        }
        if (_stopTcs is not null)
        {
            try { _stopTcs.TrySetResult(); } catch { }
            _stopTcs = null;
        }
    }

    public static async Task<string> RunMicTestAsync(int deviceId, int seconds)
    {
        if (seconds < 1) seconds = 2;
        if (seconds > 10) seconds = 10;

        try
        {
            StartRecording(deviceId);
        }
        catch (Exception ex)
        {
            return "Mikrofontest misslyckades vid start: " + ex.Message;
        }

        await Task.Delay(TimeSpan.FromSeconds(seconds));

        var pcm = await StopAndGetPcmAsync();
        if (pcm.Length == 0)
            return "Mikrofontest: ingen audio togs emot. Kontrollera mikrofonen och Windows-permissions.";

        var bytesPerSecond = (long)CaptureSampleRate * (CaptureBitsPerSample / 8) * CaptureChannels;
        var seconds16k = pcm.Length / (double)bytesPerSecond;

        try
        {
            using var memory = new MemoryStream(pcm);
            using var provider = new RawSourceWaveStream(memory, new WaveFormat(CaptureSampleRate, CaptureBitsPerSample, CaptureChannels));
            IWavePlayer? output = null;
            try { output = new WasapiOut(AudioClientShareMode.Shared, 100); Log("mic-test using WasapiOut"); }
            catch (Exception wasapiEx) { Log("mic-test WasapiOut failed: " + wasapiEx.Message); try { output = new DirectSoundOut(); Log("mic-test using DirectSoundOut"); } catch { output = new WaveOutEvent(); Log("mic-test fallback WaveOutEvent"); } }

            using (output)
            {
                output.Init(provider);
                output.Play();
                while (output.PlaybackState == PlaybackState.Playing)
                    await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            Log("mic-test playback FAILED: " + ex.GetType().Name + ": " + ex.Message);
            return "Mikrofontest spelade in " + seconds16k.ToString("0.0") + " s men kunde inte spela upp: " + ex.Message;
        }

        if (VoiceStateV1.IsEnabled)
            VoiceStateV1.TransitionTo(VoiceModeV1.Idle);

        return "Mikrofontest klart: " + seconds16k.ToString("0.0") + " sekunder inspelade och uppspelade. " +
               "Du hörde din egen röst → mikrofon + uppspelning fungerar.";
    }
}
