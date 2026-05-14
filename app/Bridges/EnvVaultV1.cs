namespace JarvisClean;

internal static class EnvVaultV1
{
    // Whitelist: bara dessa nycklar får sättas via UI. Skyddar mot att en buggy/skadlig
    // codepath skriver godtyckliga env-variabler till disk.
    public static readonly IReadOnlyList<string> AllowedKeys = new[]
    {
        "GROQ_API_KEY",
        "GEMINI_API_KEY",
        "GITHUB_TOKEN",
        "ANTHROPIC_API_KEY",
        "OPENAI_API_KEY",
        "GOOGLE_MAPS_API_KEY",
        "AISSTREAM_API_KEY",
        // OpenSky Network — autentiserade konton får 4000 credits/dag istället för 100 anonymt.
        // Stöd både OAuth2 (rekommenderat) och Basic Auth (legacy, fortfarande supportat).
        "OPENSKY_CLIENT_ID",
        "OPENSKY_CLIENT_SECRET",
        "OPENSKY_USERNAME",
        "OPENSKY_PASSWORD"
    };

    public static string EnvFilePath(string projectRoot)
    {
        return Path.Combine(projectRoot, ".env");
    }

    public static bool IsAllowed(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        return AllowedKeys.Contains(key, StringComparer.Ordinal);
    }

    public static Dictionary<string, bool> StatusReport(string projectRoot)
    {
        var status = new Dictionary<string, bool>(StringComparer.Ordinal);
        var values = ReadAllNoLogging(projectRoot);
        foreach (var key in AllowedKeys)
        {
            status[key] = values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v);
        }
        return status;
    }

    public static bool TryGetValue(string projectRoot, string key, out string value)
    {
        value = string.Empty;
        if (!IsAllowed(key)) return false;
        var values = ReadAllNoLogging(projectRoot);
        if (values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
        {
            value = v;
            return true;
        }
        return false;
    }

    public static string SetKey(string projectRoot, string key, string value)
    {
        // Returnerar en användarvänlig statussträng. Loggar ALDRIG värdet.
        if (!IsAllowed(key))
            return "Avvisad: nyckel '" + key + "' är inte i whitelist. Tillåtna: " + string.Join(", ", AllowedKeys);

        if (string.IsNullOrWhiteSpace(value))
            return "Avvisad: tomt värde. Använd Rensa för att ta bort.";

        var path = EnvFilePath(projectRoot);
        var lines = LoadLinesSafe(path);
        var newLines = new List<string>();
        var replaced = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith(key + "=", StringComparison.Ordinal))
            {
                newLines.Add(key + "=" + EscapeValue(value));
                replaced = true;
            }
            else
            {
                newLines.Add(line);
            }
        }

        if (!replaced)
            newLines.Add(key + "=" + EscapeValue(value));

        WriteLinesSafe(path, newLines);
        // Sätt i nuvarande process också, så HybridModelRouter ser värdet utan omstart.
        Environment.SetEnvironmentVariable(key, value);

        return key + " sparad i .env och aktiv för denna session.";
    }

    public static string ClearKey(string projectRoot, string key)
    {
        if (!IsAllowed(key))
            return "Avvisad: nyckel '" + key + "' är inte i whitelist.";

        var path = EnvFilePath(projectRoot);
        if (!File.Exists(path))
            return key + " var inte satt.";

        var lines = LoadLinesSafe(path);
        var kept = lines.Where(line =>
        {
            var trimmed = line.TrimStart();
            return !trimmed.StartsWith(key + "=", StringComparison.Ordinal);
        }).ToList();

        WriteLinesSafe(path, kept);
        Environment.SetEnvironmentVariable(key, null);
        return key + " borttagen från .env.";
    }

    private static Dictionary<string, string> ReadAllNoLogging(string projectRoot)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        var path = EnvFilePath(projectRoot);
        if (!File.Exists(path)) return dict;

        try
        {
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw?.TrimStart();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = UnescapeValue(line.Substring(eq + 1));
                if (!string.IsNullOrEmpty(key))
                    dict[key] = value;
            }
        }
        catch
        {
            // Aldrig logga rådata från .env.
        }
        return dict;
    }

    private static List<string> LoadLinesSafe(string path)
    {
        if (!File.Exists(path)) return new List<string>();
        try { return File.ReadAllLines(path).ToList(); }
        catch { return new List<string>(); }
    }

    private static void WriteLinesSafe(string path, IEnumerable<string> lines)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllLines(path, lines);
    }

    private static string EscapeValue(string value)
    {
        // Enkel quoting: om värdet innehåller whitespace eller specialtecken, citera det.
        if (value.IndexOfAny(new[] { ' ', '\t', '"', '\'', '#', '=' }) >= 0)
        {
            var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }
        return value;
    }

    private static string UnescapeValue(string raw)
    {
        var v = raw.Trim();
        if (v.Length >= 2 && v[0] == '"' && v[^1] == '"')
        {
            v = v.Substring(1, v.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
        return v;
    }

    public static void LoadIntoProcessAtStartup(string projectRoot)
    {
        var values = ReadAllNoLogging(projectRoot);
        foreach (var kv in values)
        {
            if (!IsAllowed(kv.Key)) continue;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(kv.Key)))
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }
    }
}
