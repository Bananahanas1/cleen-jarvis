namespace JarvisClean;

// Describes a local Ollama model profile for the model-switching system.
public record ModelProfile(string Name, string Description, string Role);

// Registry of known Ollama models for Jarvis-clean. Profiles are local och kostar inget att byta mellan.
// Användaren kan också skriva valfritt rått modellnamn till Ollama oavsett listan här.
//
// Porterat från F:\New project\app\Core\ModelCatalog.cs som del av UNIFICATION_PLAN Fas 7.
public static class ModelCatalog
{
    public static readonly ModelProfile Fast    = new("qwen3:1.7b",       "Snabb och lätt. Bra för enkel chat.",                       "fast");
    public static readonly ModelProfile Smart   = new("qwen3:8b",         "Stark allmänmodell. Bra för komplexa samtal och planering.", "smart");
    public static readonly ModelProfile Coder   = new("qwen2.5-coder:7b", "Kodspecialiserad. Bra för implementationsuppgifter.",        "code");
    public static readonly ModelProfile Reason  = new("deepseek-r1:7b",   "Djup resonering. Bra för analys och planering.",            "reason");
    public static readonly ModelProfile General = new("llama3.1:8b",      "Generell modell. Balans mellan prestanda och kvalitet.",     "general");

    public static readonly ModelProfile[] All = new[] { Fast, Smart, Coder, Reason, General };

    // Find a profile by exact name eller role (case-insensitive).
    public static ModelProfile? FindByNameOrRole(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;
        var q = query.Trim();
        return All.FirstOrDefault(m =>
            m.Name.Equals(q, StringComparison.OrdinalIgnoreCase) ||
            m.Role.Equals(q, StringComparison.OrdinalIgnoreCase));
    }

    // Auto-upgrade: när användaren är på fast-modellen och en kod-task kommer, använd coder för det anropet.
    // Andra aktiva modeller = explicit val, respekteras.
    public static string SelectForCodeTask(string activeModel) =>
        string.Equals(activeModel, Fast.Name, StringComparison.OrdinalIgnoreCase) ? Coder.Name : activeModel;

    public static string FormatList()
    {
        return "Modellprofiler:\n" + string.Join("\n", All.Select(m =>
            $"- {m.Name}  [{m.Role}]  — {m.Description}"));
    }
}
