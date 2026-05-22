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
    TaskList,
    TaskStatus,
    TaskAddRequest,
    TaskCompleteRequest,
    TaskSearch,
    FileOpen,
    FileRead,
    FileCreateRequest,
    FileWriteRequest,
    FileAppendRequest,
    FileWriteApprove,
    FileWriteCancel,
    FolderOpen,
    ProjectIndex,
    ProjectIndexSearch,
    ProjectAudit,
    JobList,
    JobStatus,
    JobCancel,
    ModelShow,
    ModelList,
    ModelChange,
    ModelProviderStatus,
    ModelProviderMode,
    AutopilotStatus,
    AutopilotSetMode,
    AutopilotStop,
    VoiceStatus,
    VoiceOn,
    VoiceOff,
    VoiceMute,
    VoiceUnmute,
    VoiceMicTest,
    VoiceModelSet,
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
    DesktopActionRequest,
    VoiceListVoices,
    VoiceSetVoice,
    SceneShow,
    MapShow,
    WidgetLayoutSave,
    WidgetLayoutLoad,
    WidgetLayoutList,
    WidgetLayoutDelete
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

        if (command == "autopilot" || command.StartsWith("autopilot ") ||
            command == "agentlage" || command.StartsWith("agentlage "))
        {
            return ParseAutopilotSlashCommand(raw, command);
        }

        if (IsVoiceNaturalLanguage(command))
            return ParseVoiceCommand(command);

        // Scen / Cinematic Workspace - använd raw för att bevara å/ä/ö
        if (command.StartsWith("scen ") || command.StartsWith("presentera ") || command.StartsWith("visa scen "))
        {
            var prefix = command.StartsWith("scen ") ? "scen "
                       : command.StartsWith("visa scen ") ? "visa scen "
                       : "presentera ";
            return SceneResult(ExtractRawArg(raw, prefix));
        }
        if (command is "scen" or "scene" or "presentera")
            return SceneResult("");

        // Karta - 3D globe panel - använd raw för att bevara å/ä/ö
        if (command.StartsWith("karta ") || command.StartsWith("kartan ")
            || command.StartsWith("visa karta ") || command.StartsWith("visa kartan ")
            || command.StartsWith("flyga till ") || command.StartsWith("zooma till "))
        {
            string prefix;
            if (command.StartsWith("visa karta ")) prefix = "visa karta ";
            else if (command.StartsWith("visa kartan ")) prefix = "visa kartan ";
            else if (command.StartsWith("flyga till ")) prefix = "flyga till ";
            else if (command.StartsWith("zooma till ")) prefix = "zooma till ";
            else if (command.StartsWith("kartan ")) prefix = "kartan ";
            else prefix = "karta ";
            return MapResult(ExtractRawArg(raw, prefix));
        }
        if (command is "karta" or "kartan" or "map" or "visa karta" or "visa kartan")
            return MapResult("");

        // Widget-layout
        if (command.StartsWith("widget save ") || command.StartsWith("spara layout som "))
        {
            var prefix = command.StartsWith("spara layout som ") ? "spara layout som " : "widget save ";
            return WidgetLayoutResult(CommandIntent.WidgetLayoutSave, command.Substring(prefix.Length).Trim());
        }
        if (command.StartsWith("widget load ") || command.StartsWith("ladda layout ")
            || command.StartsWith("byt till layout "))
        {
            string p;
            if (command.StartsWith("byt till layout ")) p = "byt till layout ";
            else if (command.StartsWith("ladda layout ")) p = "ladda layout ";
            else p = "widget load ";
            return WidgetLayoutResult(CommandIntent.WidgetLayoutLoad, command.Substring(p.Length).Trim());
        }
        if (command is "widget list" or "lista layouts" or "visa layouts")
            return WidgetLayoutResult(CommandIntent.WidgetLayoutList, "");
        if (command.StartsWith("widget delete ") || command.StartsWith("ta bort layout "))
        {
            var p = command.StartsWith("ta bort layout ") ? "ta bort layout " : "widget delete ";
            return WidgetLayoutResult(CommandIntent.WidgetLayoutDelete, command.Substring(p.Length).Trim());
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

        if (TryParseNaturalTaskCommand(raw, command, out var taskCommand))
            return taskCommand;

        if (TryParseProjectIndexSearch(raw, command, out var projectIndexQuery))
        {
            return new CommandResult
            {
                Intent = CommandIntent.ProjectIndexSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.index.search",
                Arguments = { ["query"] = projectIndexQuery },
                ShouldSendToOllama = false
            };
        }

        if (IsProjectAuditRequest(command))
        {
            return new CommandResult
            {
                Intent = CommandIntent.ProjectAudit,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.audit.start",
                ShouldSendToOllama = false
            };
        }

        if (IsProjectIndexRequest(command))
        {
            return new CommandResult
            {
                Intent = CommandIntent.ProjectIndex,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.index.start",
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

        // /scen <query>  eller  /scene <query> - använd raw för att bevara å/ä/ö
        var rawNoSlash = input.StartsWith("/") ? input.Substring(1).TrimStart() : input.TrimStart();
        if (command.StartsWith("scen ") || command.StartsWith("scene "))
        {
            var prefix = command.StartsWith("scene ") ? "scene " : "scen ";
            return SceneResult(ExtractRawArg(rawNoSlash, prefix));
        }
        if (command is "scen" or "scene")
            return SceneResult("");

        // /karta <plats>  eller  /map <plats> - använd raw för att bevara å/ä/ö
        if (command.StartsWith("karta ") || command.StartsWith("map "))
        {
            var prefix = command.StartsWith("map ") ? "map " : "karta ";
            return MapResult(ExtractRawArg(rawNoSlash, prefix));
        }
        if (command is "karta" or "map")
            return MapResult("");

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

        if (IsTaskCommand(command))
            return ParseTaskSlashCommand(input[1..].Trim(), command);

        if (command == "fil" || command.StartsWith("fil "))
            return ParseFileSlashCommand(input[1..].Trim(), command);

        if (command == "jobb" || command.StartsWith("jobb "))
            return ParseJobSlashCommand(command);

        if (command == "projekt sok" || command.StartsWith("projekt sok ") ||
            command == "index sok" || command.StartsWith("index sok ") ||
            command == "projektindex sok" || command.StartsWith("projektindex sok "))
        {
            var raw = input[1..].Trim();
            var skipWords = command.StartsWith("projektindex sok") ? 2 : 2;
            var query = TailAfterWordCount(raw, skipWords);
            return new CommandResult
            {
                Intent = CommandIntent.ProjectIndexSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.index.search",
                Arguments = { ["query"] = query },
                ShouldSendToOllama = false
            };
        }

        if (command is "projekt audit" or "projekt deep audit" or "skapa audit" or "audit projekt")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ProjectAudit,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.audit.start",
                ShouldSendToOllama = false
            };
        }

        if (command is "projekt index" or "projekt analysera" or "index projekt" or "skapa audit")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ProjectIndex,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.index.start",
                ShouldSendToOllama = false
            };
        }

        if (command == "terminal" || command.StartsWith("terminal "))
            return ParseTerminalSlashCommand(input[1..].Trim(), command);

        if (command == "autopilot" || command.StartsWith("autopilot ") ||
            command == "agentlage" || command.StartsWith("agentlage "))
            return ParseAutopilotSlashCommand(input[1..].Trim(), command);

        if (command == "voice" || command.StartsWith("voice ") ||
            command == "rost" || command.StartsWith("rost "))
            return ParseVoiceSlashCommand(command);

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
        if (command is "modell provider" or "modell providers" or "modell status" or "modell motor")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ModelProviderStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "model.provider.status",
                ShouldSendToOllama = false
            };
        }
        if (command.StartsWith("modell lage") || command.StartsWith("modell läge") ||
            command.StartsWith("modell mode") || command is "modell auto" or "modell lokal")
        {
            var raw = input[1..].Trim();
            var target =
                command.StartsWith("modell lage") || command.StartsWith("modell läge") || command.StartsWith("modell mode")
                    ? TailAfterWordCount(raw, 2)
                    : TailAfterWordCount(raw, 1);
            return new CommandResult
            {
                Intent = CommandIntent.ModelProviderMode,
                Risk = CommandRisk.SafeUi,
                ToolName = "model.provider.mode",
                Arguments = { ["mode"] = target },
                ShouldSendToOllama = false
            };
        }

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

    private static CommandResult ParseJobSlashCommand(string command)
    {
        if (command is "jobb" or "jobb lista" or "jobb list")
        {
            return new CommandResult
            {
                Intent = CommandIntent.JobList,
                Risk = CommandRisk.SafeRead,
                ToolName = "jobs.list",
                ShouldSendToOllama = false
            };
        }

        if (command == "jobb status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.JobStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "jobs.status",
                ShouldSendToOllama = false
            };
        }

        if (command is "jobb start" or "jobb index" or "jobb projekt" or "jobb analysera")
        {
            return new CommandResult
            {
                Intent = CommandIntent.ProjectIndex,
                Risk = CommandRisk.SafeRead,
                ToolName = "project.index.start",
                ShouldSendToOllama = false
            };
        }

        if (command is "jobb avbryt" or "jobb cancel" or "jobb stop")
        {
            return new CommandResult
            {
                Intent = CommandIntent.JobCancel,
                Risk = CommandRisk.SafeUi,
                ToolName = "jobs.cancel",
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.jobs.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /jobb-kommando. Exempel: /jobb, /jobb status, /jobb start, /jobb avbryt" }
        };
    }

    private static CommandResult ParseAutopilotSlashCommand(string body, string command)
    {
        // AgentAutopilotModeV1 parses BrowserAutopilot, DesktopAutopilot and BuildAgent.
        if (command is "autopilot" or "autopilot status" or "agentlage" or "agentlage status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.AutopilotStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "autopilot.status",
                ShouldSendToOllama = false
            };
        }

        if (command is "autopilot stop" or "autopilot av" or "agentlage stop" or "agentlage av")
        {
            return new CommandResult
            {
                Intent = CommandIntent.AutopilotStop,
                Risk = CommandRisk.SafeUi,
                ToolName = "autopilot.stop",
                ShouldSendToOllama = false
            };
        }

        if (command is "autopilot safe" or "agentlage safe")
        {
            return new CommandResult
            {
                Intent = CommandIntent.AutopilotSetMode,
                Risk = CommandRisk.SafeUi,
                ToolName = "autopilot.mode",
                Arguments =
                {
                    ["mode"] = AgentAutopilotLevelV1.Safe.ToString(),
                    ["mission"] = ""
                },
                ShouldSendToOllama = false
            };
        }

        var rawMode = TailWordAt(body, 1);
        if (!AgentAutopilotModeV1.TryParseLevel(rawMode, out var level))
        {
            return new CommandResult
            {
                Intent = CommandIntent.Unknown,
                ToolName = "slash.autopilot.unknown",
                ShouldSendToOllama = false,
                ValidationErrors =
                {
                    "Okänt /autopilot-kommando. Exempel: /autopilot status, /autopilot approval, /autopilot browser <uppdrag>, /autopilot desktop <uppdrag>, /autopilot build <uppdrag>, /autopilot stop"
                }
            };
        }

        var mission = TailAfterWordCount(body, 2);
        return new CommandResult
        {
            Intent = CommandIntent.AutopilotSetMode,
            Risk = CommandRisk.SafeUi,
            ToolName = "autopilot.mode",
            Arguments =
            {
                ["mode"] = level.ToString(),
                ["mission"] = mission
            },
            ShouldSendToOllama = false
        };
    }

    private static bool IsVoiceNaturalLanguage(string command)
    {
        if (command is "rost" or "rost status" or "rostlage" or "rostlage status")
            return true;
        if (command is "aktivera rost" or "rost pa" or "satta pa rost" or "satt pa rost")
            return true;
        if (command is "stang av rost" or "rost av" or "avaktivera rost")
            return true;
        if (command is "tysta dig" or "var tyst" or "var tyst tack" or "rost mute" or "mute rost")
            return true;
        if (command is "prata igen" or "prata" or "rost unmute" or "unmute rost")
            return true;
        if (command is "mick test" or "mikrofon test" or "mic test" or "testa mikrofonen")
            return true;
        if (command.StartsWith("byt rostmodell till ") || command.StartsWith("rostmodell ")
            || command.StartsWith("rost modell "))
            return true;
        if (command is "rost roster" or "rost voices" or "lista roster" or "visa roster")
            return true;
        if (command.StartsWith("byt rost till ") || command.StartsWith("byt tts till ")
            || command.StartsWith("rost rost ") || command.StartsWith("ttsrost "))
            return true;
        return false;
    }

    private static CommandResult ParseVoiceCommand(string command)
    {
        if (command is "rost" or "rost status" or "rostlage" or "rostlage status")
            return VoiceResult(CommandIntent.VoiceStatus, "voice.status");

        if (command is "aktivera rost" or "rost pa" or "satta pa rost" or "satt pa rost")
            return VoiceResult(CommandIntent.VoiceOn, "voice.on");

        if (command is "stang av rost" or "rost av" or "avaktivera rost")
            return VoiceResult(CommandIntent.VoiceOff, "voice.off");

        if (command is "tysta dig" or "var tyst" or "var tyst tack" or "rost mute" or "mute rost")
            return VoiceResult(CommandIntent.VoiceMute, "voice.mute");

        if (command is "prata igen" or "prata" or "rost unmute" or "unmute rost")
            return VoiceResult(CommandIntent.VoiceUnmute, "voice.unmute");

        if (command is "mick test" or "mikrofon test" or "mic test" or "testa mikrofonen")
            return VoiceResult(CommandIntent.VoiceMicTest, "voice.mic.test");

        if (command.StartsWith("byt rostmodell till "))
        {
            var model = command.Substring("byt rostmodell till ".Length).Trim();
            return VoiceModelResult(model);
        }
        if (command.StartsWith("rostmodell "))
        {
            var model = command.Substring("rostmodell ".Length).Trim();
            return VoiceModelResult(model);
        }
        if (command.StartsWith("rost modell "))
        {
            var model = command.Substring("rost modell ".Length).Trim();
            return VoiceModelResult(model);
        }

        if (command is "rost roster" or "rost voices" or "lista roster" or "visa roster")
            return VoiceResult(CommandIntent.VoiceListVoices, "voice.voices.list");

        if (command.StartsWith("byt rost till "))
            return VoiceVoiceResult(command.Substring("byt rost till ".Length).Trim());
        if (command.StartsWith("byt tts till "))
            return VoiceVoiceResult(command.Substring("byt tts till ".Length).Trim());
        if (command.StartsWith("rost rost "))
            return VoiceVoiceResult(command.Substring("rost rost ".Length).Trim());
        if (command.StartsWith("ttsrost "))
            return VoiceVoiceResult(command.Substring("ttsrost ".Length).Trim());

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "voice.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt röstkommando. Exempel: aktivera röst, tysta dig, prata igen, mick test, byt röstmodell till medium." }
        };
    }

    private static CommandResult ParseVoiceSlashCommand(string command)
    {
        // command is already normalized and starts with "voice " or "rost "
        var rest = command.StartsWith("voice")
            ? command.Substring("voice".Length).Trim()
            : command.Substring("rost".Length).Trim();

        if (rest.Length == 0 || rest == "status")
            return VoiceResult(CommandIntent.VoiceStatus, "voice.status");

        if (rest is "on" or "pa" or "aktivera")
            return VoiceResult(CommandIntent.VoiceOn, "voice.on");

        if (rest is "off" or "av" or "stang av" or "avaktivera")
            return VoiceResult(CommandIntent.VoiceOff, "voice.off");

        if (rest is "mute" or "tyst" or "tysta")
            return VoiceResult(CommandIntent.VoiceMute, "voice.mute");

        if (rest is "unmute" or "prata")
            return VoiceResult(CommandIntent.VoiceUnmute, "voice.unmute");

        if (rest is "mic test" or "mick test" or "test" or "mikrofon test" or "mic")
            return VoiceResult(CommandIntent.VoiceMicTest, "voice.mic.test");

        if (rest.StartsWith("modell ") || rest.StartsWith("model "))
        {
            var model = rest.Substring(rest.IndexOf(' ') + 1).Trim();
            return VoiceModelResult(model);
        }

        if (rest is "roster" or "voices" or "rost lista" or "lista")
            return VoiceResult(CommandIntent.VoiceListVoices, "voice.voices.list");

        if (rest.StartsWith("rost ") || rest.StartsWith("voice "))
        {
            var voice = rest.Substring(rest.IndexOf(' ') + 1).Trim();
            return VoiceVoiceResult(voice);
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.voice.unknown",
            ShouldSendToOllama = false,
            ValidationErrors =
            {
                "Okänt /voice-kommando. Exempel: /voice, /voice on, /voice off, /voice mute, /voice unmute, /voice mic test, /voice modell <small|medium>."
            }
        };
    }

    private static CommandResult VoiceResult(CommandIntent intent, string toolName)
    {
        return new CommandResult
        {
            Intent = intent,
            Risk = CommandRisk.SafeUi,
            ToolName = toolName,
            ShouldSendToOllama = false
        };
    }

    private static CommandResult MapResult(string query)
    {
        return new CommandResult
        {
            Intent = CommandIntent.MapShow,
            Risk = CommandRisk.SafeUi,
            ToolName = "map.show",
            Arguments = { ["query"] = query ?? "" },
            ShouldSendToOllama = false
        };
    }

    private static CommandResult WidgetLayoutResult(CommandIntent intent, string name)
    {
        var args = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(name)) args["name"] = name;
        return new CommandResult
        {
            Intent = intent,
            Risk = CommandRisk.SafeUi,
            ToolName = "widget.layout." + intent.ToString().Replace("WidgetLayout", "").ToLowerInvariant(),
            Arguments = args,
            ShouldSendToOllama = false
        };
    }

    private static CommandResult SceneResult(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new CommandResult
            {
                Intent = CommandIntent.Unknown,
                ToolName = "scene.empty",
                ShouldSendToOllama = false,
                ValidationErrors = { "Ange vad scenen ska visa. Exempel: /scen senaste nyheterna om AI, eller: presentera Stockholm väder." }
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.SceneShow,
            Risk = CommandRisk.SafeUi,
            ToolName = "scene.show",
            Arguments = { ["query"] = query },
            ShouldSendToOllama = false
        };
    }

    private static CommandResult VoiceVoiceResult(string voice)
    {
        if (string.IsNullOrWhiteSpace(voice))
        {
            return new CommandResult
            {
                Intent = CommandIntent.Unknown,
                ToolName = "voice.voice.empty",
                ShouldSendToOllama = false,
                ValidationErrors = { "Ange en TTS-röst. Exempel: /voice rost sv_SE-nst-medium. Lista installerade med /voice roster." }
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.VoiceSetVoice,
            Risk = CommandRisk.SafeUi,
            ToolName = "voice.voice.set",
            Arguments = { ["voice"] = voice.Trim() },
            ShouldSendToOllama = false
        };
    }

    private static CommandResult VoiceModelResult(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return new CommandResult
            {
                Intent = CommandIntent.Unknown,
                ToolName = "voice.model.empty",
                ShouldSendToOllama = false,
                ValidationErrors = { "Ange en modell: small eller medium. Exempel: /voice modell medium." }
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.VoiceModelSet,
            Risk = CommandRisk.SafeUi,
            ToolName = "voice.model.set",
            Arguments = { ["model"] = model.ToLowerInvariant() },
            ShouldSendToOllama = false
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

    private static CommandResult ParseTaskSlashCommand(string body, string command)
    {
        if (command is "task" or "tasks" or "task lista" or "tasks lista" or "task list" or "tasks list" or
            "uppgift" or "uppgifter" or "uppgift lista" or "uppgifter lista" or "todo" or "todo lista")
        {
            return new CommandResult
            {
                Intent = CommandIntent.TaskList,
                Risk = CommandRisk.SafeRead,
                ToolName = "task.list",
                ShouldSendToOllama = false
            };
        }

        if (command is "task status" or "tasks status" or "uppgift status" or "uppgifter status" or "todo status")
        {
            return new CommandResult
            {
                Intent = CommandIntent.TaskStatus,
                Risk = CommandRisk.SafeRead,
                ToolName = "task.status",
                ShouldSendToOllama = false
            };
        }

        var addPrefixes = new[] { "task add", "task lagg till", "task skapa", "tasks add", "todo add", "todo lagg till", "uppgift add", "uppgift lagg till", "uppgifter add" };
        if (TryGetMatchingPrefixWordCount(command, addPrefixes, out var addSkip))
        {
            var text = TailAfterWordCount(body, addSkip);
            return new CommandResult
            {
                Intent = CommandIntent.TaskAddRequest,
                Risk = CommandRisk.WritesFile,
                ToolName = "task.add.request",
                Arguments = { ["text"] = text },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        var donePrefixes = new[] { "task done", "task klar", "task complete", "tasks done", "todo done", "uppgift klar", "uppgift done", "uppgifter klar" };
        if (TryGetMatchingPrefixWordCount(command, donePrefixes, out var doneSkip))
        {
            var id = TailAfterWordCount(body, doneSkip);
            return new CommandResult
            {
                Intent = CommandIntent.TaskCompleteRequest,
                Risk = CommandRisk.WritesFile,
                ToolName = "task.complete.request",
                Arguments = { ["id"] = id },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
        }

        var searchPrefixes = new[] { "task sok", "tasks sok", "todo sok", "uppgift sok", "uppgifter sok" };
        if (TryGetMatchingPrefixWordCount(command, searchPrefixes, out var searchSkip))
        {
            var query = TailAfterWordCount(body, searchSkip);
            return new CommandResult
            {
                Intent = CommandIntent.TaskSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "task.search",
                Arguments = { ["query"] = query },
                ShouldSendToOllama = false
            };
        }

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.task.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt /task-kommando. Exempel: /task, /task add Ring banken !red, /task done T..., /task sök banken" }
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

    // Hämtar argumentdelen från raw-input efter ett normalized prefix. Bevarar accenter (å/ä/ö)
    // och original casing. Säker mot multiple-whitespace och case-skillnader.
    // Exempel: ExtractRawArg("Karta Kågeröd Sverige", "karta") -> "Kågeröd Sverige"
    private static string ExtractRawArg(string raw, string normalizedPrefix)
    {
        if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(normalizedPrefix)) return "";
        var prefixTokens = normalizedPrefix.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var rawTokens = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (rawTokens.Length <= prefixTokens.Length) return "";
        return string.Join(" ", rawTokens.Skip(prefixTokens.Length));
    }

    private static bool IsProjectIndexRequest(string command)
    {
        return command is
            "las filerna" or
            "analysera projektet" or
            "las hela repo" or
            "ga igenom allt" or
            "forsta projektet";
    }

    private static bool IsProjectAuditRequest(string command)
    {
        return command is
            "skapa audit" or
            "gor audit" or
            "gör audit" or
            "projekt audit" or
            "audit projekt" or
            "deep audit";
    }

    private static bool TryParseProjectIndexSearch(string raw, string command, out string query)
    {
        query = "";

        if (command == "sok i projektindex" || command.StartsWith("sok i projektindex "))
            query = TailAfterWordCount(raw, 3);
        else if (command == "sok projektindex" || command.StartsWith("sok projektindex "))
            query = TailAfterWordCount(raw, 2);
        else if (command == "projektindex sok" || command.StartsWith("projektindex sok "))
            query = TailAfterWordCount(raw, 2);
        else if (command == "hitta i projektindex" || command.StartsWith("hitta i projektindex "))
            query = TailAfterWordCount(raw, 3);

        return !string.IsNullOrWhiteSpace(query);
    }

    private static bool IsTaskCommand(string command)
    {
        return command == "task" || command.StartsWith("task ") ||
               command == "tasks" || command.StartsWith("tasks ") ||
               command == "todo" || command.StartsWith("todo ") ||
               command == "uppgift" || command.StartsWith("uppgift ") ||
               command == "uppgifter" || command.StartsWith("uppgifter ");
    }

    private static bool TryParseNaturalTaskCommand(string raw, string command, out CommandResult result)
    {
        result = new CommandResult();

        if (command is "visa tasks" or "visa task" or "visa uppgifter" or "mina tasks" or "mina uppgifter")
        {
            result = new CommandResult
            {
                Intent = CommandIntent.TaskList,
                Risk = CommandRisk.SafeRead,
                ToolName = "task.list",
                ShouldSendToOllama = false
            };
            return true;
        }

        if (StartsWithAny(command, "lagg till task", "lägg till task", "lagg till uppgift", "lägg till uppgift", "skapa task", "skapa uppgift"))
        {
            var skip = command.StartsWith("skapa") ? 2 : 3;
            result = new CommandResult
            {
                Intent = CommandIntent.TaskAddRequest,
                Risk = CommandRisk.WritesFile,
                ToolName = "task.add.request",
                Arguments = { ["text"] = TailAfterWordCount(raw, skip) },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
            return true;
        }

        if (StartsWithAny(command, "klar task", "klar uppgift", "markera task klar", "markera uppgift klar"))
        {
            var skip = command.StartsWith("markera") ? 3 : 2;
            result = new CommandResult
            {
                Intent = CommandIntent.TaskCompleteRequest,
                Risk = CommandRisk.WritesFile,
                ToolName = "task.complete.request",
                Arguments = { ["id"] = TailAfterWordCount(raw, skip) },
                RequiresApproval = true,
                ShouldSendToOllama = false
            };
            return true;
        }

        if (StartsWithAny(command, "sok task", "sök task", "sok uppgift", "sök uppgift", "hitta task", "hitta uppgift"))
        {
            result = new CommandResult
            {
                Intent = CommandIntent.TaskSearch,
                Risk = CommandRisk.SafeRead,
                ToolName = "task.search",
                Arguments = { ["query"] = TailAfterWordCount(raw, 2) },
                ShouldSendToOllama = false
            };
            return true;
        }

        return false;
    }

    private static bool StartsWithAny(string command, params string[] prefixes)
    {
        return prefixes.Any(prefix => command == prefix || command.StartsWith(prefix + " ", StringComparison.Ordinal));
    }

    private static bool TryGetMatchingPrefixWordCount(string command, string[] prefixes, out int wordCount)
    {
        foreach (var prefix in prefixes.OrderByDescending(prefix => prefix.Length))
        {
            if (command == prefix || command.StartsWith(prefix + " ", StringComparison.Ordinal))
            {
                wordCount = prefix.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                return true;
            }
        }

        wordCount = 0;
        return false;
    }
}
