using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace JarvisClean;

internal static class VoiceTtsEdgeV1
{
    // Trusted client token är samma som Microsoft Edge skickar för "Read aloud".
    // Endpoint ar public — ingen API-key kravs.
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";
    private const string EndpointFmt =
        "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken={0}";

    public static async Task SpeakAsync(
        string text,
        string projectRoot,
        string voiceName,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (string.IsNullOrWhiteSpace(voiceName)) voiceName = "sv-SE-MattiasNeural";

        var connectionId = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var gec = CalculateSecMsGec(projectRoot);
        // Försöker med en nyare Edge build-version. Om Microsoft kollar exakt match kan detta misslyckas.
        const string edgeVersion = "1-132.0.2957.140";
        var endpoint = string.Format(EndpointFmt, TrustedClientToken) +
                       "&Sec-MS-GEC=" + gec +
                       "&Sec-MS-GEC-Version=" + edgeVersion +
                       "&ConnectionId=" + connectionId;

        string? tempPath = null;
        try
        {
            Log(projectRoot, "edge request: chars=" + text.Length + " voice=" + voiceName + " gec=" + gec.Substring(0, 8) + "... ver=" + edgeVersion);

            using var ws = new ClientWebSocket();
            ws.Options.SetRequestHeader("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36 Edg/132.0.2957.140");
            ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
            ws.Options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
            ws.Options.SetRequestHeader("Accept-Language", "sv-SE,sv;q=0.9,en;q=0.8");
            ws.Options.SetRequestHeader("Pragma", "no-cache");
            ws.Options.SetRequestHeader("Cache-Control", "no-cache");
            ws.Options.SetRequestHeader("Sec-Fetch-Dest", "empty");
            ws.Options.SetRequestHeader("Sec-Fetch-Mode", "websocket");
            ws.Options.SetRequestHeader("Sec-Fetch-Site", "cross-site");

            await ws.ConnectAsync(new Uri(endpoint), token);

            // 1. Skicka audio-config (text-frame).
            var configFrame =
                "X-Timestamp:" + DateTime.UtcNow.ToString("o") + "\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:speech.config\r\n\r\n" +
                "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":" +
                "{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"}," +
                "\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
            await ws.SendAsync(Encoding.UTF8.GetBytes(configFrame), WebSocketMessageType.Text, true, token);

            // 2. Skicka SSML (text-frame).
            var ssml = BuildSsml(voiceName, text);
            var ssmlFrame =
                "X-RequestId:" + connectionId + "\r\n" +
                "Content-Type:application/ssml+xml\r\n" +
                "X-Timestamp:" + DateTime.UtcNow.ToString("o") + "\r\n" +
                "Path:ssml\r\n\r\n" + ssml;
            await ws.SendAsync(Encoding.UTF8.GetBytes(ssmlFrame), WebSocketMessageType.Text, true, token);

            // 3. Ta emot frames tills turn.end.
            using var audio = new MemoryStream();
            var recv = new byte[8192];
            var done = false;
            while (!done && ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                using var frameStream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(recv), token);
                    if (result.Count > 0) frameStream.Write(recv, 0, result.Count);
                } while (!result.EndOfMessage && result.MessageType != WebSocketMessageType.Close);

                if (result.MessageType == WebSocketMessageType.Close) break;

                var frame = frameStream.ToArray();
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Binary frame: 2 bytes header-length (big-endian) + header text + audio bytes.
                    if (frame.Length > 2)
                    {
                        int headerLen = (frame[0] << 8) | frame[1];
                        int audioStart = 2 + headerLen;
                        if (audioStart < frame.Length)
                            audio.Write(frame, audioStart, frame.Length - audioStart);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(frame);
                    if (msg.Contains("Path:turn.end", StringComparison.Ordinal)) done = true;
                }
            }

            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", token); } catch { }

            if (audio.Length < 200)
            {
                Log(projectRoot, "edge produced no audio (" + audio.Length + " bytes)");
                return;
            }

            tempPath = Path.Combine(Path.GetTempPath(), "jarvis_tts_edge_" + Guid.NewGuid().ToString("N") + ".mp3");
            await File.WriteAllBytesAsync(tempPath, audio.ToArray(), token);
            Log(projectRoot, "edge mp3 ok: " + audio.Length + " bytes — playing");

            await PlayMp3Async(tempPath, projectRoot, token);
            Log(projectRoot, "edge playback done");
        }
        catch (OperationCanceledException)
        {
            Log(projectRoot, "edge cancelled");
        }
        catch (Exception ex)
        {
            Log(projectRoot, "edge EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            if (tempPath is not null)
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
    }

    private static string CalculateSecMsGec(string projectRoot)
    {
        // Algoritm: (UnixSeconds + 11644473600) * 1e7, rundad ner till 5-min boundary.
        // Sen SHA256(ticks + TrustedClientToken).ToUpperHex.
        var nowUtc = DateTimeOffset.UtcNow;
        var unixSeconds = nowUtc.ToUnixTimeSeconds();
        var winTicks = (unixSeconds + 11644473600L) * 10_000_000L;
        winTicks -= winTicks % 3_000_000_000L;
        var raw = winTicks.ToString() + TrustedClientToken;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hex = Convert.ToHexString(hash);
        try
        {
            var dir = Path.Combine(projectRoot, "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "tts-debug.log"),
                "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [edge] gec calc: utcNow=" + nowUtc.ToString("o") + " unixSec=" + unixSeconds + " winTicks=" + winTicks + " gecPrefix=" + hex.Substring(0, 16) + Environment.NewLine);
        }
        catch { }
        return hex;
    }

    private static string BuildSsml(string voiceName, string text)
    {
        var lang = "sv-SE";
        if (voiceName.StartsWith("en-", StringComparison.OrdinalIgnoreCase)) lang = "en-US";
        var escaped = System.Net.WebUtility.HtmlEncode(text);
        return
            "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='" + lang + "'>" +
            "<voice name='" + voiceName + "'>" +
            "<prosody pitch='+0Hz' rate='+0%' volume='+0%'>" + escaped + "</prosody>" +
            "</voice></speak>";
    }

    private static async Task PlayMp3Async(string mp3Path, string projectRoot, CancellationToken token)
    {
        MediaFoundationReader? reader = null;
        IWavePlayer? output = null;
        try
        {
            reader = new MediaFoundationReader(mp3Path);
            try { output = new WasapiOut(AudioClientShareMode.Shared, 100); }
            catch (Exception wasapiEx)
            {
                Log(projectRoot, "WasapiOut failed: " + wasapiEx.Message + " — trying DirectSoundOut");
                try { output = new DirectSoundOut(); }
                catch { output = new WaveOutEvent(); }
            }
            output.Init(reader);
            output.Play();
            while (output.PlaybackState == PlaybackState.Playing && !token.IsCancellationRequested)
                await Task.Delay(80, token);
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
            var line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [edge] " + message + Environment.NewLine;
            File.AppendAllText(path, line);
        }
        catch { }
    }
}
