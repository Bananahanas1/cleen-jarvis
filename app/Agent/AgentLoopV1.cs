using System.Net.Http.Json;
using System.Text.Json;

namespace JarvisClean;

/// <summary>
/// Sprint 2b (2026-05-17) — Agentic tool-loop for Jarvis.
///
/// Pratar med Ollama via /api/chat och anger `tools`-array.
/// Ollama returnerar `message.tool_calls` i sina svar. AgentLoop:
///   1. Skickar user-meddelande med tool-definitions
///   2. Om LLM returnerar tool_calls -> kor varje tool via AgentToolExecutorV1
///   3. Skickar tool-resultatet tillbaka som role="tool" message
///   4. Loopar tills LLM svarar med vanlig content (utan tool_calls) ELLER finish() kallas
///   5. Max iterations = 5 (anti-infinite-loop)
/// </summary>
internal sealed class AgentLoopV1
{
    public const int MaxIterations = 5;

    private readonly string _projectRoot;
    private readonly string _ollamaUrl;
    private readonly string _model;
    private readonly HttpClient _http;
    private readonly AgentToolExecutorV1 _executor;
    private readonly Func<string, Task> _onAssistantText;
    private readonly Action<string> _logger;

    public AgentLoopV1(
        string projectRoot,
        string ollamaUrl,
        string model,
        HttpClient http,
        AgentToolExecutorV1 executor,
        Func<string, Task> onAssistantText,
        Action<string>? logger = null)
    {
        _projectRoot = projectRoot;
        _ollamaUrl = ollamaUrl;
        _model = model;
        _http = http;
        _executor = executor;
        _onAssistantText = onAssistantText;
        _logger = logger ?? (_ => { });
    }

    public async Task RunAsync(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;

        var toolsJson = ToolsRegistryV1.BuildOllamaToolsJson(_projectRoot);
        var toolsPrompt = ToolsRegistryV1.BuildAgentSystemPromptSegment(_projectRoot);

        var systemPrompt =
            "Du är Jarvis — en agentisk AI-assistent som pratar svenska. Din scen-panel är " +
            "användarens cinematic workspace där du presenterar bilder, data, källor, widgets, " +
            "video, kamera och kontext för allt användaren frågar om.\n\n" +
            "VIKTIGA REGLER:\n" +
            "1. Säg ALDRIG 'jag kan inte', 'jag är bara en språkmodell', eller 'jag har inte tillgång till X'. " +
            "Du har verktyg — använd dem.\n" +
            "2. Användaren talar naturligt språk, inte kommandon. Lista ut vad de vill, kombinera verktyg om det behövs.\n" +
            "3. För INFORMATIONS-frågor (vad är, vem är, visa, berätta om, senaste nytt om...): " +
            "kalla ALLTID `search_web` — det byter till scen-panelen och presenterar AUTOMATISKT en " +
            "multi-widget-scen med sammanfattning, bilder och källor (en widget per del). Användaren " +
            "ska aldrig se ett 'stort hero-kort' utan flera mindre widgets sammanvävda.\n" +
            "4. För VISUELLA önskemål (visa kamera, spela X, öppna kalkylator, embed en sida): kalla " +
            "`show_widget`. Kombinera flera widgets om frågan kräver: t.ex. om användaren ber om " +
            "musik + chat, skapa både iframe-widget för Spotify OCH chat-mini-widget.\n" +
            "5. För NAVIGATION (visa karta, gå till brain, öppna fil X): kalla `navigate_panel` eller `open_file`.\n" +
            "6. Var PROAKTIV: presentera bilder, källor, data utan att vänta på att bli ombedd.\n" +
            "7. Kalla `finish` med kort svensk sammanfattning när du är klar.\n" +
            "8. Om du inte vet exakt vad användaren vill — gissa rimligt och kalla ett verktyg som är troligt rätt. " +
            "Försök först, justera sen om det blev fel.\n" +
            "9. KORTA ACKNOWLEDGMENTS som 'tack', 'ok', 'bra', 'okej', 'mm', 'aha' triggar INTE search_web. " +
            "Svara bara kort och kalla finish.\n" +
            "10. Din chat-text och scen-presentationen ska MATCHA. Om du svarar med text om ett ämne, " +
            "kalla search_web på samma ämne så scen-widgets backar upp ditt svar med bilder och källor. " +
            "Skicka inte search_web på fel ämne. Tänk: 'vad svarar jag?' → 'vilken sökterm matchar det?'.\n\n" +
            toolsPrompt;

        var messages = new List<Dictionary<string, object?>>
        {
            new() { ["role"] = "system", ["content"] = systemPrompt },
            new() { ["role"] = "user", ["content"] = userText }
        };

        Log("loop start: user='" + Truncate(userText, 80) + "' model=" + _model);

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            Log("iter " + iter + ": calling ollama");
            var resp = await CallOllamaAsync(messages, toolsJson);
            if (resp is null) return;

            var (assistantText, toolCalls) = ParseResponse(resp.Value);
            Log("  resp text='" + Truncate(assistantText, 80) + "' tool_calls=" + (toolCalls?.Count ?? 0));

            // Lagg assistant-message i historiken (med eventuella tool_calls).
            var assistantMsg = new Dictionary<string, object?>
            {
                ["role"] = "assistant",
                ["content"] = assistantText ?? ""
            };
            if (toolCalls is { Count: > 0 }) assistantMsg["tool_calls"] = toolCalls;
            messages.Add(assistantMsg);

            // Om LLM gav synlig text utan tool_calls -> visa for user och avsluta.
            if ((toolCalls is null || toolCalls.Count == 0) && !string.IsNullOrWhiteSpace(assistantText))
            {
                await _onAssistantText(assistantText);
                return;
            }

            // Inga tool-calls och ingen text -> tomt svar, avsluta tyst.
            if (toolCalls is null || toolCalls.Count == 0)
            {
                Log("  empty response, ending loop");
                return;
            }

            // Kor varje tool och samla resultat.
            bool finishCalled = false;
            foreach (var call in toolCalls)
            {
                var name = ExtractToolName(call);
                var args = ExtractToolArgs(call);
                if (string.IsNullOrWhiteSpace(name)) continue;

                Log("  tool: " + name + " args=" + JsonSerializer.Serialize(args));
                var result = await _executor.ExecuteAsync(name, args);
                Log("  -> result=" + Truncate(result.ResultJson, 120));

                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "tool",
                    ["content"] = result.ResultJson
                });

