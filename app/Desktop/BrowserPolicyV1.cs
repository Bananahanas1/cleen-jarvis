namespace JarvisClean;

internal static class BrowserPolicyV1
{
    public const string DisplayName = "OperaGX/Opera";
    public const string PrimaryLaunchName = "opera";
    public const string InternalAutomationEngine = "isolated Playwright Chromium";

    private static readonly string[] BrowserAliases =
    {
        "opera",
        "operagx",
        "opera gx",
        "browser",
        "webblasare",
        "webbläsare"
    };

    private static readonly string[] BlockedBrowserNames =
    {
        "chrome",
        "google chrome",
        "edge",
        "microsoft edge",
        "firefox",
        "mozilla firefox",
        "chromium"
    };

    private static readonly string[] CandidatePaths = BuildCandidatePaths();

    public static IReadOnlyList<string> ExecutableCandidates => CandidatePaths;

    public static bool IsBrowserAlias(string appName)
    {
        var normalized = Normalize(appName);
        return BrowserAliases.Any(alias => Normalize(alias) == normalized);
    }

    public static bool IsBlockedBrowserName(string appName)
    {
        var normalized = Normalize(appName);
        return BlockedBrowserNames.Any(name => Normalize(name) == normalized);
    }

    public static string BlockedBrowserMessage(string appName)
    {
        return "Browsern '" + appName + "' är avstängd i Jarvis. " +
               "Jarvis använder bara " + DisplayName + " som synlig browser. " +
               InternalAutomationEngine + " får bara användas internt för isolerad agent-automation.";
    }

    public static string MissingBrowserMessage()
    {
        return "Kunde inte hitta " + DisplayName + ". Installera OperaGX eller Opera, " +
               "eller lägg till rätt sökväg i BrowserPolicyV1.";
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("-", " ");
    }

    private static string[] BuildCandidatePaths()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return new[]
        {
            Path.Combine(local, "Programs", "Opera GX", "opera.exe"),
            Path.Combine(local, "Programs", "Opera GX", "launcher.exe"),
            Path.Combine(programFiles, "Opera GX", "opera.exe"),
            Path.Combine(programFiles, "Opera GX", "launcher.exe"),
            Path.Combine(programFilesX86, "Opera GX", "opera.exe"),
            Path.Combine(programFilesX86, "Opera GX", "launcher.exe"),
            Path.Combine(local, "Programs", "Opera", "opera.exe"),
            Path.Combine(local, "Programs", "Opera", "launcher.exe"),
            Path.Combine(programFiles, "Opera", "opera.exe"),
            Path.Combine(programFiles, "Opera", "launcher.exe"),
            Path.Combine(programFilesX86, "Opera", "opera.exe"),
            Path.Combine(programFilesX86, "Opera", "launcher.exe")
        }
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }
}
