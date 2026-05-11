namespace JarvisClean;

internal enum CommandIntent
{
    Unknown,
    NormalChat,
    Help,
    Status,
    MemoryShow,
    MemoryImportantShow,
    MemoryProjectShow,
    MemoryStatus,
    MemorySave,
    MemorySearch,
    MemoryArchiveSearch,
    MemoryForgetPrepare,
    MemoryForgetConfirm,
    FileOpen,
    FileRead,
    FileCreateRequest,
    FileWriteRequest,
    FileAppendRequest,
    FileWriteApprove,
    FileWriteCancel,
    FolderOpen,
    ProjectIndex,
    ModelShow,
    ModelList,
    ModelChange,
    TerminalPreview,
    TerminalConfirm,
    TerminalCancel,
    TerminalShow,
    ObsidianStatus,
    OverviewShow,
    CheckpointCreate,
    CheckpointList,
    CheckpointRestore,
    BrainWindowOpen,
    AgentRun,
    ModelCatalogList,
    ModelCatalogSwitch,
    VaultSearch,
    VaultCreate,
    VaultToggle,
    VaultStatus,
    ConversationShow,
    ConversationClear,
    ProgramLaunch,
    ProgramListAllowed,
    WebSearch,
    WebFetch,
    NaturalCodeEdit,
    BuilderStart,
    BuilderAnswer,
    BuilderPlan,
    BuilderStatus,
    BuilderCancel,
    DesktopStatus,
    DesktopEnable,
    DesktopDisable,
    DesktopScreenshot,
    DesktopBridgeStart,
    DesktopBridgeStop,
    DesktopVisionRequest,
    DesktopActionRequest
}

internal enum CommandRisk
{
    SafeRead,
    SafeUi,
    ProposeOnly,
    WritesFile,
    RunsTerminal,
    DangerousBlocked
}

internal sealed class CommandResult
{
    public CommandIntent Intent { get; init; } = CommandIntent.Unknown;
    public CommandRisk Risk { get; init; } = CommandRisk.SafeRead;
    public string ToolName { get; init; } = string.Empty;
    public Dictionary<string, string> Arguments { get; init; } = new();
    public bool RequiresApproval { get; init; }
    public bool ShouldSendToOllama { get; init; }
    public List<string> ValidationErrors { get; init; } = new();

    public bool IsValid => ValidationErrors.Count == 0;
}

internal static class CommandRouterV1
{
    public static string[] SplitFileCommandArguments(string raw, int maxParts = 2)
    {
        var s = raw ?? string.Empty;
        var eqIdx = s.IndexOf('=');
        var pipeIdx = s.IndexOf('|');

        if (eqIdx >= 0 && (pipeIdx < 0 || eqIdx < pipeIdx))
            return s.Split('=', maxParts);

        if (pipeIdx >= 0)
            return s.Split('|', maxParts);

        return new[] { s };
    }

