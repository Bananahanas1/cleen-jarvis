using System.Text.Json;
using System.Text.RegularExpressions;

namespace JarvisClean;

// FileGraphBuilder bygger en kombinerad fil-graf:
//  • Projektfiler i F:\Jarvis-clean\ (.cs, .js, .html, .css, .py, .md, .json)
//  • Obsidian-vault-noter (.md med [[wikilinks]])
//  • Edges:
//      - C# typnamn-referenser
//      - JS import / require / <script src=...>
//      - HTML script + link
//      - Python import (toleranta, hoppar över duplicerade __init__)
//      - Vault [[wikilinks]] mellan noter
//      - Vault frontmatter "source_file:" → projekt-fil (cross-link!)
//
// Bug fix 2026-05-10: Tidigare ToDictionary på Path.GetFileNameWithoutExtension kraschade
// pga Python-paketens många __init__.py-filer. Nu använder vi ILookup.
internal static class FileGraphBuilder
{
    private const string ProjectRoot = @"F:\Jarvis-clean";
    // Vault-scope per användarens val 2026-05-10: bara F:\Jarvis-clean\vault\
    // (gamla F:\New project\obsidian-vault\ är read-only-referens, inte aktiv)
    private static readonly string[] VaultPaths = new[]
    {
        @"F:\Jarvis-clean\vault"
    };

    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".js", ".html", ".css", ".py", ".md", ".json"
    };
    private static readonly HashSet<string> ExcludedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget",
        "backups", ".tmp", ".checkpoints", "build-verify", "github-brain",
        "third_party", "__pycache__", "vendor"
    };
    private const long MaxFileSizeBytes = 200_000;
    private const int MaxProjectNodes = 300;
    // Vault är 3694 noter — vi cap'ar till de mest kopplade ([[wikilinks]] eller source_file matching)
    private const int MaxVaultNodes = 250;

    internal sealed class Node
    {
        public string id { get; set; } = "";
        public string path { get; set; } = "";
        public string label { get; set; } = "";
        public string kind { get; set; } = "";
        public string source { get; set; } = ""; // "project" eller "vault"
        public int degree { get; set; }
        // Minuter sedan filen ändrades — JS visar orange ring om < 60 min
        public int mtimeMin { get; set; } = 99999;
    }

    internal sealed class Edge
    {
        public string source { get; set; } = "";
        public string target { get; set; } = "";
        public string kind { get; set; } = "";
    }

    internal sealed class GraphPayload
    {
        public List<Node> nodes { get; set; } = new();
        public List<Edge> edges { get; set; } = new();
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

        // === 1. Projekt-filer ===
        if (Directory.Exists(projectRoot))
        {
            foreach (var f in EnumerateSafe(projectRoot))
            {
                if (nodes.Count >= MaxProjectNodes) break;
                var rel = Path.GetRelativePath(projectRoot, f).Replace("\\", "/");
                if (rel.Split('/').Any(p => ExcludedDirs.Contains(p))) continue;
                var ext = Path.GetExtension(f);
                if (!SourceExtensions.Contains(ext)) continue;
                try { if (new FileInfo(f).Length > MaxFileSizeBytes) continue; }
                catch { continue; }

                var id = "proj:" + rel;
                int mtimeMin;
                try { mtimeMin = (int)Math.Min(99999, (DateTime.Now - new FileInfo(f).LastWriteTime).TotalMinutes); }
                catch { mtimeMin = 99999; }
                var node = new Node
                {
                    id = id,
                    path = rel,
                    label = Path.GetFileName(rel),
                    kind = ext.TrimStart('.').ToLowerInvariant(),
                    source = "project",
                    mtimeMin = mtimeMin
                };
                nodes.Add(node);
                nodeById[id] = node;
            }
        }

        // Sökindex: filename-without-ext → list of project nodes (for C# typnamn-matchning).
        // Använder ToLookup vilket hanterar dubletter (t.ex. flera __init__.py) utan att krascha.
        var projectByName = nodes.Where(n => n.source == "project")
            .ToLookup(n => Path.GetFileNameWithoutExtension(n.path), StringComparer.OrdinalIgnoreCase);
        var projectByRelPath = nodes.Where(n => n.source == "project")
            .ToDictionary(n => n.path, n => n, StringComparer.OrdinalIgnoreCase);

        // Skanna varje projekt-fil för internal edges.
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
            }
        }

        // === 2. Vault-noter ===
        // Vi inkluderar bara noter som har minst en wikilink ELLER en source_file som matchar projekt.
        // Det håller graph-storleken hanterbar och fokuserar på meningsfulla connections.
        var vaultRaw = ScanVault();
        // Filtrera till noter med wikilinks eller source-koppling
        var keep = vaultRaw.Where(v => v.WikiLinks.Count > 0 || !string.IsNullOrEmpty(v.SourceFile))
            .Take(MaxVaultNodes).ToList();

        var vaultById = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in keep)
        {
            var id = "vault:" + v.RelPath;
            int mtimeMin;
            try
            {
                var abs = Path.Combine(@"F:\Jarvis-clean\vault", v.RelPath.Replace("/", "\\"));
                mtimeMin = (int)Math.Min(99999, (DateTime.Now - File.GetLastWriteTime(abs)).TotalMinutes);
            }
            catch { mtimeMin = 99999; }
            var node = new Node
            {
                id = id,
                path = v.RelPath,
                label = v.Title,
                kind = "vault",
                source = "vault",
                mtimeMin = mtimeMin
            };
            nodes.Add(node);
            nodeById[id] = node;
            vaultById[v.Title] = node; // index by title för wikilink-resolve
        }

        // Vault-edges: wikilinks mellan vault-noter + source_file → projekt
        foreach (var v in keep)
        {
            var srcId = "vault:" + v.RelPath;
            // Wikilinks
            foreach (var wl in v.WikiLinks)
            {
                if (vaultById.TryGetValue(wl, out var target))
                    AddEdge(srcId, target.id, "vault-link", edges, edgeSeen);
            }
            // Source-file cross-link
            if (!string.IsNullOrEmpty(v.SourceFile))
            {
                // source_file kan vara t.ex. "AI_HANDOFF/AI_WORKFLOW.md" — försök hitta i projekt
                var sf = v.SourceFile.Replace("\\", "/");
                if (projectByRelPath.TryGetValue(sf, out var projNode))
                    AddEdge(srcId, projNode.id, "vault-source", edges, edgeSeen);
            }
        }

        // 3. Beräkna degree
        foreach (var e in edges)
        {
            if (nodeById.TryGetValue(e.source, out var s)) s.degree++;
            if (nodeById.TryGetValue(e.target, out var t)) t.degree++;
        }

        return new GraphPayload { nodes = nodes, edges = edges };
    }

    private static IEnumerable<string> EnumerateSafe(string root)
    {
        try { return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories); }
        catch { return Enumerable.Empty<string>(); }
    }

    private static void AddEdge(string source, string target, string kind, List<Edge> edges, HashSet<string> seen)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return;
        if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase)) return;
        if (!seen.Add(source + "→" + target + ":" + kind)) return;
        edges.Add(new Edge { source = source, target = target, kind = kind });
    }

    private static void ScanCsFile(Node node, string content, ILookup<string, Node> projectByName, List<Edge> edges, HashSet<string> seen)
    {
        // PascalCase identifiers ≥4 tecken som matchar fil-basnamn → relation
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
        {
            ResolveJsSpec(node, m.Groups[1].Value, byRelPath, edges, seen);
        }
    }

    private static void ResolveJsSpec(Node node, string spec, Dictionary<string, Node> byRelPath, List<Edge> edges, HashSet<string> seen)
    {
        if (!spec.StartsWith(".") && !spec.StartsWith("/")) return;
        var nodeDir = Path.GetDirectoryName(node.path)?.Replace("\\", "/") ?? "";
        var combined = Path.GetFullPath(Path.Combine("/", nodeDir, spec.Trim('/')));
        var asRel = combined.Replace("\\", "/").TrimStart('/');
        foreach (var candidate in new[] { asRel, asRel + ".js", asRel + "/index.js" })
        {
            if (byRelPath.TryGetValue(candidate, out var t))
            { AddEdge(node.id, t.id, "js-import", edges, seen); return; }
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

    // === Vault scanning ===
    private sealed class VaultNote
    {
        public string RelPath { get; set; } = "";
        public string Title { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public List<string> WikiLinks { get; set; } = new();
    }

    // Hård cap: scanna max så här många vault-filer totalt innan vi ger upp.
    // Med 3694 noter i F:\New project\obsidian-vault tog full skanning 29s — för långt.
    private const int MaxVaultFilesToScan = 600;

    private static List<VaultNote> ScanVault()
    {
        var notes = new List<VaultNote>();
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var vaultRoot in VaultPaths)
        {
            if (!Directory.Exists(vaultRoot)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(vaultRoot, "*.md", SearchOption.AllDirectories))
                {
                    if (notes.Count >= MaxVaultFilesToScan) break;
                    var rel = Path.GetRelativePath(vaultRoot, f).Replace("\\", "/");
                    var title = Path.GetFileNameWithoutExtension(rel);
                    // Snabbskanna: hoppa över filer utan [[ i namnet eller relevanta tecken
                    // (vault innehåller t.ex. ".applyMatrix4()_10.md" — Three.js-API-doc, trolig referens)
                    string content;
                    try
                    {
                        var info = new FileInfo(f);
                        if (info.Length > MaxFileSizeBytes) continue;
                        if (info.Length < 30) continue; // tom fil — hoppa
                        content = File.ReadAllText(f);
                    }
                    catch { continue; }

                    // Snabb pre-check: bara filer som innehåller [[ eller source_file: tas in
                    if (content.IndexOf("[[", StringComparison.Ordinal) < 0 &&
                        content.IndexOf("source_file:", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // Dedupa titlar så vi inte får 50 ".applyMatrix4()" som dominerar.
                    if (!seenTitles.Add(title)) continue;

                    var note = new VaultNote { RelPath = rel, Title = title };

                    var fmMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---", RegexOptions.Singleline);
                    if (fmMatch.Success)
                    {
                        var fm = fmMatch.Groups[1].Value;
                        var sfMatch = Regex.Match(fm, @"source_file:\s*['""]?([^'""\n]+)['""]?");
                        if (sfMatch.Success) note.SourceFile = sfMatch.Groups[1].Value.Trim();
                    }

                    foreach (Match m in Regex.Matches(content, @"\[\[([^\]\|]+)(?:\|[^\]]*)?\]\]"))
                    {
                        var target = m.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(target) && note.WikiLinks.Count < 30)
                            note.WikiLinks.Add(target);
                    }

                    notes.Add(note);
                }
            }
            catch { }
        }
        return notes;
    }

    // Cache: graf byggs en gång per session, sparas till .checkpoints/.brain-graph-cache.json.
    // Användarens build/check-fil-tider används som invalidation-trigger om vi vill ha färsk graf.
    public static string BuildJsonForRootCached(string projectRoot = ProjectRoot, int maxAgeSec = 300)
    {
        try
        {
            var cachePath = Path.Combine(projectRoot, ".checkpoints", ".brain-graph-cache.json");
            if (File.Exists(cachePath))
            {
                var age = DateTime.Now - File.GetLastWriteTime(cachePath);
                if (age.TotalSeconds < maxAgeSec)
                    return File.ReadAllText(cachePath);
            }
            var json = BuildJsonForRoot(projectRoot);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                File.WriteAllText(cachePath, json);
            }
            catch { /* cache är best-effort */ }
            return json;
        }
        catch
        {
            return BuildJsonForRoot(projectRoot);
        }
    }
}
