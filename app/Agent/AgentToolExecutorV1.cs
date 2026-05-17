using System.Text.Json;

namespace JarvisClean;

internal sealed record AgentToolResultV1(string ResultJson, bool IsFinish, string UserVisibleText);

/// <summary>
/// Sprint 2b/2c (2026-05-17) — Executor som mappar tool-name -> handler-callback.
///
/// Handlers ges av JarvisForm via Register(). Det halls separat fran AgentLoopV1
/// sa loopen inte beror av WinForms-runtime.
///
/// Risky tools (write_file, run_terminal) gar via approval-handler som returnerar
/// "pending approval" till LLM. LLM kan da signalera klar eller forsoka annan tool.
/// </summary>
internal sealed class AgentToolExecutorV1
{
    private readonly string _projectRoot;
    private readonly Dictionary<string, Func<Dictionary<string, JsonElement>, Task<AgentToolResultV1>>> _handlers
        = new(StringComparer.OrdinalIgnoreCase);

    // Visual-output tracking: satts av tools som producerar nagot synligt i scen/karta/widget.
    // AgentLoop kollar detta post-loop for att avgora om ett auto-search_web behovs.
    public bool DidVisualOutput { get; private set; }

    // Tools som rakanas som visual-producerande.
    private static readonly HashSet<string> VisualTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "search_web", "search_news", "show_widget", "youtube_embed", "map_pin", "weather", "open_file", "navigate_panel"
    };

    public AgentToolExecutorV1(string projectRoot)
    {
        _projectRoot = projectRoot;
    }

    public void ResetVisualState() => DidVisualOutput = false;

    public void Register(string toolName, Func<Dictionary<string, JsonElement>, Task<AgentToolResultV1>> handler)
    {
        _handlers[toolName] = handler;
    }

    public async Task<AgentToolResultV1> ExecuteAsync(string toolName, Dictionary<string, JsonElement> args)
    {
        if (!_handlers.TryGetValue(toolName, out var handler))
        {
            var def = ToolsRegistryV1.FindByName(_projectRoot, toolName);
            var msg = def is null
                ? "Tool '" + toolName + "' finns inte i registry."
                : "Tool '" + toolName + "' ar inte implementerad an. Lagg till handler i JarvisForm.";
            return Error(msg);
        }

        try
        {
            var result = await handler(args);
            if (VisualTools.Contains(toolName) && !string.IsNullOrEmpty(result.ResultJson)
                && !result.ResultJson.Contains("\"ok\":false"))
            {
                DidVisualOutput = true;
            }
            return result;
        }
        catch (Exception ex)
        {
            return Error("Tool '" + toolName + "' kastade: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    public static AgentToolResultV1 Ok(object payload)
        => new(JsonSerializer.Serialize(new { ok = true, result = payload }), false, "");

    public static AgentToolResultV1 OkText(string text)
        => new(JsonSerializer.Serialize(new { ok = true, text = text }), false, "");

    public static AgentToolResultV1 Error(string message)
        => new(JsonSerializer.Serialize(new { ok = false, error = message }), false, "");

    public static AgentToolResultV1 Finish(string summary)
        => new(JsonSerializer.Serialize(new { ok = true, finished = true, summary = summary }), true, summary);

    public static AgentToolResultV1 PendingApproval(string toolName, string reason)
        => new(JsonSerializer.Serialize(new
        {
            ok = true,
            pending_approval = true,
            tool = toolName,
            reason = reason,
            note = "Anvandaren maste godkanna via approval-popup innan tooln korr."
        }), false, "");

    public static string GetString(Dictionary<string, JsonElement> args, string key, string fallback = "")
    {
        if (!args.TryGetValue(key, out var el)) return fallback;
        if (el.ValueKind == JsonValueKind.String) return el.GetString() ?? fallback;
        if (el.ValueKind == JsonValueKind.Number) return el.GetRawText();
        return el.ToString();
    }
}
