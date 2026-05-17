using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace JarvisClean;

[SupportedOSPlatform("windows")]
internal static class VoiceTtsSapiV1
{
    // SAPI via COM (SAPI.SpVoice). Krever inga NuGet-deps — bara Windows.
    // Samma underliggande motor som System.Speech.Synthesis wrappar.
    private const int SVSFDefault = 0; // synkront tal

    public static Task SpeakAsync(string text, string projectRoot, string preferredVoice, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(text)) return Task.CompletedTask;

        return Task.Run(() =>
        {
            object? sapi = null;
            try
            {
                Log(projectRoot, "sapi request: chars=" + text.Length + " preferred='" + preferredVoice + "'");

                var sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (sapiType is null)
                {
                    Log(projectRoot, "sapi: SAPI.SpVoice ProgID not found");
                    return;
                }
                sapi = Activator.CreateInstance(sapiType);
                if (sapi is null)
                {
                    Log(projectRoot, "sapi: Activator.CreateInstance returned null");
                    return;
                }

                TrySelectVoice(sapi, preferredVoice, projectRoot);

                using var reg = token.Register(() =>
                {
                    try { sapi.GetType().InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, sapi, new object[] { "", 2 }); } catch { }
                });

                sapi.GetType().InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, sapi, new object[] { text, SVSFDefault });
                Log(projectRoot, "sapi: speak done");
            }
            catch (Exception ex)
            {
                Log(projectRoot, "sapi EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (sapi is not null) try { Marshal.ReleaseComObject(sapi); } catch { }
            }
        }, token);
    }

    private static void TrySelectVoice(object sapi, string preferredVoice, string projectRoot)
    {
        try
        {
            var voices = sapi.GetType().InvokeMember("GetVoices", System.Reflection.BindingFlags.InvokeMethod, null, sapi, null);
            if (voices is null) return;
            int count = (int)voices.GetType().InvokeMember("Count", System.Reflection.BindingFlags.GetProperty, null, voices, null)!;
            object? swedishFallback = null;
            object? preferredMatch = null;

            // Logga ALLA installerade roster sa vi vet vad SAPI ser.
            Log(projectRoot, "sapi: scanning " + count + " installed voices");
            for (int i = 0; i < count; i++)
            {
                var voice = voices.GetType().InvokeMember("Item", System.Reflection.BindingFlags.InvokeMethod, null, voices, new object[] { i });
                if (voice is null) continue;

                var nameObj = voice.GetType().InvokeMember("GetAttribute", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { "Name" });
                string lang = "?";
                try
                {
                    var langObj = voice.GetType().InvokeMember("GetAttribute", System.Reflection.BindingFlags.InvokeMethod, null, voice, new object[] { "Language" });
                    lang = langObj as string ?? "?";
                }
                catch { }
                var name = nameObj as string ?? "";

                Log(projectRoot, "  voice[" + i + "]: name='" + name + "' lang='" + lang + "'");

                if (preferredMatch is null && !string.IsNullOrWhiteSpace(preferredVoice) && name.Contains(preferredVoice, StringComparison.OrdinalIgnoreCase))
                {
                    preferredMatch = voice;
                }

                // Bengt / Hedvig / Mattias / Sofie / Erik / Karl = kanda svenska SAPI-roster.
                // Language code: 41D (1037) eller 40D (1101) eller fragment.
                if (swedishFallback is null && (
                    name.Contains("Bengt", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Hedvig", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Mattias", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Sofie", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Erik", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Karl", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Swedish", StringComparison.OrdinalIgnoreCase) ||
                    lang.Contains("41d", StringComparison.OrdinalIgnoreCase) ||
                    lang.Contains("40d", StringComparison.OrdinalIgnoreCase) ||
                    lang.Equals("1037", StringComparison.OrdinalIgnoreCase) ||
                    lang.Equals("1101", StringComparison.OrdinalIgnoreCase)))
                {
                    swedishFallback = voice;
                }
            }

            var pick = preferredMatch ?? swedishFallback;
            if (pick is not null)
            {
                var nameObj = pick.GetType().InvokeMember("GetAttribute", System.Reflection.BindingFlags.InvokeMethod, null, pick, new object[] { "Name" });
                sapi.GetType().InvokeMember("Voice", System.Reflection.BindingFlags.SetProperty, null, sapi, new object[] { pick });
                Log(projectRoot, "sapi: selected voice '" + (nameObj ?? "?") + "' (preferred=" + (preferredMatch is not null) + ")");
            }
            else
            {
                Log(projectRoot, "sapi: no preferred or Swedish voice found — using default");
            }
        }
        catch (Exception ex)
        {
            Log(projectRoot, "sapi voice-select EXCEPTION: " + ex.Message);
        }
    }

    private static void Log(string projectRoot, string message)
    {
        try
        {
            var dir = Path.Combine(projectRoot, "data", "voice");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "tts-debug.log");
            var line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [sapi] " + message + Environment.NewLine;
            File.AppendAllText(path, line);
        }
        catch { }
    }
}
