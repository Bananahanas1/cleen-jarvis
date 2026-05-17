using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace JarvisClean;

internal static class VoiceTtsElevenLabsV1
{
    private const string TtsEndpoint = "https://api.elevenlabs.io/v1/text-to-speech/";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    public static async Task SpeakAsync(
        string text,
        string projectRoot,
        ElevenLabsSettingsV1 settings,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!settings.IsReady)
        {
            Log(projectRoot, "skip: ElevenLabs settings not ready (apiKey/voiceId saknas)");
            return;
        }

        var voiceId = Uri.EscapeDataString(settings.VoiceId);
        var url = TtsEndpoint + voiceId + "/stream?optimize_streaming_latency=2&output_format=mp3_44100_128";

        var bodyJson = JsonSerializer.Serialize(new
        {
            text,
            model_id = settings.ModelId,
            voice_settings = new
            {
                stability = settings.Stability,
                similarity_boost = settings.SimilarityBoost,
                style = settings.Style,
                use_speaker_boost = settings.UseSpeakerBoost
            }
        });

        string? tempPath = null;
        try
        {
            Log(projectRoot, "elevenlabs request: chars=" + text.Length + " voice=" + settings.VoiceId + " model=" + settings.ModelId);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("xi-api-key", settings.ResolvedApiKey());
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));
            req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
            if (!resp.IsSuccessStatusCode)
            {
                var errBody = await resp.Content.ReadAsStringAsync(token);
                var snip = errBody.Length > 240 ? errBody.Substring(0, 240) + "..." : errBody;
                Log(projectRoot, "elevenlabs HTTP " + (int)resp.StatusCode + " — " + snip);
                return;
            }

            tempPath = Path.Combine(Path.GetTempPath(), "jarvis_tts_eleven_" + Guid.NewGuid().ToString("N") + ".mp3");
            await using (var fs = File.Create(tempPath))
            {
                await resp.Content.CopyToAsync(fs, token);
            }

            var size = new FileInfo(tempPath).Length;
            if (size < 200)
            {
                Log(projectRoot, "elevenlabs mp3 too small (" + size + " bytes) — skipping playback");
                return;
            }
            Log(projectRoot, "elevenlabs mp3 ok: " + size + " bytes — playing");

            await PlayMp3Async(tempPath, projectRoot, token);
            Log(projectRoot, "elevenlabs playback done");
        }
        catch (OperationCanceledException)
        {
            Log(projectRoot, "elevenlabs cancelled");
        }
        catch (Exception ex)
        {
            Log(projectRoot, "elevenlabs EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            if (tempPath is not null)
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }

    private static async Task PlayMp3Async(string mp3Path, string projectRoot, CancellationToken token)
    {
        // MediaFoundationReader stöder MP3 inbyggt via Windows Media Foundation.
        // Samma fallback-kedja som Piper-versionen: Wasapi → DirectSound → WaveOutEvent.
        MediaFoundationReader? reader = null;
        IWavePlayer? output = null;
        try
        {
            reader = new MediaFoundationReader(mp3Path);

            try
            {
                output = new WasapiOut(AudioClientShareMode.Shared, 100);
            }
            catch (Exception wasapiEx)
            {
                Log(projectRoot, "WasapiOut failed: " + wasapiEx.Message + " — trying DirectSoundOut");
                try
                {
                    output = new DirectSoundOut();
                }
                catch (Exception dsEx)
                {
                    Log(projectRoot, "DirectSoundOut failed: " + dsEx.Message + " — fallback WaveOutEvent");
                    output = new WaveOutEvent();
                }
            }

            output.Init(reader);
            output.Play();

            while (output.PlaybackState == PlaybackState.Playing && !token.IsCancellationRequested)
            {
                await Task.Delay(80, token);
            }
        }
        finally
        {
            try { output?.Stop(); } catch { }
            try { output?.Dispose(); } catch { }
            try { reader?.Dispose(); } catch { }
        }
    }

    private static void Log(string projectRoot, string message)
    {
        try
        {
            var dir = Path.Combine(projectRoot, "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "tts-debug.log");
            var line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [11labs] " + message + Environment.NewLine;
            File.AppendAllText(path, line);
        }
        catch { }
    }
}
