using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace JarvisClean;

internal static class VoiceSttGroqV1
{
    private const string Endpoint = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string ModelName = "whisper-large-v3-turbo";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static async Task<VoiceSttResultV1> TranscribeAsync(
        byte[] pcm16k,
        string language,
        string projectRoot,
        CancellationToken token)
    {
        Log(projectRoot, "groq start: pcm=" + (pcm16k?.Length ?? 0) + " bytes lang=" + language);

        if (pcm16k is null || pcm16k.Length == 0)
            return new VoiceSttResultV1("", 0, "Tom audio.");

        var groqSettings = GroqSettingsV1.Load(Path.Combine(projectRoot, "config"));
        var apiKey = groqSettings.ResolvedApiKey();
        var modelName = string.IsNullOrWhiteSpace(groqSettings.Model) ? ModelName : groqSettings.Model;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log(projectRoot, "FAIL: Groq API-key saknas (config/groq.json eller GROQ_API_KEY env-var)");
            return new VoiceSttResultV1("", 0,
                "Groq API-key saknas. Lagg in i config/groq.json (apiKey-falt) eller satt env-var GROQ_API_KEY.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // PCM → WAV (med RIFF-header) sa Groq kan parsa.
            byte[] wavBytes;
            using (var ms = new MemoryStream())
            {
                WriteWavHeader(ms, pcm16k.Length, 16000, 16, 1);
                ms.Write(pcm16k, 0, pcm16k.Length);
                wavBytes = ms.ToArray();
            }

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(wavBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", "audio.wav");
            form.Add(new StringContent(modelName), "model");
            form.Add(new StringContent(string.IsNullOrWhiteSpace(language) ? "sv" : language), "language");
            form.Add(new StringContent("json"), "response_format");
            form.Add(new StringContent("0"), "temperature");

            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = form;

            using var resp = await Http.SendAsync(req, token);
            var body = await resp.Content.ReadAsStringAsync(token);
            stopwatch.Stop();

            if (!resp.IsSuccessStatusCode)
            {
                var snip = body.Length > 240 ? body.Substring(0, 240) + "..." : body;
                Log(projectRoot, "groq HTTP " + (int)resp.StatusCode + " - " + snip);
                return new VoiceSttResultV1("", stopwatch.ElapsedMilliseconds,
                    "Groq HTTP " + (int)resp.StatusCode + ": " + snip);
            }

            using var json = JsonDocument.Parse(body);
            var text = "";
            if (json.RootElement.TryGetProperty("text", out var t)) text = t.GetString() ?? "";
            text = text.Trim();

            VoiceStateV1.RecordSttElapsed(stopwatch.ElapsedMilliseconds);
            Log(projectRoot, "groq ok: '" + text + "' (" + stopwatch.ElapsedMilliseconds + " ms)");

            return new VoiceSttResultV1(text, stopwatch.ElapsedMilliseconds,
                text.Length == 0 ? "Tystnad eller obegriplig audio." : "");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new VoiceSttResultV1("", stopwatch.ElapsedMilliseconds, "Avbruten.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log(projectRoot, "groq EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
            return new VoiceSttResultV1("", stopwatch.ElapsedMilliseconds, "Groq-fel: " + ex.Message);
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
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        writer.Write(dataSize);
    }

    private static void Log(string projectRoot, string message)
    {
        try
        {
            var dir = Path.Combine(projectRoot, "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "stt-debug.log"),
                "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [groq] " + message + Environment.NewLine);
        }
        catch { }
    }
}
