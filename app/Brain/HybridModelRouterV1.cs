namespace JarvisClean;

internal enum HybridModelModeV1
{
    LocalOnly,
    AutoFree
}

internal enum HybridProviderKindV1
{
    Ollama,
    Groq,
    Gemini,
    GitHubModels
}

internal sealed record HybridProviderStatusV1(
    HybridProviderKindV1 Provider,
    string DisplayName,
    bool IsConfigured,
    string KeySource,
    string DefaultModel);

internal sealed record HybridModelDecisionV1(
    HybridProviderKindV1 Provider,
    string DisplayName,
    string Model,
    string Reason,
    bool IsOnline);

internal static class HybridModelRouterV1
{
    public const string ModeConfigFileName = "model_provider_mode.txt";
    public const string GroqApiKeyEnvironmentName = "GROQ_API_KEY";
    public const string GeminiApiKeyEnvironmentName = "GEMINI_API_KEY";
    public const string GitHubTokenEnvironmentName = "GITHUB_TOKEN";

    public static HybridModelModeV1 ParseMode(string? raw)
    {
        var value = (raw ?? string.Empty).Trim().ToLowerInvariant();
        return value is "auto" or "autofree" or "auto-free" or "gratis" or "online"
            ? HybridModelModeV1.AutoFree
            : HybridModelModeV1.LocalOnly;
    }

    public static string PersistedValue(HybridModelModeV1 mode) =>
        mode == HybridModelModeV1.AutoFree ? "auto-free" : "local";

    public static string ModeLabel(HybridModelModeV1 mode) =>
        mode == HybridModelModeV1.AutoFree ? "Auto gratis/online" : "Lokal Ollama";

    public static IReadOnlyList<HybridProviderStatusV1> ProviderStatuses()
    {
        return new[]
        {
            new HybridProviderStatusV1(HybridProviderKindV1.Ollama, "Ollama lokal", true, "localhost:11434", "aktiv lokal modell"),
            new HybridProviderStatusV1(HybridProviderKindV1.Groq, "Groq Free", IsConfigured(GroqApiKeyEnvironmentName), GroqApiKeyEnvironmentName, GetModel("JARVIS_GROQ_MODEL", "llama-3.1-8b-instant")),
            new HybridProviderStatusV1(HybridProviderKindV1.Gemini, "Gemini API Free", IsConfigured(GeminiApiKeyEnvironmentName), GeminiApiKeyEnvironmentName, GetModel("JARVIS_GEMINI_MODEL", "gemini-2.5-flash-lite")),
            new HybridProviderStatusV1(HybridProviderKindV1.GitHubModels, "GitHub Models", IsConfigured(GitHubTokenEnvironmentName), GitHubTokenEnvironmentName, GetModel("JARVIS_GITHUB_MODEL", "gpt-4o-mini"))
        };
    }

    public static HybridModelDecisionV1 Pick(string userText, HybridModelModeV1 mode, string activeOllamaModel)
    {
        if (mode == HybridModelModeV1.LocalOnly)
            return new HybridModelDecisionV1(HybridProviderKindV1.Ollama, "Ollama lokal", activeOllamaModel, "lage: lokal", false);

        var providers = ProviderStatuses();
        var groq = providers.First(p => p.Provider == HybridProviderKindV1.Groq);
        if (groq.IsConfigured)
            return new HybridModelDecisionV1(HybridProviderKindV1.Groq, groq.DisplayName, groq.DefaultModel, "auto-free: Groq ar konfigurerad", true);

        var gemini = providers.First(p => p.Provider == HybridProviderKindV1.Gemini);
        if (gemini.IsConfigured)
            return new HybridModelDecisionV1(HybridProviderKindV1.Gemini, gemini.DisplayName, gemini.DefaultModel, "auto-free: Gemini ar konfigurerad", true);

        var github = providers.First(p => p.Provider == HybridProviderKindV1.GitHubModels);
        if (github.IsConfigured)
            return new HybridModelDecisionV1(HybridProviderKindV1.GitHubModels, github.DisplayName, github.DefaultModel, "auto-free: GitHub Models ar konfigurerad", true);

        return new HybridModelDecisionV1(HybridProviderKindV1.Ollama, "Ollama lokal", activeOllamaModel, "auto-free fallback: ingen gratisnyckel hittad", false);
    }

    public static string FormatStatus(HybridModelModeV1 mode, string activeOllamaModel)
    {
        var decision = Pick("", mode, activeOllamaModel);
        var lines = new List<string>
        {
            "HybridModelRouterV1:",
            "- Lage: " + ModeLabel(mode),
            "- Aktiv backend: " + decision.DisplayName + " (" + decision.Model + ")",
            "- Orsak: " + decision.Reason,
            "",
            "Providers:"
        };

        foreach (var provider in ProviderStatuses())
        {
            lines.Add("- " + provider.DisplayName + ": " + (provider.IsConfigured ? "konfigurerad" : "saknar env") +
                      " | source: " + provider.KeySource + " | model: " + provider.DefaultModel);
        }

        lines.Add("");
        lines.Add("Regel: LLM tolkar och foreslar. Jarvis utfor via lokala tools och pending approval.");
        return string.Join(Environment.NewLine, lines);
    }

    public static string GetApiKey(HybridProviderKindV1 provider)
    {
        var env = provider switch
        {
            HybridProviderKindV1.Groq => GroqApiKeyEnvironmentName,
            HybridProviderKindV1.Gemini => GeminiApiKeyEnvironmentName,
            HybridProviderKindV1.GitHubModels => GitHubTokenEnvironmentName,
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(env)
            ? string.Empty
            : (Environment.GetEnvironmentVariable(env) ?? string.Empty).Trim();
    }

    private static bool IsConfigured(string environmentName) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentName));

    private static string GetModel(string environmentName, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(environmentName);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
