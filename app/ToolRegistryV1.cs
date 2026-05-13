namespace JarvisClean;

internal sealed class ToolDefinitionV1
{
    public string Name { get; init; } = string.Empty;
    public CommandIntent Intent { get; init; } = CommandIntent.Unknown;
    public CommandRisk Risk { get; init; } = CommandRisk.SafeRead;
    public bool RequiresApproval { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Example { get; init; } = string.Empty;
    public List<string> RequiredArguments { get; init; } = new();
}

internal static class ToolRegistryV1
{
    public static IReadOnlyList<ToolDefinitionV1> Tools { get; } = new List<ToolDefinitionV1>
    {
        new()
        {
            Name = "help.show",
            Intent = CommandIntent.Help,
            Risk = CommandRisk.SafeRead,
            Description = "Visar hjälp och kommandon.",
            Example = "hjälp"
        },

        new()
        {
            Name = "memory.save",
            Intent = CommandIntent.MemorySave,
            Risk = CommandRisk.SafeRead,
            Description = "Sparar ett minne efter validering.",
            Example = "viktigt minne: text",
            RequiredArguments = { "text" }
        },

        new()
        {
            Name = "memory.show",
            Intent = CommandIntent.MemoryShow,
            Risk = CommandRisk.SafeRead,
            Description = "Visar aktivt lokalt minne.",
            Example = "/minne visa"
        },

        new()
        {
            Name = "memory.show.important",
            Intent = CommandIntent.MemoryImportantShow,
            Risk = CommandRisk.SafeRead,
            Description = "Visar viktiga minnen.",
            Example = "/minne viktiga"
        },

        new()
        {
            Name = "memory.show.project",
            Intent = CommandIntent.MemoryProjectShow,
            Risk = CommandRisk.SafeRead,
            Description = "Visar projektminnen.",
            Example = "/minne projekt"
        },

        new()
        {
            Name = "memory.status",
            Intent = CommandIntent.MemoryStatus,
            Risk = CommandRisk.SafeRead,
            Description = "Visar minnesstatus utan att skriva något.",
            Example = "/minne status"
        },

        new()
        {
            Name = "memory.search",
            Intent = CommandIntent.MemorySearch,
            Risk = CommandRisk.SafeRead,
            Description = "Söker i aktivt minne.",
            Example = "sök minne: ollama",
            RequiredArguments = { "query" }
        },

        new()
        {
            Name = "archive.search",
            Intent = CommandIntent.MemoryArchiveSearch,
            Risk = CommandRisk.SafeRead,
            Description = "Söker i memory_archive.md.",
            Example = "sök arkiv: röd",
            RequiredArguments = { "query" }
        },

        new()
        {
            Name = "task.list",
            Intent = CommandIntent.TaskList,
            Risk = CommandRisk.SafeRead,
            Description = "Visar öppna tasks i prioriteringsordning.",
            Example = "/task"
        },

        new()
        {
            Name = "task.status",
            Intent = CommandIntent.TaskStatus,
            Risk = CommandRisk.SafeRead,
            Description = "Visar taskstatus och senaste task-ändring.",
            Example = "/task status"
        },

        new()
        {
            Name = "task.add.request",
            Intent = CommandIntent.TaskAddRequest,
            Risk = CommandRisk.WritesFile,
            RequiresApproval = true,
            Description = "Förbereder ny task via pending approval.",
            Example = "/task add Ring banken !red",
            RequiredArguments = { "text" }
        },

        new()
        {
            Name = "task.complete.request",
            Intent = CommandIntent.TaskCompleteRequest,
            Risk = CommandRisk.WritesFile,
            RequiresApproval = true,
            Description = "Förbereder task som klar via pending approval.",
            Example = "/task done T20260513...",
            RequiredArguments = { "id" }
        },

        new()
        {
            Name = "task.search",
            Intent = CommandIntent.TaskSearch,
            Risk = CommandRisk.SafeRead,
            Description = "Söker i lokala tasks.",
            Example = "/task sök banken",
            RequiredArguments = { "query" }
        },

        new()
        {
            Name = "file.open",
            Intent = CommandIntent.FileOpen,
            Risk = CommandRisk.SafeUi,
            Description = "Öppnar fil i Jarvis filpanel.",
            Example = "öppna app/Program.cs",
            RequiredArguments = { "path" }
        },

        new()
        {
            Name = "file.read",
            Intent = CommandIntent.FileRead,
            Risk = CommandRisk.SafeRead,
            Description = "Läser en projektfil.",
            Example = "läs fil: docs/PROJECT_INDEX.md",
            RequiredArguments = { "path" }
        },

        new()
        {
            Name = "file.create.request",
            Intent = CommandIntent.FileCreateRequest,
            Risk = CommandRisk.WritesFile,
            RequiresApproval = true,
            Description = "Förbereder skapande av ny projektfil via pending approval.",
            Example = "/fil skapa docs/test.md = text",
            RequiredArguments = { "path", "text" }
        },

        new()
        {
            Name = "file.write.request",
            Intent = CommandIntent.FileWriteRequest,
            Risk = CommandRisk.WritesFile,
            RequiresApproval = true,
            Description = "Förbereder filskrivning. Ska senare gå via pending approval.",
            Example = "skriv fil: docs/test.md = text",
            RequiredArguments = { "path", "text" }
        },

        new()
        {
            Name = "terminal.preview",
            Intent = CommandIntent.TerminalPreview,
            Risk = CommandRisk.RunsTerminal,
            RequiresApproval = true,
            Description = "Förhandsgranskar terminalkommando innan körning.",
            Example = "terminal preview: dotnet build",
            RequiredArguments = { "command" }
        },

        new()
        {
            Name = "terminal.confirm",
            Intent = CommandIntent.TerminalConfirm,
            Risk = CommandRisk.RunsTerminal,
            RequiresApproval = true,
            Description = "Godkänner och kör ett pending terminalkommando.",
            Example = "/terminal godkänn"
        },

        new()
        {
            Name = "terminal.cancel",
            Intent = CommandIntent.TerminalCancel,
            Risk = CommandRisk.SafeUi,
            Description = "Avbryter ett pending terminalkommando.",
            Example = "/terminal avbryt"
        },

        new()
        {
            Name = "terminal.show",
            Intent = CommandIntent.TerminalShow,
            Risk = CommandRisk.SafeRead,
            Description = "Visar senaste terminaltranskriptet.",
            Example = "/terminal visa"
        },

        new()
        {
            Name = "obsidian.status",
            Intent = CommandIntent.ObsidianStatus,
            Risk = CommandRisk.SafeRead,
            Description = "Visar säker Obsidian-status. Skriver inte till vault.",
            Example = "/obsidian status"
        },

        new()
        {
            Name = "overview.show",
            Intent = CommandIntent.OverviewShow,
            Risk = CommandRisk.SafeUi,
            Description = "Öppnar Jarvis översiktspanel.",
            Example = "/översikt"
        },

        new()
        {
            Name = "project.index.start",
            Intent = CommandIntent.ProjectIndex,
            Risk = CommandRisk.SafeRead,
            Description = "Startar read-only projektindexering som bakgrundsjobb.",
            Example = "/projekt index"
        },

        new()
        {
            Name = "project.index.search",
            Intent = CommandIntent.ProjectIndexSearch,
            Risk = CommandRisk.SafeRead,
            Description = "Söker i lokalt Project Index utan att skicka hela projektet till LLM.",
            Example = "/projekt sök CommandRouter",
            RequiredArguments = { "query" }
        },

        new()
        {
            Name = "project.audit.start",
            Intent = CommandIntent.ProjectAudit,
            Risk = CommandRisk.SafeRead,
            Description = "Startar en read-only audit som bakgrundsjobb och sparar rapport i data/jobs.",
            Example = "/projekt audit"
        },

        new()
        {
            Name = "jobs.list",
            Intent = CommandIntent.JobList,
            Risk = CommandRisk.SafeRead,
            Description = "Visar bakgrundsjobb.",
            Example = "/jobb"
        },

        new()
        {
            Name = "jobs.status",
            Intent = CommandIntent.JobStatus,
            Risk = CommandRisk.SafeRead,
            Description = "Visar senaste bakgrundsjobbet.",
            Example = "/jobb status"
        },

        new()
        {
            Name = "jobs.cancel",
            Intent = CommandIntent.JobCancel,
            Risk = CommandRisk.SafeUi,
            Description = "Avbryter aktivt bakgrundsjobb.",
            Example = "/jobb avbryt"
        }
    };

    public static ToolDefinitionV1? FindByIntent(CommandIntent intent)
    {
        return Tools.FirstOrDefault(tool => tool.Intent == intent);
    }

    public static ToolDefinitionV1? FindByName(string name)
    {
        return Tools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
