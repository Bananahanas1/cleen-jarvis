using System.Diagnostics;

namespace JarvisClean;

// Säker program-launcher: bara whitelistade applikationer får öppnas.
// Allt annat blockeras med tydligt felmeddelande.
//
// Säkerhet:
// - Whitelist är hård — ingen "öppna anything"
// - Inga argument tillåts (förhindrar t.ex. "notepad C:\windows\system32\drivers\etc\hosts")
// - Loggas till data\desktop_actions.log för audit
//
// Whitelist kan utökas via vault/Memory/safe_apps.md (framtida fas).
public static class SafeAppLauncher
{
    private static readonly Dictionary<string, string[]> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        // namn → kandidat-paths (första som finns används)
        ["notepad"]  = new[] { "notepad.exe" },
        ["calc"]     = new[] { "calc.exe" },
        ["calculator"] = new[] { "calc.exe" },
        ["explorer"] = new[] { "explorer.exe" },
        ["chrome"]   = new[]
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
        },
        ["edge"]     = new[]
        {
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
            "msedge.exe"
        },
        ["firefox"]  = new[]
        {
            @"C:\Program Files\Mozilla Firefox\firefox.exe",
            @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
        },
        ["vscode"]   = new[]
        {
            @"C:\Users\banan\AppData\Local\Programs\Microsoft VS Code\Code.exe",
            @"C:\Program Files\Microsoft VS Code\Code.exe"
        },
        ["code"]     = new[]
        {
            @"C:\Users\banan\AppData\Local\Programs\Microsoft VS Code\Code.exe",
            @"C:\Program Files\Microsoft VS Code\Code.exe"
        },
        ["spotify"]  = new[]
        {
            @"C:\Users\banan\AppData\Roaming\Spotify\Spotify.exe"
        },
        ["mspaint"]  = new[] { "mspaint.exe" },
        ["paint"]    = new[] { "mspaint.exe" },
        ["opera"]    = new[]
        {
            @"C:\Users\banan\AppData\Local\Programs\Opera\opera.exe",
            @"C:\Users\banan\AppData\Local\Programs\Opera\launcher.exe",
            @"C:\Program Files\Opera\opera.exe",
            @"C:\Program Files (x86)\Opera\opera.exe"
        }
    };

    // Öppnar en URL i Opera (Jarvis-policy 2026-05-10: alltid Opera + Google för web-sök).
    // Returnerar (true, message) om Opera startade, annars (false, fel-meddelande).
    public static LaunchResult TryOpenUrlInOpera(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new LaunchResult(false, "Tom URL.");
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        if (!Whitelist.TryGetValue("opera", out var paths))
            return new LaunchResult(false, "Opera saknas i whitelist.");

        foreach (var path in paths)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "\"" + url + "\"",
                    UseShellExecute = false,
                    ErrorDialog = false
                };
                var p = Process.Start(psi);
                if (p is not null)
                {
                    LogLaunch("opera+url", path, p.Id);
                    return new LaunchResult(true, "Öppnade " + url + " i Opera (PID " + p.Id + ").");
                }
            }
            catch { }
        }
        return new LaunchResult(false,
            "Kunde inte hitta Opera. Lägg till rätt sökväg i app/Desktop/SafeAppLauncher.cs Whitelist['opera'].");
    }

    public sealed record LaunchResult(bool Ok, string Message);

    public static LaunchResult TryLaunch(string appName)
    {
        var name = (appName ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name))
            return new LaunchResult(false, "Skriv vilket program: 'öppna program <namn>'. Tillåtna: " + string.Join(", ", Whitelist.Keys));

        if (!Whitelist.TryGetValue(name, out var candidates))
            return new LaunchResult(false,
                "Programmet '" + appName + "' är inte whitelistat.\n" +
                "Tillåtna: " + string.Join(", ", Whitelist.Keys) + "\n" +
                "Lägg till nya i app/Desktop/SafeAppLauncher.cs (kräver bygge + restart).");

        foreach (var path in candidates)
        {
            try
            {
                // Endast no-args start — förhindrar argument-injection
                var psi = new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    ErrorDialog = false
                };
                var p = Process.Start(psi);
                if (p is not null)
                {
                    LogLaunch(name, path, p.Id);
                    return new LaunchResult(true, "Öppnar " + name + " (" + Path.GetFileName(path) + ", PID " + p.Id + ").");
                }
            }
            catch
            {
                // Prova nästa kandidat
            }
        }

        return new LaunchResult(false, "Hittade inte " + name + " på någon av de förväntade sökvägarna.");
    }

    public static IReadOnlyList<string> ListAllowed() => Whitelist.Keys.OrderBy(k => k).ToList();

    private static void LogLaunch(string name, string path, int pid)
    {
        try
        {
            var dir = Path.Combine(@"F:\Jarvis-clean", "data");
            Directory.CreateDirectory(dir);
            var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  LAUNCH  " + name + "  pid=" + pid + "  path=" + path;
            File.AppendAllText(Path.Combine(dir, "desktop_actions.log"), line + Environment.NewLine);
        }
        catch { }
    }
}
