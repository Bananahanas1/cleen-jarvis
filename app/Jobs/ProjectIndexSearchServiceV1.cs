using System.Text;
using System.Text.Json;

namespace JarvisClean;

internal sealed record ProjectIndexSearchHitV1(
    string Path,
    string Summary,
    int Score,
    string[] Symbols,
    string[] Todos);

internal static class ProjectIndexSearchServiceV1
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string FormatSearchResults(string projectRoot, string query, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Skriv: /projekt sök <query>. Exempel: /projekt sök CommandRouter";

        var hits = Search(projectRoot, query, limit);
        if (hits.Count == 0)
            return "Inga träffar i projektindex för: " + query + "\nKör /projekt index om indexet saknas eller är gammalt.";

        var sb = new StringBuilder();
        sb.AppendLine("Projektindex-träffar för \"" + query.Trim() + "\":");

        foreach (var hit in hits)
        {
            sb.AppendLine();
            sb.AppendLine("[" + hit.Score + "] " + hit.Path);
            if (hit.Symbols.Length > 0)
                sb.AppendLine("Symboler: " + string.Join(", ", hit.Symbols.Take(8)));
            if (!string.IsNullOrWhiteSpace(hit.Summary))
                sb.AppendLine(Limit(hit.Summary, 260));
            if (hit.Todos.Length > 0)
                sb.AppendLine("TODO: " + string.Join(" | ", hit.Todos.Take(3)));
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildContextPrefix(string projectRoot, string query, int k = 4)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "";

        var hits = Search(projectRoot, query, k);
        if (hits.Count == 0)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine("Projektindex-kontext (lokalt index, bara relevanta filer):");
        foreach (var hit in hits)
        {
            var symbols = hit.Symbols.Length == 0 ? "" : " Symboler: " + string.Join(", ", hit.Symbols.Take(8)) + ".";
            sb.AppendLine("- " + hit.Path + ": " + Limit(hit.Summary + symbols, 420));
        }

        return sb.ToString().TrimEnd();
    }

    public static IReadOnlyList<ProjectIndexSearchHitV1> Search(string projectRoot, string query, int limit = 8)
    {
        var payload = Load(projectRoot);
        if (payload.Files.Count == 0)
            return Array.Empty<ProjectIndexSearchHitV1>();

        var terms = Normalize(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (terms.Length == 0)
            return Array.Empty<ProjectIndexSearchHitV1>();

        return payload.Files
            .Select(file => ToHit(file, terms))
            .Where(hit => hit.Score > 0)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    private static ProjectIndexSearchHitV1 ToHit(ProjectIndexFileV1 file, string[] terms)
    {
        var path = file.Path ?? "";
        var summary = file.Summary ?? "";
        var symbols = file.Symbols ?? Array.Empty<string>();
        var todos = file.Todos ?? Array.Empty<string>();
        var imports = file.Imports ?? Array.Empty<string>();

        var normalizedPath = Normalize(path);
        var normalizedSymbols = Normalize(string.Join(" ", symbols));
        var normalizedSummary = Normalize(summary);
        var normalizedTodos = Normalize(string.Join(" ", todos));
        var normalizedImports = Normalize(string.Join(" ", imports));

        var score = 0;
        foreach (var term in terms)
        {
            if (normalizedPath.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 12;
            if (normalizedSymbols.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 10;
            if (normalizedSummary.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 6;
            if (normalizedTodos.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 4;
            if (normalizedImports.Contains(term, StringComparison.OrdinalIgnoreCase))
                score += 2;
        }

        return new ProjectIndexSearchHitV1(path, summary, score, symbols, todos);
    }

    private static ProjectIndexPayloadV1 Load(string projectRoot)
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

    private static string Normalize(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");
    }

    private static string Limit(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..max] + "...";
    }
}
