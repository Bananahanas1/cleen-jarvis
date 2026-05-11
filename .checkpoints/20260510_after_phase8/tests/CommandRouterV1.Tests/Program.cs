using JarvisClean;

var tests = new (string Name, Action Test)[]
{
    ("/hjälp routes locally to help", () =>
    {
        var result = CommandRouterV1.Parse("/hjälp");

        AssertEqual(CommandIntent.Help, result.Intent, "intent");
        AssertEqual("help.show", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "slash help must stay local");
        AssertTrue(result.IsValid, "slash help should be valid");
    }),

    ("/status routes locally to status", () =>
    {
        var result = CommandRouterV1.Parse("/status");

        AssertEqual(CommandIntent.Status, result.Intent, "intent");
        AssertEqual("status.show", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "slash status must stay local");
        AssertTrue(result.IsValid, "slash status should be valid");
    }),

    ("old non-slash help behavior still routes locally", () =>
    {
        var result = CommandRouterV1.Parse("hjälp");

        AssertEqual(CommandIntent.Help, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "help must stay local");
    }),

    ("unknown slash command is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/okänd");

        AssertEqual(CommandIntent.Unknown, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "unknown slash command must not reach Ollama");
        AssertFalse(result.IsValid, "unknown slash command should be invalid");
    }),

    ("/minne visa routes locally to memory display", () =>
    {
        var result = CommandRouterV1.Parse("/minne visa");

        AssertEqual(CommandIntent.MemoryShow, result.Intent, "intent");
        AssertEqual("memory.show", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "memory display must stay local");
        AssertCommandValid(result);
    }),

    ("/minne viktiga routes locally to important memories", () =>
    {
        var result = CommandRouterV1.Parse("/minne viktiga");

        AssertEqual(CommandIntent.MemoryImportantShow, result.Intent, "intent");
        AssertEqual("memory.show.important", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "important memories must stay local");
        AssertCommandValid(result);
    }),

    ("/minne projekt routes locally to project memories", () =>
    {
        var result = CommandRouterV1.Parse("/minne projekt");

        AssertEqual(CommandIntent.MemoryProjectShow, result.Intent, "intent");
        AssertEqual("memory.show.project", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "project memories must stay local");
        AssertCommandValid(result);
    }),

    ("/minne status routes locally to memory status", () =>
    {
        var result = CommandRouterV1.Parse("/minne status");

        AssertEqual(CommandIntent.MemoryStatus, result.Intent, "intent");
        AssertEqual("memory.status", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "memory status must stay local");
        AssertCommandValid(result);
    }),

    ("/minne sök keeps query and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/minne sök röd");

        AssertEqual(CommandIntent.MemorySearch, result.Intent, "intent");
        AssertEqual("memory.search", result.ToolName, "tool");
        AssertEqual("röd", result.Arguments["query"], "query");
        AssertFalse(result.ShouldSendToOllama, "memory search must stay local");
        AssertCommandValid(result);
    }),

    ("/minne arkiv sök keeps query and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/minne arkiv sök röd");

        AssertEqual(CommandIntent.MemoryArchiveSearch, result.Intent, "intent");
        AssertEqual("archive.search", result.ToolName, "tool");
        AssertEqual("röd", result.Arguments["query"], "query");
        AssertFalse(result.ShouldSendToOllama, "archive search must stay local");
        AssertCommandValid(result);
    }),

    ("/minne sök without query is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/minne sök");

        AssertEqual(CommandIntent.MemorySearch, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "empty memory search must stay local");
        AssertCommandInvalid(result);
    }),

    ("/obsidian status routes locally to safe obsidian status", () =>
    {
        var result = CommandRouterV1.Parse("/obsidian status");

        AssertEqual(CommandIntent.ObsidianStatus, result.Intent, "intent");
        AssertEqual("obsidian.status", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "obsidian status must stay local");
        AssertCommandValid(result);
    }),

    ("/oversikt routes locally to overview panel", () =>
    {
        var result = CommandRouterV1.Parse("/oversikt");

        AssertEqual(CommandIntent.OverviewShow, result.Intent, "intent");
        AssertEqual("overview.show", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "overview command must stay local");
        AssertCommandValid(result);
    }),

    ("natural overview routes locally to overview panel", () =>
    {
        var result = CommandRouterV1.Parse("visa oversikt");

        AssertEqual(CommandIntent.OverviewShow, result.Intent, "intent");
        AssertEqual("overview.show", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "natural overview must stay local");
        AssertCommandValid(result);
    }),

    ("/fil öppna keeps path and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/fil öppna README.md");

        AssertEqual(CommandIntent.FileOpen, result.Intent, "intent");
        AssertEqual("file.open", result.ToolName, "tool");
        AssertEqual("README.md", result.Arguments["path"], "path");
        AssertFalse(result.ShouldSendToOllama, "file open must stay local");
        AssertCommandValid(result);
    }),

    ("/fil läs keeps path and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/fil läs docs/PROJECT_INDEX.md");

        AssertEqual(CommandIntent.FileRead, result.Intent, "intent");
        AssertEqual("file.read", result.ToolName, "tool");
        AssertEqual("docs/PROJECT_INDEX.md", result.Arguments["path"], "path");
        AssertFalse(result.ShouldSendToOllama, "file read must stay local");
        AssertCommandValid(result);
    }),

    ("/fil öppna without path is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/fil öppna");

        AssertEqual(CommandIntent.FileOpen, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "empty file open must stay local");
        AssertCommandInvalid(result);
    }),

    ("/fil oppna nested test file keeps path and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/fil oppna tests/terminal-approval-safety.test.js");

        AssertEqual(CommandIntent.FileOpen, result.Intent, "intent");
        AssertEqual("file.open", result.ToolName, "tool");
        AssertEqual("tests/terminal-approval-safety.test.js", result.Arguments["path"], "path");
        AssertFalse(result.ShouldSendToOllama, "nested slash file open must stay local");
        AssertCommandValid(result);
    }),

    ("/fil skapa creates a pending file-create intent", () =>
    {
        var result = CommandRouterV1.Parse("/fil skapa docs/test-new.md | hej");

        AssertEqual(CommandIntent.FileCreateRequest, result.Intent, "intent");
        AssertEqual("file.create.request", result.ToolName, "tool");
        AssertEqual("docs/test-new.md", result.Arguments["path"], "path");
        AssertEqual("hej", result.Arguments["text"], "text");
        AssertTrue(result.RequiresApproval, "file create must require pending approval");
        AssertFalse(result.ShouldSendToOllama, "file create must stay local");
        AssertCommandValid(result);
    }),

    ("/fil skapa accepts = as separator (preferred)", () =>
    {
        var result = CommandRouterV1.Parse("/fil skapa docs/test-eq.md = hej från eq");

        AssertEqual(CommandIntent.FileCreateRequest, result.Intent, "intent");
        AssertEqual("file.create.request", result.ToolName, "tool");
        AssertEqual("docs/test-eq.md", result.Arguments["path"], "path");
        AssertEqual("hej från eq", result.Arguments["text"], "text");
        AssertTrue(result.RequiresApproval, "= separator must still require approval");
        AssertFalse(result.ShouldSendToOllama, "= separator must stay local");
        AssertCommandValid(result);
    }),

    ("SplitFileCommandArguments prefers = over later |", () =>
    {
        var parts = CommandRouterV1.SplitFileCommandArguments("docs/foo.md = bar | baz", 2);

        AssertEqual(2, parts.Length, "two parts");
        AssertEqual("docs/foo.md ", parts[0], "path before =");
        AssertEqual(" bar | baz", parts[1], "content keeps later |");
    }),

    ("SplitFileCommandArguments falls back to | when = absent", () =>
    {
        var parts = CommandRouterV1.SplitFileCommandArguments("docs/foo.md | bar", 2);

        AssertEqual(2, parts.Length, "two parts");
        AssertEqual("docs/foo.md ", parts[0], "path before |");
        AssertEqual(" bar", parts[1], "content after |");
    }),

    ("SplitFileCommandArguments uses | first when | appears before =", () =>
    {
        var parts = CommandRouterV1.SplitFileCommandArguments("docs/foo.md | x = y", 2);

        AssertEqual(2, parts.Length, "two parts");
        AssertEqual("docs/foo.md ", parts[0], "path before |");
        AssertEqual(" x = y", parts[1], "content keeps later =");
    }),

    ("/fil skapa without text is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/fil skapa docs/test-new.md");

        AssertEqual(CommandIntent.FileCreateRequest, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "empty file create must stay local");
        AssertCommandInvalid(result);
    }),

    ("/terminal preview keeps command and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/terminal preview dotnet build");

        AssertEqual(CommandIntent.TerminalPreview, result.Intent, "intent");
        AssertEqual("terminal.preview", result.ToolName, "tool");
        AssertEqual("dotnet build", result.Arguments["command"], "command");
        AssertTrue(result.RequiresApproval, "terminal preview must create an approval-backed pending run");
        AssertFalse(result.ShouldSendToOllama, "terminal preview must stay local");
        AssertCommandValid(result);
    }),

    ("/terminal preview without command is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/terminal preview");

        AssertEqual(CommandIntent.TerminalPreview, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "empty terminal preview must stay local");
        AssertCommandInvalid(result);
    }),

    ("/terminal godkann stays local as terminal confirm", () =>
    {
        var result = CommandRouterV1.Parse("/terminal godkann");

        AssertEqual(CommandIntent.TerminalConfirm, result.Intent, "intent");
        AssertEqual("terminal.confirm", result.ToolName, "tool");
        AssertTrue(result.RequiresApproval, "terminal confirm must require pending approval");
        AssertFalse(result.ShouldSendToOllama, "terminal confirm must stay local");
        AssertCommandValid(result);
    }),

    ("/terminal avbryt stays local as terminal cancel", () =>
    {
        var result = CommandRouterV1.Parse("/terminal avbryt");

        AssertEqual(CommandIntent.TerminalCancel, result.Intent, "intent");
        AssertEqual("terminal.cancel", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "terminal cancel must stay local");
        AssertCommandValid(result);
    }),

    ("/terminal visa stays local as terminal transcript display", () =>
    {
        var result = CommandRouterV1.Parse("/terminal visa");

        AssertEqual(CommandIntent.TerminalShow, result.Intent, "intent");
        AssertEqual("terminal.show", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "terminal show must stay local");
        AssertCommandValid(result);
    }),

    ("/fil skriv is not implemented as direct write", () =>
    {
        var result = CommandRouterV1.Parse("/fil skriv docs/test.md | text");

        AssertEqual(CommandIntent.Unknown, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "file write slash command must not reach Ollama");
        AssertCommandInvalid(result);
    }),

    ("/checkpoint skapa with name routes to named create", () =>
    {
        var result = CommandRouterV1.Parse("/checkpoint skapa innan-port");

        AssertEqual(CommandIntent.CheckpointCreate, result.Intent, "intent");
        AssertEqual("checkpoint.create", result.ToolName, "tool");
        AssertEqual("innan-port", result.Arguments.GetValueOrDefault("name", ""), "name argument");
        AssertFalse(result.ShouldSendToOllama, "checkpoint create must stay local");
        AssertCommandValid(result);
    }),

    ("/checkpoint skapa without name still routes locally with empty name", () =>
    {
        var result = CommandRouterV1.Parse("/checkpoint skapa");

        AssertEqual(CommandIntent.CheckpointCreate, result.Intent, "intent");
        AssertEqual("", result.Arguments.GetValueOrDefault("name", ""), "empty name");
        AssertFalse(result.ShouldSendToOllama, "checkpoint create must stay local");
        AssertCommandValid(result);
    }),

    ("/checkpoint lista routes to list", () =>
    {
        var result = CommandRouterV1.Parse("/checkpoint lista");

        AssertEqual(CommandIntent.CheckpointList, result.Intent, "intent");
        AssertEqual("checkpoint.list", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "checkpoint list must stay local");
        AssertCommandValid(result);
    }),

    ("/checkpoint återställ with name routes to named restore", () =>
    {
        var result = CommandRouterV1.Parse("/checkpoint återställ innan-port");

        AssertEqual(CommandIntent.CheckpointRestore, result.Intent, "intent");
        AssertEqual("checkpoint.restore", result.ToolName, "tool");
        AssertEqual("innan-port", result.Arguments.GetValueOrDefault("name", ""), "name argument");
        AssertFalse(result.ShouldSendToOllama, "checkpoint restore must stay local");
        AssertCommandValid(result);
    }),

    ("/checkpoint återställ without name routes to latest restore", () =>
    {
        var result = CommandRouterV1.Parse("/checkpoint återställ");

        AssertEqual(CommandIntent.CheckpointRestore, result.Intent, "intent");
        AssertEqual("", result.Arguments.GetValueOrDefault("name", ""), "empty name = latest");
        AssertFalse(result.ShouldSendToOllama, "checkpoint restore must stay local");
        AssertCommandValid(result);
    }),

    ("unknown /checkpoint subcommand is blocked", () =>
    {
        var result = CommandRouterV1.Parse("/checkpoint randomsub foo");

        AssertEqual(CommandIntent.Unknown, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "unknown checkpoint sub must not reach Ollama");
        AssertCommandInvalid(result);
    }),

    // === CommandValidatorV1 unit tests (Fas A MVP) ===

    ("validator allows valid file open", () =>
    {
        var result = CommandRouterV1.Parse("/fil öppna README.md");
        var errors = CommandValidatorV1.Validate(result);
        AssertEqual(0, errors.Count, "no errors expected");
    }),

    ("validator blocks file open without path", () =>
    {
        var result = new CommandResult { Intent = CommandIntent.FileOpen };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Count > 0, "missing path must produce error");
    }),

    ("validator blocks file create request without approval flag", () =>
    {
        var result = new CommandResult
        {
            Intent = CommandIntent.FileCreateRequest,
            Arguments = { ["path"] = "docs/x.md", ["text"] = "hej" },
            RequiresApproval = false
        };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Any(e => e.Contains("pending") || e.Contains("godkännande")), "must require approval");
    }),

    ("validator blocks terminal preview without command", () =>
    {
        var result = new CommandResult { Intent = CommandIntent.TerminalPreview };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Count > 0, "missing terminal command must error");
    }),

    ("validator blocks memory save without text", () =>
    {
        var result = new CommandResult { Intent = CommandIntent.MemorySave };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Count > 0, "missing memory text must error");
    }),

    ("validator blocks model change without model", () =>
    {
        var result = new CommandResult { Intent = CommandIntent.ModelChange };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Count > 0, "missing model name must error");
    }),

    // === PendingApprovalV1 integration tests (Fas A MVP) ===

    ("pending store starts empty", () =>
    {
        PendingApprovalStoreV1.Clear();
        AssertFalse(PendingApprovalStoreV1.HasPending, "store should start empty");
        AssertEqual(null, PendingApprovalStoreV1.Get(), "Get should be null");
    }),

    ("pending store can set and get a file create approval", () =>
    {
        PendingApprovalStoreV1.Clear();
        var approval = new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileCreate,
            Target = "docs/test.md",
            Content = "hello"
        };
        PendingApprovalStoreV1.Set(approval);
        AssertTrue(PendingApprovalStoreV1.HasPending, "should have pending after set");
        var got = PendingApprovalStoreV1.Get();
        AssertEqual(PendingApprovalTypeV1.FileCreate, got!.Type, "type roundtrip");
        AssertEqual("docs/test.md", got.Target, "target roundtrip");
        AssertEqual("hello", got.Content, "content roundtrip");
        AssertTrue(got.RequiresUserApproval, "must require user approval by default");
    }),

    ("pending store clear removes the approval", () =>
    {
        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.TerminalRun,
            Target = "dotnet build"
        });
        AssertTrue(PendingApprovalStoreV1.HasPending, "set worked");
        PendingApprovalStoreV1.Clear();
        AssertFalse(PendingApprovalStoreV1.HasPending, "clear empties store");
    }),

    ("pending store overwrites previous approval (only one pending at a time)", () =>
    {
        PendingApprovalStoreV1.Clear();
        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileWrite,
            Target = "a.md"
        });
        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileDelete,
            Target = "b.md"
        });
        AssertEqual(PendingApprovalTypeV1.FileDelete, PendingApprovalStoreV1.Get()!.Type, "newer overwrites older");
        AssertEqual("b.md", PendingApprovalStoreV1.Get()!.Target, "target reflects newer");
        PendingApprovalStoreV1.Clear();
    }),

    ("pending approval CreatedAt is populated on creation", () =>
    {
        var approval = new PendingApprovalV1 { Type = PendingApprovalTypeV1.FileWrite };
        AssertFalse(string.IsNullOrWhiteSpace(approval.CreatedAt), "CreatedAt should be set");
        // Format check: yyyy-MM-dd HH:mm:ss
        AssertTrue(System.Text.RegularExpressions.Regex.IsMatch(approval.CreatedAt, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$"), "CreatedAt format yyyy-MM-dd HH:mm:ss");
    }),

    // === Multi-window slash routing (Fas 3) ===

    ("/brain routes to BrainWindowOpen locally", () =>
    {
        var result = CommandRouterV1.Parse("/brain");
        AssertEqual(CommandIntent.BrainWindowOpen, result.Intent, "intent");
        AssertEqual("brain.open", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "brain open must stay local");
        AssertCommandValid(result);
    }),

    ("/explorer routes to FileExplorerOpen locally", () =>
    {
        var result = CommandRouterV1.Parse("/explorer");
        AssertEqual(CommandIntent.FileExplorerOpen, result.Intent, "intent");
        AssertEqual("explorer.open", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "explorer open must stay local");
        AssertCommandValid(result);
    }),

    // === Fas 6: /agent ===

    ("/agent with task routes to AgentRun locally", () =>
    {
        var result = CommandRouterV1.Parse("/agent vad finns i README");
        AssertEqual(CommandIntent.AgentRun, result.Intent, "intent");
        AssertEqual("agent.run", result.ToolName, "tool");
        AssertEqual("vad finns i README", result.Arguments.GetValueOrDefault("task", ""), "task argument");
        AssertFalse(result.ShouldSendToOllama, "agent must stay local");
    }),

    // === Fas 7: ModelCatalog ===

    ("ModelCatalog has 5 profiles", () =>
    {
        AssertEqual(5, ModelCatalog.All.Length, "profile count");
    }),

    ("ModelCatalog FindByNameOrRole matches role", () =>
    {
        var p = ModelCatalog.FindByNameOrRole("code");
        AssertTrue(p is not null, "code role found");
        AssertEqual("qwen2.5-coder:7b", p!.Name, "name");
    }),

    ("ModelCatalog FindByNameOrRole matches exact name", () =>
    {
        var p = ModelCatalog.FindByNameOrRole("qwen3:8b");
        AssertTrue(p is not null, "name found");
        AssertEqual("smart", p!.Role, "role");
    }),

    ("ModelCatalog SelectForCodeTask upgrades fast→coder", () =>
    {
        AssertEqual("qwen2.5-coder:7b", ModelCatalog.SelectForCodeTask("qwen3:1.7b"), "fast upgrades to coder");
    }),

    ("ModelCatalog SelectForCodeTask respects explicit non-fast model", () =>
    {
        AssertEqual("qwen3:8b", ModelCatalog.SelectForCodeTask("qwen3:8b"), "smart stays smart");
        AssertEqual("deepseek-r1:7b", ModelCatalog.SelectForCodeTask("deepseek-r1:7b"), "reason stays reason");
    }),

    ("/modell routes to ModelCatalogList locally", () =>
    {
        var result = CommandRouterV1.Parse("/modell");
        AssertEqual(CommandIntent.ModelCatalogList, result.Intent, "intent");
        AssertEqual("model.list", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "modell list must stay local");
    }),

    ("/modell byt <name> routes to ModelCatalogSwitch with target", () =>
    {
        var result = CommandRouterV1.Parse("/modell byt qwen3:8b");
        AssertEqual(CommandIntent.ModelCatalogSwitch, result.Intent, "intent");
        AssertEqual("qwen3:8b", result.Arguments.GetValueOrDefault("target", ""), "target");
    }),

    ("/modell kod routes to ModelCatalogSwitch with target=kod", () =>
    {
        var result = CommandRouterV1.Parse("/modell kod");
        AssertEqual(CommandIntent.ModelCatalogSwitch, result.Intent, "intent");
        AssertEqual("kod", result.Arguments.GetValueOrDefault("target", ""), "target shortcut");
    })
};

var failures = new List<string>();

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine("PASS " + name);
    }
    catch (Exception ex)
    {
        failures.Add(name + ": " + ex.Message);
        Console.WriteLine("FAIL " + name);
        Console.WriteLine("     " + ex.Message);
    }
}

if (failures.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Failures:");
    foreach (var failure in failures)
        Console.WriteLine("- " + failure);

    Environment.Exit(1);
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual);
}

static void AssertTrue(bool value, string label)
{
    if (!value)
        throw new InvalidOperationException(label + ": expected true");
}

static void AssertFalse(bool value, string label)
{
    if (value)
        throw new InvalidOperationException(label + ": expected false");
}

static void AssertCommandValid(CommandResult result)
{
    var errors = CommandValidatorV1.Validate(result);
    if (errors.Count > 0)
        throw new InvalidOperationException("expected valid command, got: " + string.Join("; ", errors));
}

static void AssertCommandInvalid(CommandResult result)
{
    var errors = CommandValidatorV1.Validate(result);
    if (errors.Count == 0)
        throw new InvalidOperationException("expected invalid command");
}