    public static CommandResult Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new CommandResult
            {
                Intent = CommandIntent.Unknown,
                ToolName = "empty",
                ShouldSendToOllama = false,
                ValidationErrors = { "Skriv något först." }
            };
        }

        var raw = input.Trim();

        if (raw.StartsWith("/", StringComparison.Ordinal))
            return ParseSlashCommand(raw);

        var command = Normalize(raw);

        if (command == "hjalp" || command == "help" || command == "kommandohjalp" || command == "kommando hjalp")
        {
            return new CommandResult
            {
                Intent = CommandIntent.Help,
                Risk = CommandRisk.SafeRead,
                ToolName = "help.show",
                ShouldSendToOllama = false
            };
        }

        if (command == "status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.Status,
                Risk = CommandRisk.SafeRead,
                ToolName = "status.show",
                ShouldSendToOllama = false
            };
        }

        if (command is "oversikt" or "visa oversikt" or "jarvis oversikt" or "visa jarvis oversikt")
        {
            return new CommandResult
            {
                Intent = CommandIntent.OverviewShow,
                Risk = CommandRisk.SafeUi,
                ToolName = "overview.show",
                ShouldSendToOllama = false
            };
        }

        if (command is "obsidian status" or "visa obsidian" or "obsidian")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ObsidianStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "obsidian.status",
                ShouldSendToOllama = false
            };
        }

        if (command is "minnesstatus" or "minne status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.MemoryStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "memory.status",
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.NormalChat,
            Risk = CommandRisk.SafeRead,
            ToolName = "ollama.chat",
            ShouldSendToOllama = true
        };
    }

    private static CommandResult ParseSlashCommand(string input)
    {
        var command = Normalize(input[1..]);

        if (string.IsNullOrWhiteSpace(command))
        {
            return new CommandResult
            {
                Intent = CommandIntent.Unknown,
                ToolName = "slash.empty",
                ShouldSendToOllama = false,
                ValidationErrors = { "Skriv ett slash-kommando, till exempel: /hjälp" }
            };
        }

        if (command == "hjalp" || command == "help")
        {
            return new CommandResult
            {
                Intent = CommandIntent.Help,
                Risk = CommandRisk.SafeRead,
                ToolName = "help.show",
                ShouldSendToOllama = false
            };
        }

        if (command == "status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.Status,
                Risk = CommandRisk.SafeRead,
                ToolName = "status.show",
                ShouldSendToOllama = false
            };
        }

        if (command is "oversikt" or "overview")
        {
            return new CommandResult
            {
                Intent = CommandIntent.OverviewShow,
                Risk = CommandRisk.SafeUi,
                ToolName = "overview.show",
                ShouldSendToOllama = false
            };
        }

        if (command == "obsidian" || command.StartsWith("obsidian "))
            return ParseObsidianSlashCommand(command);

        if (command == "minne" || command.StartsWith("minne "))
            return ParseMemorySlashCommand(input[1..].Trim(), command);

        if (command == "fil" || command.StartsWith("fil "))
            return ParseFileSlashCommand(input[1..].Trim(), command);

        if (command == "terminal" || command.StartsWith("terminal "))
            return ParseTerminalSlashCommand(input[1..].Trim(), command);

        if (command == "desktop" || command.StartsWith("desktop "))
            return ParseDesktopSlashCommand(input[1..].Trim(), command);

        if (command is "skarm" or "screenshot")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopScreenshot,
                Risk = CommandRisk.SafeRead,
                ToolName = "desktop.screenshot",
                ShouldSendToOllama = false
            };
        }

        if (command == "checkpoint" || command.StartsWith("checkpoint "))
            return ParseCheckpointSlashCommand(input[1..].Trim(), command);

        if (command == "vault" || command.StartsWith("vault "))
            return ParseVaultSlashCommand(input[1..].Trim(), command);

        if (command == "bygg" || command.StartsWith("bygg "))
            return ParseBuilderSlashCommand(input[1..].Trim(), command);

        if (command is "historik" or "history")
        {
            return new CommandResult { Intent = CommandIntent.ConversationShow, Risk = CommandRisk.SafeRead, ToolName = "conversation.show", ShouldSendToOllama = false };
        }
        if (command is "glöm samtal" or "glom samtal" or "rensa samtal" or "clear chat")
        {
            return new CommandResult { Intent = CommandIntent.ConversationClear, Risk = CommandRisk.SafeUi, ToolName = "conversation.clear", ShouldSendToOllama = false };
        }

        // /öppna program <namn> — säker app-launcher med whitelist
        if (command == "öppna program" || command == "oppna program" ||
            command.StartsWith("öppna program ") || command.StartsWith("oppna program "))
        {
            var raw = input[1..].Trim();
            var appName = TailAfterWordCount(raw, 2);
            return new CommandResult
            {
                Intent = CommandIntent.ProgramLaunch,
                Risk = CommandRisk.RunsTerminal,
                ToolName = "program.launch",
                Arguments = { ["app"] = appName },
                ShouldSendToOllama = false
            };
        }
        if (command is "lista program" or "vilka program" or "tillåtna program" or "tillatna program")
        {
            return new CommandResult { Intent = CommandIntent.ProgramListAllowed, Risk = CommandRisk.SafeRead, ToolName = "program.list", ShouldSendToOllama = false };
        }

        // /sök <query> — DuckDuckGo HTML scraping (offline-graceful)
        if (command == "sök" || command == "sok" || command.StartsWith("sök ") || command.StartsWith("sok "))
        {
            var raw = input[1..].Trim();
            var query = TailAfterWordCount(raw, 1);
            return new CommandResult
            {
                Intent = CommandIntent.WebSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "web.search",
                Arguments = { ["query"] = query },
                ShouldSendToOllama = false
            };
        }
        // /läs <url> — fetch + sammanfatta sida
        if (command == "läs" || command == "las" || command.StartsWith("läs ") || command.StartsWith("las "))
        {
            var raw = input[1..].Trim();
            var url = TailAfterWordCount(raw, 1);
            // Bara behandla som web-fetch om det ser ut som URL
            if (url.Contains("://") || url.StartsWith("www.") || (url.Contains(".") && !url.Contains("/") && !url.Contains(" ") && !url.EndsWith(".md") && !url.EndsWith(".cs") && !url.EndsWith(".js")))
            {
                return new CommandResult
                {
                    Intent = CommandIntent.WebFetch,
                    Risk = CommandRisk.SafeRead,
                    ToolName = "web.fetch",
                    Arguments = { ["url"] = url },
                    ShouldSendToOllama = false
                };
            }
        }

        if (command == "edit" || command.StartsWith("edit ") ||
            command == "ändra fil" || command.StartsWith("ändra fil ") ||
            command == "andra fil" || command.StartsWith("andra fil "))
        {
            var raw = input[1..].Trim();
            var skipWords = command.StartsWith("ändra fil") || command.StartsWith("andra fil") ? 2 : 1;
            var editBody = TailAfterWordCount(raw, skipWords);
            var parts = SplitFileCommandArguments(editBody, 2);
            var path = parts.Length > 0 ? parts[0].Trim() : "";
            var instruction = parts.Length > 1 ? parts[1].Trim() : "";

            return new CommandResult
            {
                Intent = CommandIntent.NaturalCodeEdit,
                Risk = CommandRisk.WritesFile,
                ToolName = "natural_edit.request",
                Arguments =
                {
                    ["path"] = path,
                    ["instruction"] = instruction
                },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command == "brain" || command == "hjarna")
        {
            return new CommandResult
            {
                Intent = CommandIntent.BrainWindowOpen,
                Risk = CommandRisk.SafeUi,
                ToolName = "brain.open",
                ShouldSendToOllama = false
            };
        }

        // /explorer borttagen 2026-05-10: Project Explorer (vänsterpanel) är "the" explorer.

        if (command == "agent" || command.StartsWith("agent "))
        {
            // /agent <task> kör read-only agent-harness mot lokal Ollama. Inga writes.
            var task = TailAfterWordCount(input[1..].Trim(), 1);
            return new CommandResult
            {
                Intent = CommandIntent.AgentRun,
                Risk = CommandRisk.SafeRead,
                ToolName = "agent.run",
                Arguments = { ["task"] = task },
                ShouldSendToOllama = false
            };
        }

        // /modell — modellprofil-katalogen från ModelCatalog.cs
        if (command == "modell" || command == "modell lista" || command == "modell visa" || command == "modeller")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ModelCatalogList,
                Risk = CommandRisk.SafeRead,
                ToolName = "model.list",
                ShouldSendToOllama = false
            };
        }
        if (command.StartsWith("modell byt") || command.StartsWith("modell snabb") ||
            command.StartsWith("modell smart") || command.StartsWith("modell kod") ||
            command.StartsWith("modell reason") || command.StartsWith("modell general"))
        {
            var raw = input[1..].Trim();
            // Plocka ut målet: "modell byt qwen3:8b" → "qwen3:8b"; "modell kod" → "kod"
            string target;
            if (command.StartsWith("modell byt"))
                target = TailAfterWordCount(raw, 2);
            else
                target = TailAfterWordCount(raw, 1); // ord efter "modell" är profil-rollen
            return new CommandResult
            {
                Intent = CommandIntent.ModelCatalogSwitch,
                Risk = CommandRisk.SafeUi,
                ToolName = "model.switch",
                Arguments = { ["target"] = target },
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt slash-kommando: /" + input[1..].Trim() + "\nSkriv /hjälp för lokala kommandon." }
        };
    }

    private static CommandResult ParseBuilderSlashCommand(string body, string command)
    {
        if (command == "bygg status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.BuilderStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "builder.status",
                ShouldSendToOllama = false
            };
        }

        if (command is "bygg avbryt" or "bygg cancel")
        {
            return new CommandResult
            {
                Intent = CommandIntent.BuilderCancel,
                Risk = CommandRisk.SafeUi,
                ToolName = "builder.cancel",
                ShouldSendToOllama = false
            };
        }

        if (command is "bygg plan" or "bygg fortsätt" or "bygg fortsatt")
        {
            return new CommandResult
            {
                Intent = CommandIntent.BuilderPlan,
                Risk = CommandRisk.WritesFile,
                ToolName = "builder.plan",
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command == "bygg svar" || command.StartsWith("bygg svar "))
        {
            var answer = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.BuilderAnswer,
                Risk = CommandRisk.SafeUi,
                ToolName = "builder.answer",
                Arguments = { ["answer"] = answer },
                ShouldSendToOllama = false
            };
        }

        if (command == "bygg")
        {
            return new CommandResult
            {
                Intent = CommandIntent.BuilderStart,
                Risk = CommandRisk.SafeUi,
                ToolName = "builder.start",
                ShouldSendToOllama = false,
                ValidationErrors = { "Byggbeskrivning saknas. Skriv: /bygg <vad du vill bygga>" }
            };
        }

        var description = TailAfterWordCount(body, 1);
        return new CommandResult
        {
            Intent = CommandIntent.BuilderStart,
            Risk = CommandRisk.SafeUi,
            ToolName = "builder.start",
            Arguments = { ["description"] = description },
            ShouldSendToOllama = false
        };
    }

    private static CommandResult ParseDesktopSlashCommand(string body, string command)
    {
        if (command is "desktop" or "desktop status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "desktop.status",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop på" or "desktop pa" or "desktop on")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopEnable,
                Risk = CommandRisk.SafeUi,
                ToolName = "desktop.enable",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop av" or "desktop off" or "desktop stop" or "desktop stopp")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopDisable,
                Risk = CommandRisk.SafeUi,
                ToolName = "desktop.disable",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop skarm" or "desktop screenshot")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopScreenshot,
                Risk = CommandRisk.SafeRead,
                ToolName = "desktop.screenshot",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop tars start" or "desktop uitars start" or "desktop ui-tars start")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopBridgeStart,
                Risk = CommandRisk.SafeUi,
                ToolName = "desktop.bridge.start",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop tars stop" or "desktop uitars stop" or "desktop ui-tars stop")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopBridgeStop,
                Risk = CommandRisk.SafeUi,
                ToolName = "desktop.bridge.stop",
                ShouldSendToOllama = false
            };
        }

        if (command == "desktop fråga" || command.StartsWith("desktop fråga ") ||
            command == "desktop fraga" || command.StartsWith("desktop fraga ") ||
            command == "desktop föreslå" || command.StartsWith("desktop föreslå ") ||
            command == "desktop foresla" || command.StartsWith("desktop foresla "))
        {
            var instruction = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.DesktopVisionRequest,
                Risk = CommandRisk.RunsTerminal,
                ToolName = "desktop.vision.request",
                Arguments = { ["instruction"] = instruction },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command.StartsWith("desktop "))
        {
            var action = TailWordAt(body, 1);
            var payload = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.DesktopActionRequest,
                Risk = CommandRisk.RunsTerminal,
                ToolName = "desktop.action.request",
                Arguments =
                {
                    ["action"] = action,
                    ["payload"] = payload
                },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop" or "desktop status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "desktop.status",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop pa" or "desktop på" or "desktop on")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopEnable,
                Risk = CommandRisk.SafeUi,
                ToolName = "desktop.enable",
                ShouldSendToOllama = false
            };
        }

        if (command is "desktop av" or "desktop off" or "desktop stop")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopDisable,
                Risk = CommandRisk.SafeUi,
                ToolName = "desktop.disable",
                ShouldSendToOllama = false
            };
        }

        if (command is "skarm" or "screenshot")
        {
            return new CommandResult
            {
                Intent = CommandIntent.DesktopScreenshot,
                Risk = CommandRisk.SafeRead,
                ToolName = "desktop.screenshot",
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.desktop.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /desktop-kommando. Exempel: /desktop på, /desktop status, /desktop klick 100 200, /desktop fråga <instruktion>, /desktop av" }
        };
    }

    // /checkpoint skapa <namn>     -> CheckpointCreate (named)
    // /checkpoint skapa            -> CheckpointCreate (timestamp-only)
    // /checkpoint lista            -> CheckpointList
    // /checkpoint återställ <namn> -> CheckpointRestore (named)
    // /checkpoint återställ        -> CheckpointRestore (latest)
    private static CommandResult ParseCheckpointSlashCommand(string body, string command)
    {
        if (command == "checkpoint skapa" || command.StartsWith("checkpoint skapa "))
        {
            var name = TailAfterWordCount(body, 2).Trim();
            return new CommandResult
            {
                Intent = CommandIntent.CheckpointCreate,
                Risk = CommandRisk.SafeUi,
                ToolName = "checkpoint.create",
                Arguments = { ["name"] = name },
                ShouldSendToOllama = false
            };
        }

        if (command is "checkpoint lista" or "checkpoint visa" or "checkpoint list")
        {
            return new CommandResult
            {
                Intent = CommandIntent.CheckpointList,
                Risk = CommandRisk.SafeRead,
                ToolName = "checkpoint.list",
                ShouldSendToOllama = false
            };
        }

        if (command == "checkpoint aterstall" || command.StartsWith("checkpoint aterstall ") ||
            command == "checkpoint restore" || command.StartsWith("checkpoint restore "))
        {
            var name = TailAfterWordCount(body, 2).Trim();
            return new CommandResult
            {
                Intent = CommandIntent.CheckpointRestore,
                Risk = CommandRisk.WritesFile,
                ToolName = "checkpoint.restore",
                Arguments = { ["name"] = name },
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.checkpoint.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /checkpoint-kommando. Exempel: /checkpoint skapa innan-port, /checkpoint lista, /checkpoint återställ innan-port" }
        };
    }

    // /vault sök <query>          → topp 10 träffar i chatten
    // /vault skapa <namn> = <text>→ ny MD-fil i vault/auto/ via PendingApproval
    // /vault på  | /vault av      → toggle auto-context i AskOllamaAsync
    // /vault status               → visar antal noter, om auto-context är på, senaste användning
    private static CommandResult ParseVaultSlashCommand(string body, string command)
    {
        if (command == "vault" || command == "vault status")
        {
            return new CommandResult { Intent = CommandIntent.VaultStatus, Risk = CommandRisk.SafeRead, ToolName = "vault.status", ShouldSendToOllama = false };
        }
        if (command == "vault på" || command == "vault pa" || command == "vault av")
        {
            var on = command.EndsWith("på") || command.EndsWith("pa");
            return new CommandResult
            {
                Intent = CommandIntent.VaultToggle,
                Risk = CommandRisk.SafeUi,
                ToolName = "vault.toggle",
                Arguments = { ["on"] = on ? "true" : "false" },
                ShouldSendToOllama = false
            };
        }
        if (command == "vault sök" || command.StartsWith("vault sök ") ||
            command == "vault sok" || command.StartsWith("vault sok "))
        {
            var query = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.VaultSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "vault.search",
                Arguments = { ["query"] = query },
                ShouldSendToOllama = false
            };
        }
        if (command == "vault skapa" || command.StartsWith("vault skapa "))
        {
            // Format: vault skapa <namn> = <text>
            var raw = TailAfterWordCount(body, 2);
            var parts = SplitFileCommandArguments(raw, 2);
            var name = parts.Length > 0 ? parts[0].Trim() : "";
            var content = parts.Length > 1 ? parts[1].Trim() : "";
            return new CommandResult
            {
                Intent = CommandIntent.VaultCreate,
                Risk = CommandRisk.WritesFile,
                ToolName = "vault.create",
                Arguments = { ["name"] = name, ["text"] = content },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.vault.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /vault-kommando. Exempel: /vault status, /vault sök <query>, /vault skapa <namn> = <text>, /vault på, /vault av" }
        };
    }

    private static CommandResult ParseFileSlashCommand(string body, string command)
    {
        if (command == "fil skapa" || command.StartsWith("fil skapa "))
        {
            var raw = TailAfterWordCount(body, 2);
            var parts = SplitFileCommandArguments(raw, 2);
            var path = parts.Length > 0 ? parts[0].Trim() : "";
            var text = parts.Length > 1 ? parts[1].Trim() : "";

            return new CommandResult
            {
                Intent = CommandIntent.FileCreateRequest,
                Risk = CommandRisk.WritesFile,
                ToolName = "file.create.request",
                Arguments =
                {
                    ["path"] = path,
                    ["text"] = text
                },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command == "fil oppna" || command.StartsWith("fil oppna "))
        {
            var path = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.FileOpen,
                Risk = CommandRisk.SafeUi,
                ToolName = "file.open",
                Arguments = { ["path"] = path },
                ShouldSendToOllama = false
            };
        }

        if (command == "fil las" || command.StartsWith("fil las "))
        {
            var path = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.FileRead,
                Risk = CommandRisk.SafeRead,
                ToolName = "file.read",
                Arguments = { ["path"] = path },
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.file.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /fil-kommando. Just nu finns: /fil öppna README.md, /fil läs docs/PROJECT_INDEX.md och /fil skapa docs/test.md = text" }
        };
    }

    private static CommandResult ParseTerminalSlashCommand(string body, string command)
    {
        if (command == "terminal preview" || command.StartsWith("terminal preview "))
        {
            var terminalCommand = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.TerminalPreview,
                Risk = CommandRisk.RunsTerminal,
                ToolName = "terminal.preview",
                Arguments = { ["command"] = terminalCommand },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command is "terminal godkann" or "terminal bekrafta" or "terminal confirm")
        {
            return new CommandResult
            {
                Intent = CommandIntent.TerminalConfirm,
                Risk = CommandRisk.RunsTerminal,
                ToolName = "terminal.confirm",
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        if (command is "terminal avbryt" or "terminal cancel")
        {
            return new CommandResult
            {
                Intent = CommandIntent.TerminalCancel,
                Risk = CommandRisk.SafeUi,
                ToolName = "terminal.cancel",
                ShouldSendToOllama = false
            };
        }

        if (command is "terminal visa" or "terminal show" or "terminal output")
        {
            return new CommandResult
            {
                Intent = CommandIntent.TerminalShow,
                Risk = CommandRisk.SafeRead,
                ToolName = "terminal.show",
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.terminal.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /terminal-kommando. Exempel: /terminal preview dotnet build, /terminal visa, /terminal godkänn, /terminal avbryt" }
        };
    }

    private static CommandResult ParseMemorySlashCommand(string body, string command)
    {
        if (command == "minne status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.MemoryStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "memory.status",
                ShouldSendToOllama = false
            };
        }

        if (command == "minne visa")
        {
            return new CommandResult
            {
                Intent = CommandIntent.MemoryShow,
                Risk = CommandRisk.SafeRead,
                ToolName = "memory.show",
                ShouldSendToOllama = false
            };
        }

        if (command == "minne viktiga")
        {
            return new CommandResult
            {
                Intent = CommandIntent.MemoryImportantShow,
                Risk = CommandRisk.SafeRead,
                ToolName = "memory.show.important",
                ShouldSendToOllama = false
            };
        }

        if (command == "minne projekt")
        {
            return new CommandResult
            {
                Intent = CommandIntent.MemoryProjectShow,
                Risk = CommandRisk.SafeRead,
                ToolName = "memory.show.project",
                ShouldSendToOllama = false
            };
        }

        if (command == "minne sok" || command.StartsWith("minne sok "))
        {
            var query = TailAfterWordCount(body, 2);
            return new CommandResult
            {
                Intent = CommandIntent.MemorySearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "memory.search",
                Arguments = { ["query"] = query },
                ShouldSendToOllama = false
            };
        }

        if (command == "minne arkiv sok" || command.StartsWith("minne arkiv sok "))
        {
            var query = TailAfterWordCount(body, 3);
            return new CommandResult
            {
                Intent = CommandIntent.MemoryArchiveSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "archive.search",
                Arguments = { ["query"] = query },
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.memory.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /minne-kommando. Exempel: /minne status, /minne visa, /minne viktiga, /minne sök röd" }
        };
    }

    private static CommandResult ParseObsidianSlashCommand(string command)
    {
        if (command is "obsidian status" or "obsidian visa")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ObsidianStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "obsidian.status",
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.obsidian.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /obsidian-kommando. Just nu finns: /obsidian status" }
        };
    }

    private static string TailAfterWordCount(string value, int wordsToSkip)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length <= wordsToSkip)
            return "";

        return string.Join(" ", parts.Skip(wordsToSkip)).Trim();
    }

    private static string TailWordAt(string value, int zeroBasedIndex)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= zeroBasedIndex ? "" : parts[zeroBasedIndex].Trim();
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");

        return string.Join(" ", normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
