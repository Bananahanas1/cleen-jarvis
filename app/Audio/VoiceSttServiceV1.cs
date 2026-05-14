using System.Diagnostics;
using Whisper.net;

namespace JarvisClean;

internal static class VoiceSttServiceV1
{
    private static readonly object Lock = new();
    private static WhisperFactory? _factory;
    private static string _loadedModelPath = "";

    private static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(@"F:\Jarvis-clean", "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "stt-debug.log"),
                "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + message + Environment.NewLine);
        }
        catch { }
    }

    public static async Task<VoiceSttResultV1> TranscribeAsync(byte[] pcm16k, string modelPath, string language)
    {
        Log("TranscribeAsync start: pcm=" + (pcm16k?.Length ?? 0) + " bytes, model=" + Path.GetFileName(modelPath ?? "?") + ", lang=" + language);

        if (pcm16k is null || pcm16k.Length == 0)
        {
            Log("FAIL: pcm is null or empty");
            return new VoiceSttResultV1("", 0, "Inget audio.");
        }

        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            Log("FAIL: modelPath missing: " + modelPath);
            return new VoiceSttResultV1("", 0, "STT-modell saknas: " + modelPath);
        }

        WhisperFactory factory;
        try
        {
            factory = GetOrLoadFactory(modelPath);
            Log("WhisperFactory loaded ok");
        }
        catch (Exception ex)
        {
            Log("FAIL: WhisperFactory load: " + ex.GetType().Name + ": " + ex.Message);
            return new VoiceSttResultV1("", 0, "Kunde inte ladda STT-modell: " + ex.Message);
        }

        var lang = string.IsNullOrWhiteSpace(language) ? "sv" : language;
        var stopwatch = Stopwatch.StartNew();
        var transcript = string.Empty;
        int segments = 0;

        try
        {
            // Initial prompt = "primer" som ger Whisper kontext om vad användaren troligen säger.
            // Detta minskar hallucinationer ("Tack för nu!", "Bye bye!") signifikant.
            const string initialPrompt =
                "Jarvis. Hej Jarvis, vad gör du. Visa terminal. Öppna fil. Stäng. " +
                "Skapa task. Indexera projekt. Status. Hjälp. Avbryt. " +
                "Aktivera röst. Stäng av röst. Mick test. Tysta dig. Prata igen.";

            await using var processor = factory.CreateBuilder()
                .WithLanguage(lang)
                .WithPrompt(initialPrompt)
                .Build();

            using var wavStream = new MemoryStream();
            WriteWavHeader(wavStream, pcm16k.Length, VoiceCaptureV1.CaptureSampleRate, VoiceCaptureV1.CaptureBitsPerSample, VoiceCaptureV1.CaptureChannels);
            wavStream.Write(pcm16k, 0, pcm16k.Length);
            wavStream.Position = 0;

            await foreach (var segment in processor.ProcessAsync(wavStream))
            {
                segments++;
                if (!string.IsNullOrWhiteSpace(segment.Text))
                    transcript += segment.Text;
            }

            transcript = transcript.Trim();
            Log("ProcessAsync done: segments=" + segments + ", transcript='" + transcript + "', elapsed=" + stopwatch.ElapsedMilliseconds + "ms");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log("FAIL: ProcessAsync EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
            return new VoiceSttResultV1("", stopwatch.ElapsedMilliseconds, "STT-fel: " + ex.Message);
        }

        stopwatch.Stop();
        VoiceStateV1.RecordSttElapsed(stopwatch.ElapsedMilliseconds);

        // Filtrera bort kända Whisper-hallucinationer som dyker upp när audio är tyst/kort/dålig.
        // Dessa är populära fraser i träningsdatat som modellen "ser" trots att de inte sagts.
        if (IsLikelyHallucination(transcript))
        {
            Log("transcript filtered as hallucination: '" + transcript + "'");
            return new VoiceSttResultV1("", stopwatch.ElapsedMilliseconds, "Whisper-hallucination filtrerad. Tala tydligare och längre.");
        }

        return new VoiceSttResultV1(transcript, stopwatch.ElapsedMilliseconds, transcript.Length == 0 ? "Tystnad eller obegriplig audio." : "");
    }

    private static readonly HashSet<string> KnownHallucinations = new(StringComparer.OrdinalIgnoreCase)
    {
        "tack för nu", "tack för att du tittade", "tack för att ni tittade",
        "tack så mycket", "tack så jättemycket", "tack för titten",
        "hej då", "hej hej", "bye bye", "bye", "thank you", "thanks",
        "tack", "ja", "nej", "ok", "okej",
        "undertexter av", "undertexter:", "översättning av",
        "you", ".", "...", "*",
        // Engelska Whisper-fallbacks även på sv
        "thanks for watching", "bye", "thank you for watching",
        // "Joris" är en vanlig Whisper-misshörning av "Jarvis"
    };

    private static bool IsLikelyHallucination(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return false;
        var cleaned = transcript.Trim().TrimEnd('.', '!', '?', ',').Trim().ToLowerInvariant();
        if (KnownHallucinations.Contains(cleaned)) return true;
        // Mycket korta transkript (< 4 tecken) på audio som var längre än 0.5 sek är ofta hallucinationer.
        return false;
    }

    private static WhisperFactory GetOrLoadFactory(string modelPath)
    {
        lock (Lock)
        {
            if (_factory is not null && _loadedModelPath == modelPath)
                return _factory;

            try { _factory?.Dispose(); } catch { }
            _factory = WhisperFactory.FromPath(modelPath);
            _loadedModelPath = modelPath;
            return _factory;
        }
    }

    private static void WriteWavHeader(Stream stream, int dataSize, int sampleRate, int bitsPerSample, int channels)
    {
        var blockAlign = channels * (bitsPerSample / 8);
        var byteRate = sampleRate * blockAlign;
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        writer.Write(36 + dataSize);
        writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
        writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        writer.Write(dataSize);
    }
}

internal sealed record VoiceSttResultV1(string Transcript, long ElapsedMs, string Error);
