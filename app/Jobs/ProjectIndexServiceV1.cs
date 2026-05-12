using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JarvisClean;

internal sealed class ProjectIndexFileV1
{
    public string Path { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime ModifiedUtc { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string[] Symbols { get; init; } = Array.Empty<string>();
    public string[] Todos { get; init; } = Array.Empty<string>();
    public string[] Imports { get; init; } = Array.Empty<string>();
    public string Status { get; init; } = "active";
}

internal sealed class ProjectIndexFolderV1
{
    public string Path { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public string Summary { get; init; } = string.Empty;
    public string[] TopExtensions { get; init; } = Array.Empty<string>();
}

internal sealed class ProjectIndexPayloadV1
{
    public DateTime GeneratedAtUtc { get; init; }
    public string ProjectRoot { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public int HashedFileCount { get; init; }
    public int ReusedFileCount { get; init; }
    public int DeletedFileCount { get; init; }
    public List<ProjectIndexFileV1> Files { get; init; } = new();
    public List<ProjectIndexFolderV1> Folders { get; init; } = new();
}

internal sealed record ProjectIndexResultV1(
    string IndexPath,
    int FileCount,
    int HashedFileCount,
    int ReusedFileCount,
    int DeletedFileCount,
    string SearchIndexPath);

internal static class ProjectIndexServiceV1
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".checkpoints",
        ".nuget",
        ".vs",
        "bin",
        "obj",
        "dist",
        "node_modules",
        "graphify-out",
        "EBWebView"
    };

    private const long MaxFileBytes = 2 * 1024 * 1024;
    private const int SummaryReadLimitChars = 120_000;

    public static async Task<ProjectIndexResultV1> BuildAsync(
        string projectRoot,
        Action<int, int, string> progress,
        CancellationToken cancellationToken)
    {
        var indexRoot = Path.Combine(projectRoot, "data", "project-index");
        var indexPath = Path.Combine(indexRoot, "index.json");
        var previous = LoadPrevious(indexPath);
        var previousByPath = previous.Files
            .Where(file => !string.IsNullOrWhiteSpace(file.Path))
            .ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);

        var files = EnumerateFilesSafe(projectRoot)
            .Where(path => !IsExcluded(path, projectRoot))
            .Select(path => new FileInfo(path))
            .Where(info => info.Exists && info.Length <= MaxFileBytes)
            .OrderBy(info => info.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entries = new List<ProjectIndexFileV1>();
        var hashed = 0;
        var reused = 0;

        for (var i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var info = files[i];
            var relPath = Path.GetRelativePath(projectRoot, info.FullName).Replace('\\', '/');

            ProjectIndexFileV1 entry;
            if (previousByPath.TryGetValue(relPath, out var old) &&
                old.SizeBytes == info.Length &&
                old.ModifiedUtc == info.LastWriteTimeUtc &&
                !string.IsNullOrWhiteSpace(old.Sha256))
            {
                entry = new ProjectIndexFileV1
                {
                    Path = relPath,
                    Extension = info.Extension.ToLowerInvariant(),
                    SizeBytes = info.Length,
                    ModifiedUtc = info.LastWriteTimeUtc,
                    Sha256 = old.Sha256,
                    Summary = old.Summary,
                    Symbols = old.Symbols ?? Array.Empty<string>(),
                    Todos = old.Todos ?? Array.Empty<string>(),
                    Imports = old.Imports ?? Array.Empty<string>(),
                    Status = "active"
                };
                reused++;
            }
            else
            {
                var hash = await ComputeHashAsync(info.FullName, cancellationToken);
                var summary = await BuildFileSummaryAsync(info, relPath, cancellationToken);
                entry = new ProjectIndexFileV1
                {
                    Path = relPath,
                    Extension = info.Extension.ToLowerInvariant(),
                    SizeBytes = info.Length,
                    ModifiedUtc = info.LastWriteTimeUtc,
                    Sha256 = hash,
                    Summary = summary.Text,
                    Symbols = summary.Symbols,
                    Todos = summary.Todos,
                    Imports = summary.Imports,
                    Status = "active"
                };
                hashed++;
            }

            entries.Add(entry);

            if (i % 25 == 0 || i == files.Count - 1)
                progress(i + 1, files.Count, "Indexerar " + relPath + " (nytt: " + hashed + ", återanvänt: " + reused + ")");
        }

        Directory.CreateDirectory(indexRoot);

        var currentPaths = entries.Select(entry => entry.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deleted = previousByPath.Keys.Count(path => !currentPaths.Contains(path));
        var folders = BuildFolderSummaries(entries);
        var searchIndexPath = Path.Combine(indexRoot, "search.jsonl");

        var payload = new ProjectIndexPayloadV1
        {
            GeneratedAtUtc = DateTime.UtcNow,
            ProjectRoot = projectRoot,
            FileCount = entries.Count,
            HashedFileCount = hashed,
            ReusedFileCount = reused,
            DeletedFileCount = deleted,
            Files = entries,
            Folders = folders
        };

        await WriteDetailFilesAsync(indexRoot, entries, folders, cancellationToken);
        await WriteSearchIndexAsync(searchIndexPath, entries, cancellationToken);

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await File.WriteAllTextAsync(indexPath, json, cancellationToken);

        return new ProjectIndexResultV1(indexPath, entries.Count, hashed, reused, deleted, searchIndexPath);
    }

    private static bool IsExcluded(string path, string projectRoot)
    {
        var rel = Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
        if (rel.StartsWith("data/project-index/", StringComparison.OrdinalIgnoreCase) ||
            rel.StartsWith("data/jobs/", StringComparison.OrdinalIgnoreCase))
            return true;

        var segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => ExcludedDirectories.Contains(segment));
    }

    private static ProjectIndexPayloadV1 LoadPrevious(string indexPath)
    {
        try
        {
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

    private static IEnumerable<string> EnumerateFilesSafe(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
                yield return file;

            IEnumerable<string> dirs;
            try
            {
                dirs = Directory.EnumerateDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (var child in dirs)
            {
                if (!IsExcluded(child, root))
                    pending.Push(child);
            }
        }
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static async Task<ProjectIndexFileSummaryV1> BuildFileSummaryAsync(FileInfo info, string relPath, CancellationToken cancellationToken)
    {
        if (!IsTextLike(info.Extension))
        {
            return new ProjectIndexFileSummaryV1(
                relPath + " (" + info.Extension.ToLowerInvariant() + ", " + info.Length + " bytes)",
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        try
        {
            var content = await File.ReadAllTextAsync(info.FullName, cancellationToken);
            if (content.Length > SummaryReadLimitChars)
                content = content[..SummaryReadLimitChars];

            var symbols = ExtractSymbols(content);
            var todos = ExtractInterestingLines(content, "todo|fixme|hack", 8);
            var imports = ExtractImports(content, 10);
            var heading = ExtractFirstHeading(content);
            var firstLine = ExtractFirstMeaningfulLine(content);

            var parts = new List<string> { relPath + " (" + info.Extension.ToLowerInvariant() + ", " + info.Length + " bytes)" };
            if (!string.IsNullOrWhiteSpace(heading))
                parts.Add("Heading: " + heading);
            if (symbols.Length > 0)
                parts.Add("Symbols: " + string.Join(", ", symbols.Take(12)));
            if (imports.Length > 0)
                parts.Add("Imports: " + string.Join(", ", imports.Take(6)));
            if (todos.Length > 0)
                parts.Add("TODO: " + string.Join(" | ", todos.Take(4)));
            if (symbols.Length == 0 && todos.Length == 0 && !string.IsNullOrWhiteSpace(firstLine))
                parts.Add("Excerpt: " + firstLine);

            return new ProjectIndexFileSummaryV1(Limit(string.Join(". ", parts), 900), symbols, todos, imports);
        }
        catch (Exception ex)
        {
            return new ProjectIndexFileSummaryV1(
                relPath + " kunde inte sammanfattas: " + ex.Message,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }
    }

    private static bool IsTextLike(string extension)
    {
        return extension.ToLowerInvariant() is
            ".cs" or ".js" or ".ts" or ".tsx" or ".jsx" or ".html" or ".css" or ".json" or ".md" or ".txt" or
            ".xml" or ".yml" or ".yaml" or ".ps1" or ".bat" or ".cmd" or ".py" or ".sql" or ".toml" or ".config" or "";
    }

    private static string[] ExtractSymbols(string content)
    {
        var symbols = new List<string>();
        AddMatches(symbols, content, @"\b(class|interface|record|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)", 2);
        AddMatches(symbols, content, @"\b(function|def)\s+([A-Za-z_][A-Za-z0-9_]*)", 2);
        AddMatches(symbols, content, @"\b(?:public|private|internal|protected|static|\s)+\s*[\w<>\[\],\?]+\s+([A-Z_a-z][A-Za-z0-9_]*)\s*\(", 1);
        AddMatches(symbols, content, @"\bconst\s+([A-Za-z_][A-Za-z0-9_]*)\s*=", 1);
        return symbols.Distinct(StringComparer.OrdinalIgnoreCase).Take(40).ToArray();
    }

    private static void AddMatches(List<string> target, string content, string pattern, int group)
    {
        foreach (Match match in Regex.Matches(content, pattern, RegexOptions.Multiline))
        {
            var value = match.Groups[group].Value.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                target.Add(value);
        }
    }

    private static string[] ExtractInterestingLines(string content, string pattern, int limit)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
            .Select(line => Limit(line, 180))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static string[] ExtractImports(string content, int limit)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line =>
                line.StartsWith("using ", StringComparison.Ordinal) ||
                line.StartsWith("import ", StringComparison.Ordinal) ||
                line.StartsWith("from ", StringComparison.Ordinal) ||
                line.StartsWith("#include", StringComparison.Ordinal) ||
                line.StartsWith("namespace ", StringComparison.Ordinal))
            .Select(line => Limit(line, 180))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static string ExtractFirstHeading(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("#", StringComparison.Ordinal)) ?? "";
    }

    private static string ExtractFirstMeaningfulLine(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal)) ?? "";
    }

    private static List<ProjectIndexFolderV1> BuildFolderSummaries(List<ProjectIndexFileV1> entries)
    {
        return entries
            .GroupBy(entry => FolderOf(entry.Path), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var extensions = group
                    .GroupBy(file => string.IsNullOrWhiteSpace(file.Extension) ? "(none)" : file.Extension, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(ext => ext.Count())
                    .ThenBy(ext => ext.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .Select(ext => ext.Key + ":" + ext.Count())
                    .ToArray();

                var symbols = group
                    .SelectMany(file => file.Symbols ?? Array.Empty<string>())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(12)
                    .ToArray();

                var summary = group.Key + ": " + group.Count() + " filer";
                if (extensions.Length > 0)
                    summary += ". Typer: " + string.Join(", ", extensions);
                if (symbols.Length > 0)
                    summary += ". Symboler: " + string.Join(", ", symbols);

                return new ProjectIndexFolderV1
                {
                    Path = group.Key,
                    FileCount = group.Count(),
                    Summary = Limit(summary, 900),
                    TopExtensions = extensions
                };
            })
            .ToList();
    }

    private static async Task WriteDetailFilesAsync(
        string indexRoot,
        List<ProjectIndexFileV1> entries,
        List<ProjectIndexFolderV1> folders,
        CancellationToken cancellationToken)
    {
        var filesDir = Path.Combine(indexRoot, "files");
        var foldersDir = Path.Combine(indexRoot, "folders");
        Directory.CreateDirectory(filesDir);
        Directory.CreateDirectory(foldersDir);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(filesDir, SafeId(entry.Path) + ".json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(entry, JsonOptions), cancellationToken);
        }

        foreach (var folder in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.Combine(foldersDir, SafeId(folder.Path) + ".json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(folder, JsonOptions), cancellationToken);
        }
    }

    private static async Task WriteSearchIndexAsync(string searchIndexPath, List<ProjectIndexFileV1> entries, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(searchIndexPath, append: false, Encoding.UTF8);
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = JsonSerializer.Serialize(new
            {
                entry.Path,
                entry.Extension,
                entry.Summary,
                entry.Symbols,
                entry.Todos,
                entry.Imports
            });
            await writer.WriteLineAsync(line);
        }
    }

    private static string FolderOf(string relPath)
    {
        var normalized = relPath.Replace('\\', '/');
        var index = normalized.LastIndexOf('/');
        return index < 0 ? "." : normalized[..index];
    }

    private static string SafeId(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..24];
    }

    private static string Limit(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..max] + "...";
    }

    private sealed record ProjectIndexFileSummaryV1(
        string Text,
        string[] Symbols,
        string[] Todos,
        string[] Imports);
}
