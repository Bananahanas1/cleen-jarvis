using System.Text;
using System.Text.Json;

namespace JarvisClean;

// OllamaAgentHarness — multi-step tool-calling-agent mot lokal Ollama.
// Fas 6: READ-ONLY-version. Endast läsverktyg är aktiva. Skrivverktyg
// (write_file, replace_in_file, run_command) blockeras tills Fas 8 har integrerat
// dem med PendingApprovalV1 i main JarvisForm.
//
// Säkerhetsmål:
// - Agenten ska aldrig kunna ändra filer utanför PendingApprovalV1-flödet.
// - Agenten ska aldrig kunna köra arbiträra terminalkommandon.
// - F:\New project är fortfarande read-only-referens.
public sealed class OllamaAgentHarness
{
    private const int MaxAgentSteps = 6;
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(180) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public sealed record AgentResult(string Answer, int Steps, List<string> ToolsUsed);

    public async Task<AgentResult> RunAsync(
        string endpoint,
        string model,
        string userTask,
        string workspaceRoot,
        CancellationToken cancellationToken = default)
    {
        var fullRoot = Path.GetFullPath(workspaceRoot);
        var tools = BuildToolDefinitions();
        var toolsUsed = new List<string>();
        var messages = new List<object>
        {
            new { role = "system", content = SystemPrompt(fullRoot) },
            new { role = "user", content = userTask }
        };

        for (var step = 1; step <= MaxAgentSteps; step++)
        {
            string responseBody;
            try
            {
                responseBody = await SendRequestAsync(endpoint, model, tools, messages, cancellationToken);
            }
            catch (Exception ex)
            {
                return new AgentResult(
                    "Agentkörning misslyckades: " + ex.Message + "\n(Är Ollama igång på " + endpoint + "?)",
                    step - 1, toolsUsed);
            }

            var (content, toolCalls) = ParseResponse(responseBody);

            if (toolCalls.Count == 0)
            {
                var answer = string.IsNullOrWhiteSpace(content)
                    ? "Agenten slutförde uppgiften utan svar." : content.Trim();
                return new AgentResult(answer, step, toolsUsed);
            }

            messages.Add(BuildAssistantToolCallMessage(content, toolCalls));

            foreach (var (toolName, argsJson) in toolCalls)
            {
                toolsUsed.Add(toolName);
                var toolOutput = ExecuteToolSafely(toolName, argsJson, fullRoot);
                messages.Add(new { role = "tool", content = Truncate(toolOutput, 6000) });
            }
        }

        return new AgentResult(
            "Agenten nådde max antal steg (" + MaxAgentSteps + ") utan att avsluta. Säg till om du vill köra nästa pass.",
            MaxAgentSteps, toolsUsed);
    }

    private static string SystemPrompt(string workspaceRoot) =>
        "Du är Jarvis-agent som arbetar i en svensk kodbas. Aktuell arbetsmapp: " + workspaceRoot + "\n\n" +
        "Du har LÄSVERKTYG: read_file, list_files, glob_files, grep_text, get_project_context.\n" +
        "Du har INTE skrivverktyg. För filskrivning, terminalkörning eller delete: säg till användaren\n" +
        "att köra '/fil skapa', '/fil skriv' eller 'terminal preview:' själv så går det via godkännande-popupen.\n\n" +
        "Svara på svenska. Var koncis. När uppgiften är klar — svara utan tool calls.";

    // === Tool definitions (read-only) ===
    private static List<object> BuildToolDefinitions()
    {
        return new List<object>
        {
            ToolDef("read_file", "Läs textinnehåll från en relativ filsökväg under arbetsmappen.",
                new { path = new { type = "string", description = "Relativ sökväg" } }, new[] { "path" }),
            ToolDef("list_files", "Lista filer i en mapp (rekursivt). Returnerar relativa sökvägar.",
                new { folder = new { type = "string", description = "Relativ mapp, tom = projektrot" } }, Array.Empty<string>()),
            ToolDef("glob_files", "Hitta filer som matchar ett glob-mönster, t.ex. **/*.cs.",
                new { pattern = new { type = "string" } }, new[] { "pattern" }),
            ToolDef("grep_text", "Sök efter en substring (case-insensitive) i textfiler.",
                new { query = new { type = "string" }, glob = new { type = "string", description = "Filtreringsglob, t.ex. **/*.md" } },
                new[] { "query" }),
            ToolDef("get_project_context", "Returnerar översikt av projektets viktiga MD-filer.",
                new { }, Array.Empty<string>())
        };
    }

