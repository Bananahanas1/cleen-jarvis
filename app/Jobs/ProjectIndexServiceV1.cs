using System.Security.Cryptography;
using System.Text.Json;

namespace JarvisClean;

internal sealed record ProjectIndexFileV1(
    string Path,
    string Extension,
    long SizeBytes,
    DateTime ModifiedUtc,
    string Sha256);

internal sealed record ProjectIndexResultV1(string IndexPath, int FileCount);

internal static class ProjectIndexServiceV1
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".checkpoints",
        "bin",
        "obj",
        "dist",
        "node_modules",
        "graphify-out",
        "EBWebView"
    };

    private const long MaxFileBytes = 2 * 1024 * 1024;

    public static async Task<ProjectIndexResultV1> BuildAsync(
        string projectRoot,
        Action<int, int, string> progress,
        CancellationToken cancellationToken)
    {
        var files = Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(path, projectRoot))
            .Where(path => new FileInfo(path).Length <= MaxFileBytes)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entries = new List<ProjectIndexFileV1>();
        for (var i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = files[i];
            var info = new FileInfo(file);
            var relPath = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
            var hash = await ComputeHashAsync(file, cancellationToken);
            entries.Add(new ProjectIndexFileV1(
                relPath,
                info.Extension.ToLowerInvariant(),
                info.Length,
                info.LastWriteTimeUtc,
                hash));

            if (i % 25 == 0 || i == files.Count - 1)
                progress(i + 1, files.Count, "Indexerar " + relPath);
        }

        var indexRoot = Path.Combine(projectRoot, "data", "project-index");
        Directory.CreateDirectory(indexRoot);
        var indexPath = Path.Combine(indexRoot, "index.json");
        var payload = new
        {
            generatedAtUtc = DateTime.UtcNow,
            projectRoot,
            fileCount = entries.Count,
            files = entries
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(indexPath, json, cancellationToken);

        return new ProjectIndexResultV1(indexPath, entries.Count);
    }

    private static bool IsExcluded(string path, string projectRoot)
    {
        var rel = Path.GetRelativePath(projectRoot, path);
        var segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(segment => ExcludedDirectories.Contains(segment));
    }

    private static async Task<string> ComputeHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
