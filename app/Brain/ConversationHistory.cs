namespace JarvisClean;

// Multi-turn samtals-historik för Ollama. Sliding window: max MaxTurns turns,
// max MaxTotalChars tecken total. Trimmar äldsta först.
//
// Används av AskOllamaAsync så Jarvis "kommer ihåg" tidigare meddelanden.
// Slash: /historik visar, /glöm samtal rensar.
public static class ConversationHistory
{
    private const int MaxTurns = 20;
    private const int MaxTotalChars = 8000;

    private static readonly object _lock = new();
    private static readonly List<(string Role, string Content)> _turns = new();

    public static void AddUser(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_lock)
        {
            _turns.Add(("user", text.Trim()));
            Trim();
        }
    }

    public static void AddAssistant(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_lock)
        {
            _turns.Add(("assistant", text.Trim()));
            Trim();
        }
    }

    // Returnerar hela historiken som Ollama-message-objekt (utan system-prompt).
    public static List<object> AsOllamaMessages()
    {
        lock (_lock)
        {
            return _turns.Select(t => (object)new { role = t.Role, content = t.Content }).ToList();
        }
    }

    public static int Count
    {
        get { lock (_lock) return _turns.Count; }
    }

    public static int TotalChars
    {
        get { lock (_lock) return _turns.Sum(t => t.Content.Length); }
    }

    public static void Clear()
    {
        lock (_lock) _turns.Clear();
    }

    public static string FormatForChat()
    {
        lock (_lock)
        {
            if (_turns.Count == 0) return "Ingen samtals-historik.";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Samtals-historik (" + _turns.Count + " turns, " + TotalChars + " tecken):");
            sb.AppendLine();
            for (int i = 0; i < _turns.Count; i++)
            {
                var t = _turns[i];
                var preview = t.Content.Length > 120 ? t.Content.Substring(0, 120) + "..." : t.Content;
                sb.AppendLine((i + 1) + ". [" + t.Role + "] " + preview);
            }
            return sb.ToString();
        }
    }

    private static void Trim()
    {
        // Hård cap på antal turns — ta bort äldsta
        while (_turns.Count > MaxTurns) _turns.RemoveAt(0);

        // Mjuk cap på tecken — ta bort äldsta tills vi är under
        var total = _turns.Sum(t => t.Content.Length);
        while (total > MaxTotalChars && _turns.Count > 4)
        {
            total -= _turns[0].Content.Length;
            _turns.RemoveAt(0);
        }
    }
}
