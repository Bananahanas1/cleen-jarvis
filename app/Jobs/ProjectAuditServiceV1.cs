using System.Text;
using System.Text.Json;

namespace JarvisClean;

internal sealed record ProjectAuditResultV1(string ReportPath, int FileCount, int TodoCount);

internal static class ProjectAuditServiceV1
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<ProjectAuditResultV1> WriteAuditAsync(
        string projectRoot,
        string reportPath,
        CancellationToken cancellationToken)
    {
        var payload = LoadIndex(projectRoot);
        var files = payload.Files
            .Where(file => string.Equals(file.Status, "active", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var todos = files
            .SelectMany(file => (file.Todos ?? Array.Empty<string>()).Select(todo => (file.Path, Todo: todo)))
            .Take(25)
            .ToList();

        var topExtensions = files
            .GroupBy(file => string.IsNullOrWhiteSpace(file.Extension) ? "(none)" : file.Extension, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(group => "- " + group.Key + ": " + group.Count())
            .ToList();

        var keyFiles = files
            .OrderByDescending(file => ScoreKeyFile(file))
            .ThenBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        var symbols = files
            .SelectMany(file => file.Symbols ?? Array.Empty<string>())
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(symbol => symbol, StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();

        var folders = payload.Folders
            .OrderByDescending(folder => folder.FileCount)
            .ThenBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("# Jarvis Project Audit");
        sb.AppendLine();
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Project root: " + projectRoot);
        sb.AppendLine();
        sb.AppendLine("## Sammanfattning");
        sb.AppendLine();
        sb.AppendLine("- Filer i index: " + files.Count);
        sb.AppendLine("- Mappar i index: " + payload.Folders.Count);
        sb.AppendLine("- TODO/FIXME/HACK-rader: " + todos.Count);
        sb.AppendLine("- Senaste indexering: " + payload.GeneratedAtUtc.ToString("u"));
        sb.AppendLine();

        sb.AppendLine("## Viktiga moduler");
        sb.AppendLine();
        if (folders.Count == 0)
            sb.AppendLine("- Inga mappar hittades i indexet.");
        foreach (var folder in folders)
            sb.AppendLine("- " + Limit(folder.Summary, 220));
        sb.AppendLine();

        sb.AppendLine("## Filtyper");
        sb.AppendLine();
        if (topExtensions.Count == 0)
            sb.AppendLine("- Inga filtyper hittades.");
        foreach (var line in topExtensions)
            sb.AppendLine(line);
        sb.AppendLine();

        sb.AppendLine("## Nyckelfiler");
        sb.AppendLine();
        if (keyFiles.Count == 0)
            sb.AppendLine("- Inga nyckelfiler hittades.");
        foreach (var file in keyFiles)
        {
            sb.AppendLine("- `" + file.Path + "`");
            if (!string.IsNullOrWhiteSpace(file.Summary))
                sb.AppendLine("  - " + Limit(file.Summary, 220));
        }
        sb.AppendLine();

        sb.AppendLine("## Symboler");
        sb.AppendLine();
        if (symbols.Count == 0)
            sb.AppendLine("- Inga klasser/funktioner kunde extraheras.");
        else
            sb.AppendLine(string.Join(", ", symbols));
        sb.AppendLine();

        sb.AppendLine("## TODO och riskmarkörer");
        sb.AppendLine();
        if (todos.Count == 0)
            sb.AppendLine("- Inga TODO/FIXME/HACK-rader hittades i indexerade textfiler.");
        foreach (var item in todos)
            sb.AppendLine("- `" + item.Path + "`: " + item.Todo);
        sb.AppendLine();

        sb.AppendLine("## Nästa rekommenderade steg");
        sb.AppendLine();
        sb.AppendLine("- Kör `/projekt sök <ämne>` för att hämta smal kontext i stället för att läsa hela projektet.");
        sb.AppendLine("- Kör `/projekt index` efter större kodändringar så incremental scan uppdaterar metadata.");
        sb.AppendLine("- Fortsätt bryta ut tung logik från `Program.cs` till fokuserade services.");

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
        await File.WriteAllTextAsync(reportPath, sb.ToString(), cancellationToken);

        return new ProjectAuditResultV1(reportPath, files.Count, todos.Count);
    }

    private static ProjectIndexPayloadV1 LoadIndex(string projectRoot)
    {
        try
        {
            var indexPath = Path.Combine(projectRoot, "data", "project-index", "index.json");
            if (!File.Exists(indexPath))
                return new ProjectIndexPayloadV1();

            var json = File.ReadAllText(indexPath);
            return JsonSerializer.Deserialize<ProjectIndexPayloadV1>(json, JsonOptions) ?? new ProjectIndexPayloadV1();
        }
        catch
        {
            return new ProjectIndexPayloadV1();
        }
    }

    private static int ScoreKeyFile(ProjectIndexFileV1 file)
    {
        var path = file.Path ?? "";
        var score = 0;
        if (path.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase))
            score += 100;
        if (path.Contains("CommandRouter", StringComparison.OrdinalIgnoreCase))
            score += 80;
        if (path.Contains("PendingApproval", StringComparison.OrdinalIgnoreCase))
            score += 70;
        if (path.Contains("Jobs/", StringComparison.OrdinalIgnoreCase))
            score += 60;
        score += Math.Min(30, (file.Symbols ?? Array.Empty<string>()).Length * 3);
        score += Math.Min(20, (file.Todos ?? Array.Empty<string>()).Length * 4);
        return score;
    }

    private static string Limit(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..max] + "...";
    }
}
