using System.Text.Json;

namespace JarvisClean;

/// <summary>
/// Sprint 2a (2026-05-17) — laddar tool-definitions från data/agent/tools.json och
/// formaterar dem för Ollama native tool calling.
///
/// Tools.json är källan; ToolsRegistryV1.LoadAll() ger en lista av tool-records.
/// BuildOllamaTools() returnerar Ollama-API-formaterad JSON för system-prompt-injection.
/// </summary>
internal sealed record AgentToolDefinitionV1(
    string Name,
    string Description,
    string Risk,
    JsonElement Parameters);

internal static class ToolsRegistryV1
{
    public const string ToolsFileName = "tools.json";
    public const string AgentDataDir = "data/agent";

    public const string RiskSafe = "safe";
    public const string RiskNeedsApproval = "needs_approval";

    private static readonly object Lock = new();
    private static List<AgentToolDefinitionV1>? _cached;
    private static DateTime _cachedAt;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public static List<AgentToolDefinitionV1> LoadAll(string projectRoot)
    {
        lock (Lock)
        {
            if (_cached is not null && (DateTime.UtcNow - _cachedAt) < CacheTtl)
                return _cached;

            var path = Path.Combine(projectRoot, AgentDataDir, ToolsFileName);
            if (!File.Exists(path))
            {
                _cached = new List<AgentToolDefinitionV1>();
                _cachedAt = DateTime.UtcNow;
                return _cached;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("tools", out var toolsArr))
                {
                    _cached = new List<AgentToolDefinitionV1>();
                    _cachedAt = DateTime.UtcNow;
                    return _cached;
                }

                var list = new List<AgentToolDefinitionV1>();
                foreach (var t in toolsArr.EnumerateArray())
                {
                    var name = t.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var desc = t.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                    var risk = t.TryGetProperty("risk", out var r) ? r.GetString() ?? RiskSafe : RiskSafe;
                    var pars = t.TryGetProperty("parameters", out var p) ? p.Clone() : default;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    list.Add(new AgentToolDefinitionV1(name, desc, risk, pars));
                }

                _cached = list;
                _cachedAt = DateTime.UtcNow;
                return _cached;
            }
            catch
            {
                _cached = new List<AgentToolDefinitionV1>();
                _cachedAt = DateTime.UtcNow;
                return _cached;
            }
        }
    }

    public static AgentToolDefinitionV1? FindByName(string projectRoot, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return LoadAll(projectRoot)
            .FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Bygger Ollama-native-format för tools-array (samma som OpenAI function-calling):
    /// [{type:"function", function:{name, description, parameters}}, ...]
    /// </summary>
    public static string BuildOllamaToolsJson(string projectRoot)
    {
        var tools = LoadAll(projectRoot);
        var formatted = tools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.Parameters
            }
        }).ToList();
        return JsonSerializer.Serialize(formatted);
    }

    /// <summary>
    /// Systemprompt-tillägg som forklarar tool-loopen for LLM:n.
    /// Anvands tillsammans med vanlig system-prompt for chat-bots.
    /// </summary>
    public static string BuildAgentSystemPromptSegment(string projectRoot)
    {
        var tools = LoadAll(projectRoot);
        if (tools.Count == 0) return "";

        var lines = new List<string>
        {
            "Du har tillgång till verktyg (tools) som du kan kalla för att utföra uppgifter åt användaren.",
            "Använd verktyg när det behövs — t.ex. när användaren ber dig öppna en fil, söka info, köra ett kommando, eller agera på något.",
            "Kalla finish() när du är klar och har gett ett komplett svar.",
            "Verktyg med risk='needs_approval' kräver godkännande av användaren — du föreslår, användaren bekräftar.",
            "",
            "Tillgängliga verktyg:"
        };

        foreach (var t in tools)
        {
            var riskTag = t.Risk == RiskNeedsApproval ? " [GODKÄNNANDE]" : "";
            lines.Add("- " + t.Name + riskTag + ": " + t.Description);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
