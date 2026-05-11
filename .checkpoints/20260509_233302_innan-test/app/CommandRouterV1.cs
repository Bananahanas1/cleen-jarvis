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
    ProgramLaunch
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

        if (command == "checkpoint" || command.StartsWith("checkpoint "))
            return ParseCheckpointSlashCommand(input[1..].Trim(), command);

        return new CommandResult
        {
            Intent = CommandIntent.Unknown,
            ToolName = "slash.unknown",
            ShouldSendToOllama = false,
            ValidationErrors = { "Okänt slash-kommando: /" + input[1..].Trim() + "\nSkriv /hjälp för lokala kommandon." }
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

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");

        return string.Join(" ", normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
