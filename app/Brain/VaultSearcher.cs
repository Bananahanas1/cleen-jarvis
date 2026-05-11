using System.Text.RegularExpressions;

namespace JarvisClean;

// VaultSearcher — söker i F:\Jarvis-clean\vault\ för att hitta relevanta MD-noter
// som ska injiceras som AI-kontext innan Ollama-svar (BR2/BR6 i BRAIN_3D_SUPERPLAN).
//
// Strategi: enkel ord-frekvens-scoring med TF-koncept. Inte full TF-IDF —
// vi vill ha snabb (<100 ms) lookup på <500 noter, inte perfekt relevans.
// Cache: index byggs om om någon vault-fil ändrats sedan senaste indexbygge.
//
// Säkerhet: läser bara från VaultRoot. Returnerar trunkerade utdrag (max 600 tecken/not).
internal static class VaultSearcher
{
    private const string VaultRoot = @"F:\Jarvis-clean\vault";
    private const int MaxNoteSizeBytes = 200_000;
    private const int MaxExcerptChars = 600;
    private const int MaxContextChars = 4000; // hård cap per chat-request

    // Default ON enligt användarens val 2026-05-10. Toggle via /vault av / /vault på.
    public static bool AutoContextEnabled { get; set; } = true;

    private sealed class IndexedNote
    {
        public string RelPath = "";
        public string Title = "";
        public string Content = "";
        public Dictionary<string, int> Terms = new(StringComparer.OrdinalIgnoreCase);
        public DateTime LastWriteUtc;
    }

    private static readonly object _lock = new();
    private static List<IndexedNote> _index = new();
    private static DateTime _indexBuiltAt = DateTime.MinValue;
    private static DateTime _lastUsed = DateTime.MinValue;
    private static int _lastUsedNoteCount;
    // Senaste använda vault-noter (relativ path) — exponeras till JS för trail-glow
    private static List<string> _lastUsedRelPaths = new();
    public static IReadOnlyList<string> LastUsedRelPaths { get { lock (_lock) { return _lastUsedRelPaths.ToList(); } } }

    // Public status för Översikt-chip.
    public static (int totalNotes, DateTime lastUsed, int lastUsedCount, bool enabled) Status()
    {
        lock (_lock)
        {
            return (_index.Count, _lastUsed, _lastUsedNoteCount, AutoContextEnabled);
        }
    }

