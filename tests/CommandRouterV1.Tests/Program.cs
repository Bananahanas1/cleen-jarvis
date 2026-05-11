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

    ("/explorer is not a known slash command (Project Explorer is the explorer)", () =>
    {
        var result = CommandRouterV1.Parse("/explorer");
        AssertEqual(CommandIntent.Unknown, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "unknown slash must stay local");
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
    }),

    // === Vault (BR2/BR6) ===

    ("/vault status routes to VaultStatus", () =>
    {
        var result = CommandRouterV1.Parse("/vault status");
        AssertEqual(CommandIntent.VaultStatus, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "must stay local");
    }),

    ("/vault på toggles auto-context on", () =>
    {
        var result = CommandRouterV1.Parse("/vault på");
        AssertEqual(CommandIntent.VaultToggle, result.Intent, "intent");
        AssertEqual("true", result.Arguments.GetValueOrDefault("on", ""), "on=true");
    }),

    ("/vault av toggles auto-context off", () =>
    {
        var result = CommandRouterV1.Parse("/vault av");
        AssertEqual(CommandIntent.VaultToggle, result.Intent, "intent");
        AssertEqual("false", result.Arguments.GetValueOrDefault("on", ""), "on=false");
    }),

    ("/vault sök <query> keeps query and stays local", () =>
    {
        var result = CommandRouterV1.Parse("/vault sök unification plan");
        AssertEqual(CommandIntent.VaultSearch, result.Intent, "intent");
        AssertEqual("unification plan", result.Arguments.GetValueOrDefault("query", ""), "query");
    }),

    ("/vault skapa parses name and content via = separator", () =>
    {
        var result = CommandRouterV1.Parse("/vault skapa my-note = some content here");
        AssertEqual(CommandIntent.VaultCreate, result.Intent, "intent");
        AssertEqual("my-note", result.Arguments.GetValueOrDefault("name", ""), "name");
        AssertEqual("some content here", result.Arguments.GetValueOrDefault("text", ""), "text");
        AssertTrue(result.RequiresApproval, "create must require approval");
        AssertCommandValid(result);
    }),

    ("validator blocks vault create without approval flag", () =>
    {
        var result = new CommandResult
        {
            Intent = CommandIntent.VaultCreate,
            Arguments = { ["name"] = "my-note", ["text"] = "some content" },
            RequiresApproval = false
        };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Any(e => e.Contains("pending") || e.Contains("godkännande")), "vault create must require approval");
    }),

    // === B3: NaturalEditTool ===

    ("/edit parses path and instruction via = separator", () =>
    {
        var result = CommandRouterV1.Parse("/edit docs/test.md = gör texten tydligare");
        AssertEqual(CommandIntent.NaturalCodeEdit, result.Intent, "intent");
        AssertEqual("natural_edit.request", result.ToolName, "tool");
        AssertEqual("docs/test.md", result.Arguments.GetValueOrDefault("path", ""), "path");
        AssertEqual("gör texten tydligare", result.Arguments.GetValueOrDefault("instruction", ""), "instruction");
        AssertTrue(result.RequiresApproval, "natural edit must require approval");
        AssertFalse(result.ShouldSendToOllama, "natural edit slash command must stay local");
        AssertCommandValid(result);
    }),

    ("/edit without instruction is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/edit docs/test.md");
        AssertEqual(CommandIntent.NaturalCodeEdit, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "empty natural edit must stay local");
        AssertCommandInvalid(result);
    }),

    ("validator blocks natural edit without approval flag", () =>
    {
        var result = new CommandResult
        {
            Intent = CommandIntent.NaturalCodeEdit,
            Arguments = { ["path"] = "docs/test.md", ["instruction"] = "gör texten tydligare" },
            RequiresApproval = false
        };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Any(e => e.Contains("pending") || e.Contains("godkännande")), "natural edit must require approval");
    }),

    ("NaturalEditTool parses Swedish natural edit command", () =>
    {
        var ok = NaturalEditTool.TryParseNaturalLanguage("gå in i docs/test.md och gör texten tydligare", out var request);
        AssertTrue(ok, "natural edit parse");
        AssertEqual("docs/test.md", request.RelativePath, "path");
        AssertEqual("gör texten tydligare", request.Instruction, "instruction");
    }),

    ("NaturalEditTool cleans fenced model output", () =>
    {
        var cleaned = NaturalEditTool.CleanFullFileContent("```md\n# Hej\nText\n```");
        AssertEqual("# Hej\nText", cleaned, "cleaned fenced content");
    }),

    // === B4: BuilderMode ===

    ("/bygg with description starts BuilderMode locally", () =>
    {
        var result = CommandRouterV1.Parse("/bygg en enkel TODO-app i HTML");
        AssertEqual(CommandIntent.BuilderStart, result.Intent, "intent");
        AssertEqual("builder.start", result.ToolName, "tool");
        AssertEqual("en enkel TODO-app i HTML", result.Arguments.GetValueOrDefault("description", ""), "description");
        AssertFalse(result.ShouldSendToOllama, "builder start must stay local");
        AssertCommandValid(result);
    }),

    ("/bygg without description is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/bygg");
        AssertEqual(CommandIntent.BuilderStart, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "empty builder command must stay local");
        AssertCommandInvalid(result);
    }),

    ("/bygg svar keeps answer locally", () =>
    {
        var result = CommandRouterV1.Parse("/bygg svar målgrupp: bara jag");
        AssertEqual(CommandIntent.BuilderAnswer, result.Intent, "intent");
        AssertEqual("builder.answer", result.ToolName, "tool");
        AssertEqual("målgrupp: bara jag", result.Arguments.GetValueOrDefault("answer", ""), "answer");
        AssertFalse(result.ShouldSendToOllama, "builder answer must stay local");
        AssertCommandValid(result);
    }),

    ("/bygg plan requires approval because it creates PLAN.md", () =>
    {
        var result = CommandRouterV1.Parse("/bygg plan");
        AssertEqual(CommandIntent.BuilderPlan, result.Intent, "intent");
        AssertEqual("builder.plan", result.ToolName, "tool");
        AssertTrue(result.RequiresApproval, "builder plan must require approval");
        AssertFalse(result.ShouldSendToOllama, "builder plan must stay local");
        AssertCommandValid(result);
    }),

    ("/bygg status and avbryt route locally", () =>
    {
        var status = CommandRouterV1.Parse("/bygg status");
        AssertEqual(CommandIntent.BuilderStatus, status.Intent, "status intent");
        AssertFalse(status.ShouldSendToOllama, "builder status must stay local");

        var cancel = CommandRouterV1.Parse("/bygg avbryt");
        AssertEqual(CommandIntent.BuilderCancel, cancel.Intent, "cancel intent");
        AssertFalse(cancel.ShouldSendToOllama, "builder cancel must stay local");
    }),

    ("BuilderMode creates safe vault build plan path", () =>
    {
        var session = BuilderMode.Start("En TODO-app i HTML!", "1. Fråga?");
        AssertTrue(session.Slug.Contains("todo-app"), "slug should include sanitized topic");
        AssertTrue(BuilderMode.PlanRelativePath(session).StartsWith("vault/builds/"), "plan path in vault/builds");
        AssertTrue(BuilderMode.PlanRelativePath(session).EndsWith("/PLAN.md"), "plan path ends with PLAN.md");
        BuilderMode.Clear();
    }),

    ("BuilderMode markdown records pending-approval warning", () =>
    {
        var session = BuilderMode.Start("En enkel app", "1. Fråga?");
        BuilderMode.AddAnswer("Svar");
        var markdown = BuilderMode.BuildPlanMarkdown(session, "- Plan");
        AssertTrue(markdown.Contains("PendingApproval"), "plan should mention PendingApproval");
        AssertTrue(markdown.Contains("Originalidé"), "plan should include original idea");
        BuilderMode.Clear();
    }),

    // === D1/D3/D4: UI-TARS desktop-control ===

    ("/desktop status routes locally", () =>
    {
        var result = CommandRouterV1.Parse("/desktop status");
        AssertEqual(CommandIntent.DesktopStatus, result.Intent, "intent");
        AssertEqual("desktop.status", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "desktop status must stay local");
        AssertCommandValid(result);
    }),

    ("/desktop på and av route locally", () =>
    {
        var on = CommandRouterV1.Parse("/desktop på");
        AssertEqual(CommandIntent.DesktopEnable, on.Intent, "enable intent");
        AssertFalse(on.ShouldSendToOllama, "desktop enable must stay local");
        AssertCommandValid(on);

        var off = CommandRouterV1.Parse("/desktop av");
        AssertEqual(CommandIntent.DesktopDisable, off.Intent, "disable intent");
        AssertFalse(off.ShouldSendToOllama, "desktop disable must stay local");
        AssertCommandValid(off);
    }),

    ("/desktop tars start and stop route locally", () =>
    {
        var start = CommandRouterV1.Parse("/desktop tars start");
        AssertEqual(CommandIntent.DesktopBridgeStart, start.Intent, "start intent");
        AssertEqual("desktop.bridge.start", start.ToolName, "start tool");
        AssertFalse(start.ShouldSendToOllama, "bridge start must stay local");
        AssertCommandValid(start);

        var stop = CommandRouterV1.Parse("/desktop tars stop");
        AssertEqual(CommandIntent.DesktopBridgeStop, stop.Intent, "stop intent");
        AssertEqual("desktop.bridge.stop", stop.ToolName, "stop tool");
        AssertFalse(stop.ShouldSendToOllama, "bridge stop must stay local");
        AssertCommandValid(stop);
    }),

    ("/skärm routes locally to screenshot", () =>
    {
        var result = CommandRouterV1.Parse("/skärm");
        AssertEqual(CommandIntent.DesktopScreenshot, result.Intent, "intent");
        AssertEqual("desktop.screenshot", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "screenshot must stay local");
        AssertCommandValid(result);
    }),

    ("/desktop klick creates approval-backed action request", () =>
    {
        var result = CommandRouterV1.Parse("/desktop klick 100 200");
        AssertEqual(CommandIntent.DesktopActionRequest, result.Intent, "intent");
        AssertEqual("desktop.action.request", result.ToolName, "tool");
        AssertEqual("klick", result.Arguments.GetValueOrDefault("action", ""), "action");
        AssertEqual("100 200", result.Arguments.GetValueOrDefault("payload", ""), "payload");
        AssertTrue(result.RequiresApproval, "desktop click must require approval");
        AssertFalse(result.ShouldSendToOllama, "desktop click must stay local");
        AssertCommandValid(result);
    }),

    ("/desktop fråga creates approval-backed UI-TARS request", () =>
    {
        var result = CommandRouterV1.Parse("/desktop fråga klicka på sökrutan");
        AssertEqual(CommandIntent.DesktopVisionRequest, result.Intent, "intent");
        AssertEqual("desktop.vision.request", result.ToolName, "tool");
        AssertEqual("klicka på sökrutan", result.Arguments.GetValueOrDefault("instruction", ""), "instruction");
        AssertTrue(result.RequiresApproval, "desktop vision must require approval");
        AssertFalse(result.ShouldSendToOllama, "desktop vision must stay local");
        AssertCommandValid(result);
    }),

    ("validator blocks desktop action without approval flag", () =>
    {
        var result = new CommandResult
        {
            Intent = CommandIntent.DesktopActionRequest,
            Arguments = { ["action"] = "klick", ["payload"] = "100 200" },
            RequiresApproval = false
        };
        var errors = CommandValidatorV1.Validate(result);
        AssertTrue(errors.Any(e => e.Contains("pending") || e.Contains("godkännande")), "desktop action must require approval");
    }),

    ("DesktopActionRequest parses manual click and UI-TARS click", () =>
    {
        var ok = DesktopActionRequestV1.TryCreateManual("klick", "10 20", out var manual, out var manualError);
        AssertTrue(ok, manualError);
        AssertEqual(DesktopActionKindV1.Click, manual.Kind, "manual kind");
        AssertEqual(10, manual.X, "manual x");
        AssertEqual(20, manual.Y, "manual y");

        var parsed = DesktopActionRequestV1.TryParsePrediction(
            "click(start_box='(30,40)')",
            1920,
            1080,
            "klicka",
            "shot.png",
            out var action,
            out var parseError);
        AssertTrue(parsed, parseError);
        AssertEqual(DesktopActionKindV1.Click, action.Kind, "prediction kind");
        AssertEqual(30, action.X, "prediction x");
        AssertEqual(40, action.Y, "prediction y");
        AssertEqual("ui-tars", action.Source, "prediction source");
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