                if (result.IsFinish)
                {
                    if (!string.IsNullOrWhiteSpace(result.UserVisibleText))
                        await _onAssistantText(result.UserVisibleText);
                    finishCalled = true;
                    break;
                }
            }

            if (finishCalled)
            {
                Log("  finish called, ending loop");
                return;
            }
        }

        Log("loop ended: max iterations reached (" + MaxIterations + ")");
        await _onAssistantText("(Agent: max " + MaxIterations + " iterationer nåddes utan finish.)");
    }

    private async Task<JsonElement?> CallOllamaAsync(List<Dictionary<string, object?>> messages, string toolsJson)
    {
        try
        {
            var toolsEl = JsonDocument.Parse(toolsJson).RootElement.Clone();
            var payload = new Dictionary<string, object?>
            {
                ["model"] = _model,
                ["messages"] = messages,
                ["stream"] = false,
                ["keep_alive"] = "30m",
                ["tools"] = toolsEl
            };
            using var resp = await _http.PostAsJsonAsync(_ollamaUrl, payload);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                Log("ollama HTTP " + (int)resp.StatusCode + ": " + Truncate(body, 200));
                return null;
            }
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            Log("ollama call EXCEPTION: " + ex.Message);
            return null;
        }
    }

    private static (string text, List<JsonElement>? toolCalls) ParseResponse(JsonElement root)
    {
        var text = "";
        List<JsonElement>? calls = null;
        if (root.TryGetProperty("message", out var msg))
        {
            if (msg.TryGetProperty("content", out var c)) text = c.GetString() ?? "";
            if (msg.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
            {
                calls = new List<JsonElement>();
                foreach (var call in tc.EnumerateArray()) calls.Add(call.Clone());
            }
        }

        // Fallback: vissa modeller (qwen2.5-coder, gemma m.fl.) returnerar tool-calls som
        // JSON-TEXT i content istället för i tool_calls-array. Vi parsar manuellt sa
        // tool-loopen funkar även med dessa modeller. Format vi accepterar:
        //   {"name":"tool","arguments":{...}}
        //   {"name":"tool","parameters":{...}}
        //   {"function":"tool","arguments":{...}}
        //   tool_name({"arg":"val"})           // function-call syntax
        if ((calls is null || calls.Count == 0) && !string.IsNullOrWhiteSpace(text))
        {
            var parsed = TryParseInlineToolCalls(text);
            if (parsed.calls.Count > 0)
            {
                calls = parsed.calls;
                text = parsed.cleanedText;
            }
        }
        return (text, calls);
    }

    private static (List<JsonElement> calls, string cleanedText) TryParseInlineToolCalls(string text)
    {
        var calls = new List<JsonElement>();
        var working = text.Trim();

        // Forsok 1: hela texten ar ett JSON-object med "name" + "arguments".
        if (working.StartsWith("{") && working.EndsWith("}"))
        {
            if (TryNormalizeJsonToolCall(working, out var normalized))
            {
                calls.Add(normalized);
                return (calls, "");
            }
        }

        // Forsok 2: hitta JSON-block inom texten — t.ex. modellen sager "Jag kommer kalla {...}".
        var depth = 0; int start = -1;
        for (int i = 0; i < working.Length; i++)
        {
            var ch = working[i];
            if (ch == '{') { if (depth == 0) start = i; depth++; }
            else if (ch == '}') { depth--; if (depth == 0 && start >= 0) {
                var block = working.Substring(start, i - start + 1);
                if (TryNormalizeJsonToolCall(block, out var norm))
                    calls.Add(norm);
                start = -1;
            } }
        }
        if (calls.Count > 0)
            return (calls, "");

        // Forsok 3: function-call syntax  "tool_name({...})".
        var m = System.Text.RegularExpressions.Regex.Match(working,
            @"(?<n>[a-zA-Z_][a-zA-Z0-9_]*)\s*\((?<a>\{[\s\S]*\})\s*\)");
        if (m.Success)
        {
            var fakeJson = "{\"name\":\"" + m.Groups["n"].Value + "\",\"arguments\":" + m.Groups["a"].Value + "}";
            if (TryNormalizeJsonToolCall(fakeJson, out var norm))
            {
                calls.Add(norm);
                return (calls, "");
            }
        }

        return (calls, text);
    }

    private static bool TryNormalizeJsonToolCall(string json, out JsonElement normalized)
    {
        normalized = default;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string? name = null;
            JsonElement? args = null;

            if (root.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                name = n.GetString();
            else if (root.TryGetProperty("function", out var f))
            {
                if (f.ValueKind == JsonValueKind.String) name = f.GetString();
                else if (f.ValueKind == JsonValueKind.Object && f.TryGetProperty("name", out var fn))
                    name = fn.GetString();
            }
            if (name is null) return false;

            if (root.TryGetProperty("arguments", out var a1)) args = a1;
            else if (root.TryGetProperty("parameters", out var a2)) args = a2;
            else if (root.TryGetProperty("args", out var a3)) args = a3;
            else if (root.TryGetProperty("function", out var f2) && f2.TryGetProperty("arguments", out var a4)) args = a4;

            var rebuiltJson = JsonSerializer.Serialize(new
            {
                function = new
                {
                    name = name,
                    arguments = args.HasValue ? JsonDocument.Parse(args.Value.GetRawText()).RootElement : JsonDocument.Parse("{}").RootElement
                }
            });
            using var rebuiltDoc = JsonDocument.Parse(rebuiltJson);
            normalized = rebuiltDoc.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractToolName(JsonElement call)
    {
        if (call.TryGetProperty("function", out var fn) &&
            fn.TryGetProperty("name", out var n))
            return n.GetString() ?? "";
        if (call.TryGetProperty("name", out var nDirect))
            return nDirect.GetString() ?? "";
        return "";
    }

    private static Dictionary<string, JsonElement> ExtractToolArgs(JsonElement call)
    {
        var args = new Dictionary<string, JsonElement>();
        JsonElement argsEl = default;
        bool found = false;
        if (call.TryGetProperty("function", out var fn))
        {
            if (fn.TryGetProperty("arguments", out var a)) { argsEl = a; found = true; }
        }
        if (!found && call.TryGetProperty("arguments", out var a2))
        {
            argsEl = a2; found = true;
        }
        if (!found) return args;

        // Ollama kan ge arguments som JSON-object eller som JSON-string. Hantera bagge.
        if (argsEl.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var doc = JsonDocument.Parse(argsEl.GetString() ?? "{}");
                foreach (var p in doc.RootElement.EnumerateObject())
                    args[p.Name] = p.Value.Clone();
            }
            catch { }
        }
        else if (argsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in argsEl.EnumerateObject())
                args[p.Name] = p.Value.Clone();
        }
        return args;
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    private void Log(string message)
    {
        try
        {
            var dir = Path.Combine(_projectRoot, "data", "agent");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "loop-debug.log"),
                "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + message + Environment.NewLine);
        }
        catch { }
        _logger(message);
    }
}
