using System.Diagnostics;

namespace JarvisClean;

// Säker program-launcher: bara whitelistade applikationer får öppnas.
// Browser-policy: användarens synliga browser är alltid OperaGX/Opera.
// Intern agent-automation får använda isolerad Playwright Chromium, men Chrome,
// Edge och Firefox får inte vara synliga launch-mål i Jarvis.
public static class SafeAppLauncher
{
    private static readonly Dictionary<string, string[]> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        ["notepad"] = new[] { "notepad.exe" },
        ["calc"] = new[] { "calc.exe" },
        ["calculator"] = new[] { "calc.exe" },
        ["explorer"] = new[] { "explorer.exe" },
        ["vscode"] = new[]
        {
            @"C:\Users\banan\AppData\Local\Programs\Microsoft VS Code\Code.exe",
            @"C:\Program Files\Microsoft VS Code\Code.exe"
        },
        ["code"] = new[]
        {
            @"C:\Users\banan\AppData\Local\Programs\Microsoft VS Code\Code.exe",
            @"C:\Program Files\Microsoft VS Code\Code.exe"
        },
        ["spotify"] = new[]
        {
            @"C:\Users\banan\AppData\Roaming\Spotify\Spotify.exe"
        },
        ["mspaint"] = new[] { "mspaint.exe" },
        ["paint"] = new[] { "mspaint.exe" },
        ["opera"] = BrowserPolicyV1.ExecutableCandidates.ToArray()
    };

    public sealed record LaunchResult(bool Ok, string Message);

    public static LaunchResult TryOpenUrlInOpera(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new LaunchResult(false, "Tom URL.");

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        foreach (var path in BrowserPolicyV1.ExecutableCandidates)
        {
            try
            {
                if (!File.Exists(path))
                    continue;

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
                    return new LaunchResult(true, "Öppnade " + url + " i " + BrowserPolicyV1.DisplayName + " (PID " + p.Id + ").");
                }
            }
            catch
            {
                // Prova nästa kandidat.
            }
        }

        return new LaunchResult(false, BrowserPolicyV1.MissingBrowserMessage());
    }

    public static LaunchResult TryLaunch(string appName)
    {
        var name = (appName ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name))
            return new LaunchResult(false, "Skriv vilket program: 'öppna program <namn>'. Tillåtna: " + string.Join(", ", ListAllowed()));

        if (BrowserPolicyV1.IsBlockedBrowserName(name))
            return new LaunchResult(false, BrowserPolicyV1.BlockedBrowserMessage(appName ?? name));

        if (BrowserPolicyV1.IsBrowserAlias(name))
            name = BrowserPolicyV1.PrimaryLaunchName;

        if (!Whitelist.TryGetValue(name, out var candidates))
            return new LaunchResult(false,
                "Programmet '" + appName + "' är inte whitelistat.\n" +
                "Tillåtna: " + string.Join(", ", ListAllowed()) + "\n" +
                "Lägg till nya i app/Desktop/SafeAppLauncher.cs (kräver bygge + restart).");

        foreach (var path in candidates)
        {
            try
            {
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
                // Prova nästa kandidat.
            }
        }

        if (name == BrowserPolicyV1.PrimaryLaunchName)
            return new LaunchResult(false, BrowserPolicyV1.MissingBrowserMessage());

        return new LaunchResult(false, "Hittade inte " + name + " på någon av de förväntade sökvägarna.");
    }

    public static IReadOnlyList<string> ListAllowed()
    {
        return Whitelist.Keys
            .Concat(new[] { "operagx", "opera gx", "browser", "webbläsare" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k)
            .ToList();
    }

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
