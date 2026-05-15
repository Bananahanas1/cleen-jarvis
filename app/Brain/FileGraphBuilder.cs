using System.Text.Json;
using System.Text.RegularExpressions;

namespace JarvisClean;

// Relations-first graph builder for Brain.
// It favors meaningful code/vault connections over showing every generated file.
internal static class FileGraphBuilder
{
    private const string ProjectRoot = @"F:\Jarvis-clean";
    private const string BuilderVersion = "relations-first-v1-20260515-pruned";
    private const long MaxFileSizeBytes = 200_000;
    private const int MaxProjectNodes = 300;
    private const int MaxVaultNodes = 250;
    private const int MaxVaultFilesToScan = 600;

    private static readonly HashSet<string> CodeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".js", ".html", ".css", ".py"
    };

    private static readonly HashSet<string> ProjectMarkdownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md"
    };

    private static readonly HashSet<string> RuntimeOrGeneratedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget",
        "backups", ".tmp", ".checkpoints", "build-verify", "github-brain",
        "third_party", "__pycache__", "vendor", "data", "graphify-out",
        ".claude", "Obsidian valv", ".venv", "logs", "runtimes"
    };

    internal sealed class Node
    {
        public string id { get; set; } = "";
        public string path { get; set; } = "";
        public string label { get; set; } = "";
        public string kind { get; set; } = "";
        public string source { get; set; } = "";
        public int degree { get; set; }
        public int mtimeMin { get; set; } = 99999;
    }

    internal sealed class Edge
    {
        public string source { get; set; } = "";
        public string target { get; set; } = "";
        public string kind { get; set; } = "";
    }

    internal sealed class GraphMeta
    {
        public string mode { get; set; } = "relations-first";
        public string builderVersion { get; set; } = BuilderVersion;
        public int projectCandidateFiles { get; set; }
        public int vaultCandidateFiles { get; set; }
        public int includedProjectNodes { get; set; }
        public int includedVaultNodes { get; set; }
        public int hiddenGeneratedFiles { get; set; }
        public int hiddenVaultProjectFiles { get; set; }
        public int hiddenJsonFiles { get; set; }
        public int hiddenOversizeFiles { get; set; }
        public int hiddenUnlinkedMarkdownFiles { get; set; }
        public int zeroDegreeCount { get; set; }
    }

    internal sealed class GraphPayload
    {
        public List<Node> nodes { get; set; } = new();
        public List<Edge> edges { get; set; } = new();
        public GraphMeta meta { get; set; } = new();
    }

    private sealed class ProjectCandidate
    {
        public string FullPath { get; init; } = "";
        public string RelPath { get; init; } = "";
        public string Ext { get; init; } = "";
        public string Kind => Ext.TrimStart('.').ToLowerInvariant();
        public bool IsCode => CodeExtensions.Contains(Ext);
        public bool IsMarkdown => ProjectMarkdownExtensions.Contains(Ext);
    }

    private sealed class VaultNote
    {
        public string FullPath { get; set; } = "";
        public string RelPath { get; set; } = "";
        public string Title { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public List<string> WikiLinks { get; set; } = new();
    }

    public static string BuildJsonForRoot(string projectRoot = ProjectRoot)
    {
        var graph = BuildForRoot(projectRoot);
        return JsonSerializer.Serialize(graph);
    }

    public static GraphPayload BuildForRoot(string projectRoot)
    {
        var nodes = new List<Node>();
        var nodeById = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
        var edges = new List<Edge>();
        var edgeSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var meta = new GraphMeta();

        var candidates = EnumerateProjectCandidates(projectRoot, meta);
        meta.projectCandidateFiles = candidates.Count;

        var candidateByRelPath = candidates
            .GroupBy(c => c.RelPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var candidateByWikiKey = BuildProjectWikiIndex(candidates);

        var linkedMarkdown = FindLinkedProjectMarkdown(candidates, candidateByRelPath, candidateByWikiKey);
        var selectedCandidates = candidates
            .Where(c => c.IsCode || linkedMarkdown.Contains(c.RelPath))
            .OrderBy(ProjectPriority)
            .ThenBy(c => c.RelPath, StringComparer.OrdinalIgnoreCase)
            .Take(MaxProjectNodes)
            .ToList();
        meta.hiddenUnlinkedMarkdownFiles = candidates.Count(c => c.IsMarkdown && !linkedMarkdown.Contains(c.RelPath));

        foreach (var f in selectedCandidates)
        {
            var node = CreateProjectNode(f);
            nodes.Add(node);
            nodeById[node.id] = node;
        }

        var projectByName = nodes.Where(n => n.source == "project")
            .ToLookup(n => Path.GetFileNameWithoutExtension(n.path), StringComparer.OrdinalIgnoreCase);
        var projectByRelPath = nodes.Where(n => n.source == "project")
            .ToDictionary(n => n.path, n => n, StringComparer.OrdinalIgnoreCase);
        var projectByWikiKey = BuildProjectNodeWikiIndex(nodes);

        foreach (var node in nodes.Where(n => n.source == "project").ToList())
        {
            var abs = Path.Combine(projectRoot, node.path.Replace("/", "\\"));
            string content;
            try { content = File.ReadAllText(abs); }
            catch { continue; }

            switch (node.kind)
            {
                case "cs": ScanCsFile(node, content, projectByName, edges, edgeSeen); break;
                case "js": ScanJsFile(node, content, projectByRelPath, edges, edgeSeen); break;
                case "html": ScanHtmlFile(node, content, projectByRelPath, edges, edgeSeen); break;
                case "py": ScanPyFile(node, content, projectByName, edges, edgeSeen); break;
                case "md": ScanMarkdownFile(node, content, projectByRelPath, projectByWikiKey, edges, edgeSeen); break;
            }
        }

        var vaultRaw = ScanVault(projectRoot);
        meta.vaultCandidateFiles = vaultRaw.Count;
        var vaultKeep = SelectVaultNotes(vaultRaw).Take(MaxVaultNodes).ToList();
        var vaultByWikiKey = BuildVaultWikiIndex(vaultKeep);

        foreach (var v in vaultKeep)
        {
            var node = new Node
            {
                id = "vault:" + v.RelPath,
                path = v.RelPath,
                label = v.Title,
                kind = "vault",
                source = "vault",
                mtimeMin = SafeMtimeMin(v.FullPath)
            };
            nodes.Add(node);
            nodeById[node.id] = node;
        }

        foreach (var v in vaultKeep)
        {
            var srcId = "vault:" + v.RelPath;
            foreach (var wl in v.WikiLinks)
            {
                if (TryResolveVaultWiki(wl, vaultByWikiKey, out var target))
                    AddEdge(srcId, "vault:" + target.RelPath, "vault-link", edges, edgeSeen);
            }

            if (!string.IsNullOrWhiteSpace(v.SourceFile))
            {
                var sf = NormalizeRelPath(v.SourceFile);
                if (projectByRelPath.TryGetValue(sf, out var projNode))
                    AddEdge(srcId, projNode.id, "vault-source", edges, edgeSeen);
            }
        }

        foreach (var e in edges)
        {
            if (nodeById.TryGetValue(e.source, out var s)) s.degree++;
            if (nodeById.TryGetValue(e.target, out var t)) t.degree++;
        }

        meta.includedProjectNodes = nodes.Count(n => n.source == "project");
        meta.includedVaultNodes = nodes.Count(n => n.source == "vault");
        meta.zeroDegreeCount = nodes.Count(n => n.degree == 0);

        return new GraphPayload { nodes = nodes, edges = edges, meta = meta };
    }

    private static List<ProjectCandidate> EnumerateProjectCandidates(string root, GraphMeta meta)
    {
        if (!Directory.Exists(root)) return new List<ProjectCandidate>();

        var list = new List<ProjectCandidate>();
        foreach (var f in EnumerateSafePruned(root).OrderBy(p => NormalizeRelPath(Path.GetRelativePath(root, p)), StringComparer.OrdinalIgnoreCase))
        {
            var rel = NormalizeRelPath(Path.GetRelativePath(root, f));
            var parts = rel.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && string.Equals(parts[0], "vault", StringComparison.OrdinalIgnoreCase))
            {
                meta.hiddenVaultProjectFiles++;
                continue;
            }

            if (parts.Any(p => RuntimeOrGeneratedDirs.Contains(p)))
            {
                meta.hiddenGeneratedFiles++;
                continue;
            }

            var ext = Path.GetExtension(f);
            if (string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase))
            {
                meta.hiddenJsonFiles++;
                continue;
            }

            if (!CodeExtensions.Contains(ext) && !ProjectMarkdownExtensions.Contains(ext))
                continue;

            try
            {
                if (new FileInfo(f).Length > MaxFileSizeBytes)
                {
                    meta.hiddenOversizeFiles++;
                    continue;
                }
            }
            catch { continue; }

            list.Add(new ProjectCandidate { FullPath = f, RelPath = rel, Ext = ext });
        }

        return list;
    }

    private static HashSet<string> FindLinkedProjectMarkdown(
        List<ProjectCandidate> candidates,
        Dictionary<string, ProjectCandidate> byRelPath,
        Dictionary<string, ProjectCandidate> byWikiKey)
    {
        var linked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates.Where(c => c.IsMarkdown))
        {
            string content;
            try { content = File.ReadAllText(candidate.FullPath); }
            catch { continue; }

            foreach (var target in ExtractMarkdownProjectTargets(candidate.RelPath, content, byRelPath, byWikiKey))
            {
                linked.Add(candidate.RelPath);
                if (target.IsMarkdown)
                    linked.Add(target.RelPath);
            }
        }

        return linked;
    }

    private static Node CreateProjectNode(ProjectCandidate f)
    {
        return new Node
        {
            id = "proj:" + f.RelPath,
            path = f.RelPath,
            label = Path.GetFileName(f.RelPath),
            kind = f.Kind,
            source = "project",
            mtimeMin = SafeMtimeMin(f.FullPath)
        };
    }

    private static int ProjectPriority(ProjectCandidate c)
    {
        return c.Ext.ToLowerInvariant() switch
        {
            ".cs" => 0,
            ".js" => 1,
            ".html" => 2,
            ".css" => 3,
            ".py" => 4,
            ".md" => 5,
            _ => 99
        };
    }

    private static IEnumerable<string> EnumerateSafePruned(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(dir); }
            catch { continue; }

            foreach (var file in files)
                yield return file;

            IEnumerable<string> children;
            try { children = Directory.EnumerateDirectories(dir); }
            catch { continue; }

            foreach (var child in children)
            {
                var name = Path.GetFileName(child);
                if (RuntimeOrGeneratedDirs.Contains(name))
                    continue;
                pending.Push(child);
            }
        }
    }

    private static void AddEdge(string source, string target, string kind, List<Edge> edges, HashSet<string> seen)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return;
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase)) return;
        if (!seen.Add(source + "->" + target + ":" + kind)) return;
        edges.Add(new Edge { source = source, target = target, kind = kind });
    }

    private static void ScanCsFile(Node node, string content, ILookup<string, Node> projectByName, List<Edge> edges, HashSet<string> seen)
    {
        foreach (Match m in Regex.Matches(content, @"\b([A-Z][A-Za-z0-9_]+)\b"))
        {
            var name = m.Groups[1].Value;
            if (name.Length < 4) continue;
            foreach (var t in projectByName[name])
                AddEdge(node.id, t.id, "cs-ref", edges, seen);
        }
    }

    private static void ScanJsFile(Node node, string content, Dictionary<string, Node> byRelPath, List<Edge> edges, HashSet<string> seen)
    {
        foreach (Match m in Regex.Matches(content, @"(?:import\s.*?from\s*|require\(\s*|import\(\s*)['""]([^'""]+)['""]"))
            ResolveJsSpec(node, m.Groups[1].Value, byRelPath, edges, seen);
    }

    private static void ResolveJsSpec(Node node, string spec, Dictionary<string, Node> byRelPath, List<Edge> edges, HashSet<string> seen)
    {
        if (!spec.StartsWith(".") && !spec.StartsWith("/")) return;
        var nodeDir = Path.GetDirectoryName(node.path)?.Replace("\\", "/") ?? "";
        var combined = Path.GetFullPath(Path.Combine("/", nodeDir, spec.Trim('/')));
        var asRel = NormalizeRelPath(combined.TrimStart('\\', '/'));
        foreach (var candidate in new[] { asRel, asRel + ".js", asRel + "/index.js" })
        {
            if (byRelPath.TryGetValue(candidate, out var t))
            {
                AddEdge(node.id, t.id, "js-import", edges, seen);
                return;
            }
        }
    }

    private static void ScanHtmlFile(Node node, string content, Dictionary<string, Node> byRelPath, List<Edge> edges, HashSet<string> seen)
    {
        foreach (Match m in Regex.Matches(content, @"<script[^>]+src\s*=\s*['""]([^'""]+)['""]"))
            ResolveJsSpec(node, m.Groups[1].Value, byRelPath, edges, seen);
        foreach (Match m in Regex.Matches(content, @"<link[^>]+href\s*=\s*['""]([^'""]+)['""]"))
            ResolveJsSpec(node, m.Groups[1].Value, byRelPath, edges, seen);
    }

    private static void ScanPyFile(Node node, string content, ILookup<string, Node> projectByName, List<Edge> edges, HashSet<string> seen)
    {
        foreach (Match m in Regex.Matches(content, @"^\s*(?:from\s+(\w+)\s+import|import\s+(\w+))", RegexOptions.Multiline))
        {
            var name = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (string.IsNullOrEmpty(name)) continue;
            foreach (var t in projectByName[name])
                if (t.kind == "py")
                    AddEdge(node.id, t.id, "py-import", edges, seen);
        }
    }

    private static void ScanMarkdownFile(
        Node node,
        string content,
        Dictionary<string, Node> byRelPath,
        Dictionary<string, Node> byWikiKey,
        List<Edge> edges,
        HashSet<string> seen)
    {
        foreach (var target in ExtractMarkdownNodeTargets(node.path, content, byRelPath, byWikiKey))
            AddEdge(node.id, target.id, "md-link", edges, seen);

        foreach (var sf in ExtractSourceFiles(content))
        {
            if (TryResolveProjectRel(node.path, sf, byRelPath, out var target))
                AddEdge(node.id, target.id, "md-source", edges, seen);
        }
    }

    private static IEnumerable<ProjectCandidate> ExtractMarkdownProjectTargets(
        string sourceRelPath,
        string content,
        Dictionary<string, ProjectCandidate> byRelPath,
        Dictionary<string, ProjectCandidate> byWikiKey)
    {
        foreach (var sf in ExtractSourceFiles(content))
            if (TryResolveProjectRel(sourceRelPath, sf, byRelPath, out var t))
                yield return t;

        foreach (var spec in ExtractMarkdownLinkSpecs(content).Concat(ExtractBacktickPathSpecs(content)))
            if (TryResolveProjectRel(sourceRelPath, spec, byRelPath, out var t))
                yield return t;

        foreach (var wiki in ExtractWikiTargets(content))
            if (TryResolveProjectWiki(wiki, byWikiKey, out var t))
                yield return t;
    }

    private static IEnumerable<Node> ExtractMarkdownNodeTargets(
        string sourceRelPath,
        string content,
        Dictionary<string, Node> byRelPath,
        Dictionary<string, Node> byWikiKey)
    {
        foreach (var spec in ExtractMarkdownLinkSpecs(content).Concat(ExtractBacktickPathSpecs(content)))
            if (TryResolveProjectRel(sourceRelPath, spec, byRelPath, out var t))
                yield return t;

        foreach (var wiki in ExtractWikiTargets(content))
            if (TryResolveProjectWiki(wiki, byWikiKey, out var t))
                yield return t;
    }

    private static IEnumerable<string> ExtractSourceFiles(string content)
    {
        foreach (Match m in Regex.Matches(content, @"(?im)^\s*source_file:\s*['""]?([^'""\r\n]+)['""]?\s*$"))
            yield return m.Groups[1].Value.Trim();
    }

    private static IEnumerable<string> ExtractMarkdownLinkSpecs(string content)
    {
        foreach (Match m in Regex.Matches(content, @"\[[^\]]+\]\(([^)#]+)(?:#[^)]+)?\)"))
        {
            var spec = m.Groups[1].Value.Trim();
            if (IsLocalFileSpec(spec))
                yield return spec;
        }
    }

    private static IEnumerable<string> ExtractBacktickPathSpecs(string content)
    {
        foreach (Match m in Regex.Matches(content, "`([^`]+)`"))
        {
            var spec = m.Groups[1].Value.Trim();
            if (IsLocalFileSpec(spec))
                yield return spec;
        }
    }

    private static IEnumerable<string> ExtractWikiTargets(string content)
    {
        foreach (Match m in Regex.Matches(content, @"\[\[([^\]\|#]+)(?:[#\|][^\]]*)?\]\]"))
        {
            var target = m.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(target))
                yield return target;
        }
    }

    private static bool TryResolveProjectRel<T>(string sourceRelPath, string spec, Dictionary<string, T> byRelPath, out T target)
    {
        target = default!;
        var normalized = NormalizeRelPath(spec.Split('#')[0].Trim());
        if (string.IsNullOrWhiteSpace(normalized)) return false;

        var candidates = new List<string>();
        if (normalized.StartsWith("."))
        {
            var sourceDir = Path.GetDirectoryName(sourceRelPath)?.Replace("\\", "/") ?? "";
            var combined = Path.GetFullPath(Path.Combine("/", sourceDir, normalized)).TrimStart('\\', '/');
            candidates.Add(NormalizeRelPath(combined));
        }
        candidates.Add(normalized.TrimStart('/'));

        foreach (var c in candidates.Select(NormalizeRelPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var variant in CandidatePathVariants(c))
            {
                if (byRelPath.TryGetValue(variant, out var found))
                {
                    target = found;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryResolveProjectWiki<T>(string target, Dictionary<string, T> byWikiKey, out T value)
    {
        value = default!;
        var key = NormalizeWikiKey(target);
        if (byWikiKey.TryGetValue(key, out var exact))
        {
            value = exact;
            return true;
        }

        if (byWikiKey.TryGetValue(Path.GetFileNameWithoutExtension(key), out var byName))
        {
            value = byName;
            return true;
        }

        return false;
    }

    private static IEnumerable<string> CandidatePathVariants(string rel)
    {
        yield return rel;
        if (!Path.HasExtension(rel))
        {
            foreach (var ext in CodeExtensions.Concat(ProjectMarkdownExtensions))
                yield return rel + ext;
            yield return rel + "/index.js";
            yield return rel + "/README.md";
        }
    }

    private static Dictionary<string, ProjectCandidate> BuildProjectWikiIndex(IEnumerable<ProjectCandidate> candidates)
    {
        var index = new Dictionary<string, ProjectCandidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in candidates)
        {
            foreach (var key in ProjectWikiKeys(c.RelPath))
                index.TryAdd(key, c);
        }
        return index;
    }

    private static Dictionary<string, Node> BuildProjectNodeWikiIndex(IEnumerable<Node> nodes)
    {
        var index = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in nodes.Where(n => n.source == "project"))
        {
            foreach (var key in ProjectWikiKeys(n.path))
                index.TryAdd(key, n);
        }
        return index;
    }

    private static IEnumerable<string> ProjectWikiKeys(string relPath)
    {
        var noExt = NormalizeRelPath(Path.ChangeExtension(relPath, null) ?? relPath);
        yield return NormalizeWikiKey(noExt);
        yield return NormalizeWikiKey(Path.GetFileNameWithoutExtension(relPath));
    }

    private static List<VaultNote> ScanVault(string projectRoot)
    {
        var vaultRoot = Path.Combine(projectRoot, "vault");
        var notes = new List<VaultNote>();
        if (!Directory.Exists(vaultRoot)) return notes;

        try
        {
            foreach (var f in Directory.EnumerateFiles(vaultRoot, "*.md", SearchOption.AllDirectories)
                         .OrderBy(p => NormalizeRelPath(Path.GetRelativePath(vaultRoot, p)), StringComparer.OrdinalIgnoreCase))
            {
                if (notes.Count >= MaxVaultFilesToScan) break;
                var rel = NormalizeRelPath(Path.GetRelativePath(vaultRoot, f));
                string content;
                try
                {
                    var info = new FileInfo(f);
                    if (info.Length > MaxFileSizeBytes || info.Length < 3) continue;
                    content = File.ReadAllText(f);
                }
                catch { continue; }

                var note = new VaultNote
                {
                    FullPath = f,
                    RelPath = rel,
                    Title = Path.GetFileNameWithoutExtension(rel),
                    SourceFile = ExtractSourceFiles(content).FirstOrDefault() ?? "",
                    WikiLinks = ExtractWikiTargets(content).Take(30).ToList()
                };
                notes.Add(note);
            }
        }
        catch { }

        return notes;
    }

    private static IEnumerable<VaultNote> SelectVaultNotes(List<VaultNote> notes)
    {
        var index = BuildVaultWikiIndex(notes);
        var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var note in notes)
        {
            if (note.WikiLinks.Count > 0 || !string.IsNullOrWhiteSpace(note.SourceFile))
                keep.Add(note.RelPath);

            foreach (var wl in note.WikiLinks)
                if (TryResolveVaultWiki(wl, index, out var target))
                    keep.Add(target.RelPath);
        }

        return notes.Where(n => keep.Contains(n.RelPath))
            .OrderBy(n => n.RelPath, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, VaultNote> BuildVaultWikiIndex(IEnumerable<VaultNote> notes)
    {
        var index = new Dictionary<string, VaultNote>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in notes)
        {
            foreach (var key in VaultWikiKeys(n))
                index.TryAdd(key, n);
        }
        return index;
    }

    private static IEnumerable<string> VaultWikiKeys(VaultNote note)
    {
        var noExt = NormalizeRelPath(Path.ChangeExtension(note.RelPath, null) ?? note.RelPath);
        yield return NormalizeWikiKey(noExt);
        yield return NormalizeWikiKey(note.RelPath);
        yield return NormalizeWikiKey(note.Title);
    }

    private static bool TryResolveVaultWiki(string target, Dictionary<string, VaultNote> byWikiKey, out VaultNote note)
    {
        note = default!;
        var key = NormalizeWikiKey(target);
        if (byWikiKey.TryGetValue(key, out var exact))
        {
            note = exact;
            return true;
        }

        if (byWikiKey.TryGetValue(NormalizeWikiKey(key + ".md"), out var byMd))
        {
            note = byMd;
            return true;
        }

        if (byWikiKey.TryGetValue(NormalizeWikiKey(Path.GetFileNameWithoutExtension(key)), out var byName))
        {
            note = byName;
            return true;
        }

        return false;
    }

    private static string NormalizeRelPath(string path)
    {
        return path.Replace("\\", "/").Trim().TrimStart('/');
    }

    private static string NormalizeWikiKey(string value)
    {
        return NormalizeRelPath(value)
            .Trim()
            .TrimStart('/')
            .TrimEnd('/')
            .Replace(".md", "", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocalFileSpec(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec)) return false;
        if (spec.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return false;
        if (spec.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
        if (spec.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return false;
        if (spec.StartsWith("#")) return false;
        return spec.Contains('/') || spec.Contains('\\') || Path.HasExtension(spec);
    }

    private static int SafeMtimeMin(string path)
    {
        try { return (int)Math.Min(99999, (DateTime.Now - File.GetLastWriteTime(path)).TotalMinutes); }
        catch { return 99999; }
    }

    public static string BuildJsonForRootCached(string projectRoot = ProjectRoot, int maxAgeSec = 300)
    {
        try
        {
            var cachePath = Path.Combine(projectRoot, ".checkpoints", ".brain-graph-cache.json");
            if (File.Exists(cachePath))
            {
                var age = DateTime.Now - File.GetLastWriteTime(cachePath);
                if (age.TotalSeconds < maxAgeSec)
                {
                    var cached = File.ReadAllText(cachePath);
                    if (CacheMatchesBuilderVersion(cached))
                        return cached;
                }
            }

            var json = BuildJsonForRoot(projectRoot);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllText(cachePath, json);
            }
            catch { }
            return json;
        }
        catch
        {
            return BuildJsonForRoot(projectRoot);
        }
    }

    private static bool CacheMatchesBuilderVersion(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("meta", out var meta) &&
                   meta.TryGetProperty("builderVersion", out var version) &&
                   string.Equals(version.GetString(), BuilderVersion, StringComparison.Ordinal);
        }
        catch { return false; }
    }
}