    // Bygger index om vaulten ändrats sedan senaste bygge.
    private static void EnsureIndexFresh()
    {
        lock (_lock)
        {
            if (!Directory.Exists(VaultRoot))
            {
                _index = new List<IndexedNote>();
                return;
            }

            // Snabbcheck: ändrad mtime på roten eller nyaste fil
            DateTime newest;
            try
            {
                newest = Directory.EnumerateFiles(VaultRoot, "*.md", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f).LastWriteTimeUtc)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
            }
            catch { newest = DateTime.MinValue; }

            if (_index.Count > 0 && newest <= _indexBuiltAt) return;

            _index = BuildIndex();
            _indexBuiltAt = DateTime.UtcNow;
        }
    }

    private static List<IndexedNote> BuildIndex()
    {
        var notes = new List<IndexedNote>();
        if (!Directory.Exists(VaultRoot)) return notes;
        try
        {
            foreach (var f in Directory.EnumerateFiles(VaultRoot, "*.md", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(f);
                    if (info.Length > MaxNoteSizeBytes || info.Length < 10) continue;
                    var content = File.ReadAllText(f);
                    var rel = Path.GetRelativePath(VaultRoot, f).Replace("\\", "/");
                    var note = new IndexedNote
                    {
                        RelPath = rel,
                        Title = Path.GetFileNameWithoutExtension(rel),
                        Content = content,
                        LastWriteUtc = info.LastWriteTimeUtc,
                        Terms = TokenizeAndCount(content)
                    };
                    notes.Add(note);
                }
                catch { }
            }
        }
        catch { }
        return notes;
    }

    // Plockar ut alla unicode-bokstäver (sv: åäö ingår) i lowercase. Hoppar över korta token.
    private static Dictionary<string, int> TokenizeAndCount(string text)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(text, @"[\p{L}][\p{L}\p{Nd}_-]{2,}"))
        {
            var w = m.Value.ToLowerInvariant();
            if (StopWords.Contains(w)) continue;
            counts[w] = counts.GetValueOrDefault(w, 0) + 1;
        }
        return counts;
    }

    // Begränsad svensk + engelsk stopp-lista (vanliga småord som blåser upp scoringen)
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "och","att","det","som","den","den","har","vad","jag","inte","med","för","till","kan","ska","vet","mig","dig","oss","är",
        "the","and","not","for","with","this","that","you","are","was","but","from","what","when","where","how","its","into","they"
    };

    // Returnerar topp-K noter rankade efter hur ofta query-orden förekommer.
    public static List<(string RelPath, string Title, string Excerpt, int Score)> TopMatches(string query, int k = 5)
    {
        EnsureIndexFresh();
        var queryTokens = TokenizeAndCount(query).Keys.ToList();
        if (queryTokens.Count == 0) return new();

        List<IndexedNote> snapshot;
        lock (_lock) { snapshot = _index.ToList(); }

        var scored = new List<(IndexedNote note, int score)>();
        foreach (var note in snapshot)
        {
            int score = 0;
            foreach (var qt in queryTokens)
            {
                if (note.Terms.TryGetValue(qt, out var c)) score += c;
                // Boost: query-ord i titel räknas extra (signal för relevans)
                if (note.Title.Contains(qt, StringComparison.OrdinalIgnoreCase)) score += 5;
            }
            if (score > 0) scored.Add((note, score));
        }

        return scored
            .OrderByDescending(x => x.score)
            .Take(k)
            .Select(x => (x.note.RelPath, x.note.Title, BuildExcerpt(x.note, queryTokens), x.score))
            .ToList();
    }

    // Plockar ut ett excerpt runt första query-träffen, max MaxExcerptChars.
    private static string BuildExcerpt(IndexedNote note, List<string> queryTokens)
    {
        var content = note.Content;
        // Hoppa över frontmatter --- ... ---
        if (content.StartsWith("---"))
        {
            var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (end > 0 && end < content.Length - 5) content = content.Substring(end + 4).TrimStart();
        }

        int firstHit = -1;
        foreach (var qt in queryTokens)
        {
            var idx = content.IndexOf(qt, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && (firstHit == -1 || idx < firstHit)) firstHit = idx;
        }
        if (firstHit < 0) firstHit = 0;

        var start = Math.Max(0, firstHit - 100);
        var len = Math.Min(MaxExcerptChars, content.Length - start);
        var excerpt = content.Substring(start, len).Trim();
        if (start > 0) excerpt = "..." + excerpt;
        if (start + len < content.Length) excerpt += "...";
        return excerpt;
    }

    // Bygger system-prompt-prefix att injicera. Returnerar tom sträng om auto-context är av.
    public static string BuildContextPrefix(string userQuery, int k = 5)
    {
        if (!AutoContextEnabled) return "";
        var matches = TopMatches(userQuery, k);
        if (matches.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Här är relevant kontext från Jarvis-vaulten (F:\\Jarvis-clean\\vault\\). " +
                      "Använd den för ditt svar men hänvisa bara om relevant — inte i varje svar.");
        sb.AppendLine();

        int totalChars = 0;
        int used = 0;
        var usedPaths = new List<string>();
        foreach (var (relPath, title, excerpt, score) in matches)
        {
            var block = $"--- {relPath} ---\n{excerpt}\n\n";
            if (totalChars + block.Length > MaxContextChars) break;
            sb.Append(block);
            totalChars += block.Length;
            used++;
            usedPaths.Add(relPath);
        }

        lock (_lock)
        {
            _lastUsed = DateTime.Now;
            _lastUsedNoteCount = used;
            _lastUsedRelPaths = usedPaths;
        }
        return sb.ToString();
    }

    // Tvingar omläsning av indexet (t.ex. efter /vault skapa).
    public static void InvalidateIndex()
    {
        lock (_lock)
        {
            _indexBuiltAt = DateTime.MinValue;
        }
    }
}