    private static object ToolDef(string name, string description, object properties, string[] required)
    {
        return new
        {
            type = "function",
            function = new
            {
                name,
                description,
                parameters = new { type = "object", properties, required }
            }
        };
    }

    // === Safe tool execution (read-only enforced) ===
    private static string ExecuteToolSafely(string toolName, string argsJson, string workspaceRoot)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson);
            var args = doc.RootElement;

            return toolName switch
            {
                "read_file" => ToolReadFile(GetArg(args, "path"), workspaceRoot),
                "list_files" => ToolListFiles(GetArg(args, "folder"), workspaceRoot),
                "glob_files" => ToolGlobFiles(GetArg(args, "pattern"), workspaceRoot),
                "grep_text" => ToolGrepText(GetArg(args, "query"), GetArg(args, "glob"), workspaceRoot),
                "get_project_context" => ToolProjectContext(workspaceRoot),
                // Säkerhet: alla skriv-/exekverings-verktyg blockeras explicit i Fas 6.
                "write_file" or "replace_in_file" or "run_command" or "run_shell" or "run_build" or "delete_file" =>
                    "Verktyget '" + toolName + "' är inte tillgängligt i agentläge. " +
                    "All filskrivning och terminalkörning måste gå genom användaren via /fil skapa, " +
                    "/fil skriv = ... eller terminal preview: ... — det utlöser approval-popup.",
                _ => "Okänt verktyg: " + toolName
            };
        }
        catch (Exception ex)
        {
            return "Verktygsfel (" + toolName + "): " + ex.Message;
        }
    }

    private static string GetArg(JsonElement args, string key)
    {
        return args.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? "" : "";
    }

    // === Read-only tool implementations ===
    private static string ToolReadFile(string relPath, string root)
    {
        var safe = ResolveSafePath(root, relPath);
        if (safe is null) return "Sökväg utanför arbetsmappen blockerad: " + relPath;
        if (!File.Exists(safe)) return "Filen finns inte: " + relPath;
        try
        {
            var info = new FileInfo(safe);
            if (info.Length > 200_000) return "Filen är för stor (>200 KB) för agentläsning. Använd grep_text eller läs delar av den.";
            return File.ReadAllText(safe);
        }
        catch (Exception ex) { return "Läsfel: " + ex.Message; }
    }

    private static string ToolListFiles(string relFolder, string root)
    {
        var safe = ResolveSafePath(root, string.IsNullOrWhiteSpace(relFolder) ? "" : relFolder);
        if (safe is null) return "Mapp utanför arbetsmappen blockerad: " + relFolder;
        if (!Directory.Exists(safe)) return "Mappen finns inte: " + relFolder;

        var files = new List<string>();
        try
        {
            foreach (var f in Directory.EnumerateFiles(safe, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, f).Replace("\\", "/");
                if (rel.Contains("/bin/") || rel.Contains("/obj/") || rel.Contains("/.git/") ||
                    rel.Contains("/node_modules/") || rel.Contains("/.checkpoints/")) continue;
                files.Add(rel);
                if (files.Count >= 200) break;
            }
        }
        catch { }
        return files.Count == 0 ? "Inga filer hittades." : string.Join("\n", files);
    }

    private static string ToolGlobFiles(string pattern, string root)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return "Pattern saknas.";
        try
        {
            // Enkel glob → regex: ** = vad som helst inkl. /, * = vad som helst utom /, ? = ett tecken.
            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace(@"\*\*", ".*")
                .Replace(@"\*", "[^/\\\\]*")
                .Replace(@"\?", ".") + "$";
            var rx = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var hits = new List<string>();
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (hits.Count >= 100) break;
                var rel = Path.GetRelativePath(root, f).Replace("\\", "/");
                if (rel.Contains("/bin/") || rel.Contains("/obj/") || rel.Contains("/.git/") ||
                    rel.Contains("/node_modules/") || rel.Contains("/.checkpoints/")) continue;
                if (rx.IsMatch(rel)) hits.Add(rel);
            }
            return hits.Count == 0 ? "Inga matcher." : string.Join("\n", hits);
        }
        catch (Exception ex) { return "Glob-fel: " + ex.Message; }
    }

    private static string ToolGrepText(string query, string glob, string root)
    {
        if (string.IsNullOrWhiteSpace(query)) return "Query saknas.";
        var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".md", ".txt", ".cs", ".html", ".css", ".js", ".json", ".py", ".ps1", ".csproj", ".xml" };
        var hits = new List<string>();
        try
        {
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (hits.Count >= 60) break;
                var rel = Path.GetRelativePath(root, f).Replace("\\", "/");
                if (rel.Contains("/bin/") || rel.Contains("/obj/") || rel.Contains("/.git/") ||
                    rel.Contains("/node_modules/") || rel.Contains("/.checkpoints/")) continue;
                if (!allowedExt.Contains(Path.GetExtension(f))) continue;
                if (!string.IsNullOrWhiteSpace(glob) && !Path.GetFileName(f).Contains(glob, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var lines = File.ReadAllLines(f);
                    for (int i = 0; i < lines.Length && hits.Count < 60; i++)
                    {
                        if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                            hits.Add(rel + ":" + (i + 1) + ": " + lines[i].Trim());
                    }
                }
                catch { }
            }
        }
        catch { }
        return hits.Count == 0 ? "Inga träffar för: " + query : string.Join("\n", hits);
    }

    private static string ToolProjectContext(string root)
    {
        var sb = new StringBuilder();
        var keyFiles = new[] { "README.md", "AGENTS.md", "CURRENT_STATE.md", "MASTER_PLAN.md", "BUILD_PLAN.md", "TODO_NEXT.md" };
        foreach (var f in keyFiles)
        {
            var p = Path.Combine(root, f);
            if (!File.Exists(p)) continue;
            try
            {
                var text = File.ReadAllText(p);
                if (text.Length > 1500) text = text.Substring(0, 1500) + "\n[...trimmed...]";
                sb.AppendLine("=== " + f + " ===");
                sb.AppendLine(text);
                sb.AppendLine();
            }
            catch { }
        }
        return sb.Length == 0 ? "Inga MD-filer hittades." : sb.ToString();
    }

    private static string? ResolveSafePath(string root, string relPath)
    {
        try
        {
            var combined = string.IsNullOrEmpty(relPath) ? root : Path.Combine(root, relPath.Replace("/", "\\"));
            var full = Path.GetFullPath(combined);
            var fullRoot = Path.GetFullPath(root);
            return full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
        catch { return null; }
    }

    // === Ollama HTTP ===
    private static async Task<string> SendRequestAsync(string endpoint, string model, List<object> tools, List<object> messages, CancellationToken ct)
    {
        var payload = new { model, stream = false, messages, tools };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await HttpClient.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    private static (string content, List<(string toolName, string argsJson)> toolCalls) ParseResponse(string body)
    {
        var calls = new List<(string, string)>();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("message", out var msg)) return ("", calls);
            var content = msg.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
            if (msg.TryGetProperty("tool_calls", out var tcs) && tcs.ValueKind == JsonValueKind.Array)
            {
                foreach (var tc in tcs.EnumerateArray())
                {
                    if (!tc.TryGetProperty("function", out var fn)) continue;
                    var name = fn.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var args = fn.TryGetProperty("arguments", out var a) ? a.GetRawText() : "{}";
                    calls.Add((name, args));
                }
            }
            return (content, calls);
        }
        catch { return ("", calls); }
    }

    private static object BuildAssistantToolCallMessage(string content, List<(string toolName, string argsJson)> toolCalls)
    {
        return new
        {
            role = "assistant",
            content,
            tool_calls = toolCalls.Select(t => new
            {
                type = "function",
                function = new { name = t.toolName, arguments = t.argsJson }
            }).ToArray()
        };
    }

    private static string Truncate(string s, int max)
    {
        return s.Length <= max ? s : s.Substring(0, max) + "\n[...trimmed " + (s.Length - max) + " chars...]";
    }
}
