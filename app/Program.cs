using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace JarvisClean;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new JarvisForm());
    }
}

public sealed class JarvisForm : Form
{
    private const string ProjectRoot = @"F:\Jarvis-clean";
    private const string DashboardPath = @"F:\Jarvis-clean\dashboard\index.html";
    private const string OllamaUrl = "http://127.0.0.1:11434/api/chat";
    private const string OllamaModel = "qwen2.5-coder:1.5b"; // legacy default; faktisk aktiv modell hålls i _activeModel.

    // Aktiv modell — kan ändras via /modell-kommandon. Sparas till config\model.txt så valet
    // överlever omstart. Default = OllamaModel om inget sparat val finns.
    private static string _activeModel = OllamaModel;
    private const int TerminalRunTimeoutMs = 120000;
    private const int TerminalOutputPreviewMaxLength = 2800;
    private const int TerminalOutputPreviewHeadLength = 1600;
    private const int TerminalOutputPreviewTailLength = 900;

    private readonly WebView2 _webView = new();

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static List<MemoryBlock> PendingForgetMatches = new();

    private sealed record MemoryBlock(string FullBlock, string Preview);
    private sealed record FileChangeReviewLineV1(string Kind, string Text);
    private sealed record FileChangeReviewV1(string Path, string ChangeKind, int Added, int Removed, List<FileChangeReviewLineV1> Lines);
    private sealed record FileUndoSnapshotV1(string Path, string BeforeContent, bool ExistedBefore, string Description);
    private sealed record TerminalPanelV1(string Command, string WorkingDirectory, int ExitCode, bool TimedOut, string Summary, string Output, string Error);
    // BuildStatusV1: senaste dotnet build/publish/test som körts via terminal-panel. Driver Översikt-cellen "Senaste bygge".
    private sealed record BuildStatusV1(string Command, int ExitCode, bool TimedOut, DateTime FinishedAt);
    // MemoryChangeV1: senaste skrivning till data\memory.md. Driver Översikt-cellen "Senaste minnesförändring".
    private sealed record MemoryChangeV1(string Operation, string Preview, DateTime ChangedAt);

    private static FileChangeReviewV1? LatestFileChangeReviewV1;
    private static FileUndoSnapshotV1? LatestFileUndoV1;
    private static TerminalPanelV1? LatestTerminalPanelV1;
    private static int LatestTerminalPanelVersionV1;
    private int SentTerminalPanelVersionV1;
    private static BuildStatusV1? LatestBuildStatusV1;
    private static MemoryChangeV1? LatestMemoryChangeV1;

    // Brain och Explorer är inbyggda paneler i mittpanelen — inte separata Forms.
    // (Refaktor 2026-05-10 efter användarfeedback "ETT program, inga lösa fönster".)

    // NeuroLinked Python-server (always-on per UNIFICATION_PLAN Fas 5).
    // Auto-startas av OnLoad efter dashboard ready. Auto-stoppas av OnMainFormClosing.
    private static readonly NeuroLinkedBridge _neuroLinkedBridge = new();
    private static readonly UiTarsBridge _uiTarsBridge = new();
    private const int DesktopKillHotkeyIdV1 = 0x4A01;
    private const int WmHotkeyV1 = 0x0312;
    private const uint ModAltV1 = 0x0001;
    private const uint ModControlV1 = 0x0002;
    private const uint ModShiftV1 = 0x0004;

    public JarvisForm()
    {
        Text = "Jarvis";
        Width = 1300;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        _webView.Dock = DockStyle.Fill;
        Controls.Add(_webView);

        _instance = this;

        // Läs persistat modellval från config\model.txt om det finns.
        try
        {
            var modelPath = Path.Combine(ProjectRoot, "config", "model.txt");
            if (File.Exists(modelPath))
            {
                var saved = File.ReadAllText(modelPath).Trim();
                if (!string.IsNullOrEmpty(saved)) _activeModel = saved;
            }
        }
        catch { /* default till OllamaModel */ }

        Load += OnLoad;
        FormClosing += OnMainFormClosing;
    }

    // Static instans för WebView-anrop från static metoder.
    private static JarvisForm? _instance;

    // När main-fönstret stängs ska Python-servern stoppas så vi inte läcker python.exe i bakgrunden.
    private void OnMainFormClosing(object? sender, FormClosingEventArgs e)
    {
        DesktopActionGate.Disable();
        try { _uiTarsBridge.StopAsync().GetAwaiter().GetResult(); } catch { }
        try { _neuroLinkedBridge.StopAsync().GetAwaiter().GetResult(); } catch { }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try { RegisterHotKey(Handle, DesktopKillHotkeyIdV1, ModControlV1 | ModShiftV1 | ModAltV1, (uint)Keys.J); } catch { }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        try { UnregisterHotKey(Handle, DesktopKillHotkeyIdV1); } catch { }
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkeyV1 && m.WParam.ToInt32() == DesktopKillHotkeyIdV1)
        {
            DesktopActionGate.Disable();
            if (PendingApprovalStoreV1.Get()?.Type == PendingApprovalTypeV1.DesktopAction)
                PendingApprovalStoreV1.Clear();

            _ = BeginInvoke(new Action(async () =>
            {
                await AddAssistantMessage("Desktop-control hard kill: Ctrl+Shift+Alt+J. Desktop-control är AV och pending desktop-action är rensad.");
                await ShowOrHidePendingApprovalPopupV1Async();
                await SendJarvisOverviewV1Async(showPanel: false);
            }));
            return;
        }

        base.WndProc(ref m);
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // Brain är en inbyggd vy i main-fönstret (inte ett separat Form).
    // Explorer-vy borttagen — Project Explorer (vänsterpanelen) är "the" explorer.
    public static string OpenBrainWindow()
    {
        TriggerWorkspaceMode("brain");
        return "Brain-vyn öppnad i mittpanelen.";
    }

    // Skickar ett mode-byte till dashboarden via WebView2.
    private static void TriggerWorkspaceMode(string mode)
    {
        try
        {
            var view = _instance?._webView?.CoreWebView2;
            if (view is null) return;
            var script = "window.jarvisShowWorkspaceMode && window.jarvisShowWorkspaceMode(" +
                         JsonSerializer.Serialize(mode) + ");";
            _ = view.ExecuteScriptAsync(script);
        }
        catch { }
    }

    // === ModelCatalog (Fas 7): listning + byte ===
    private static string ListModelCatalogTool() =>
        ModelCatalog.FormatList() + "\n\nAktiv modell: " + _activeModel;

    private static string SwitchModelCatalogTool(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return "Skriv: /modell byt <namn-eller-roll>. Exempel: /modell byt qwen3:8b eller /modell kod";
        var profile = ModelCatalog.FindByNameOrRole(target);
        if (profile is null)
            return "Hittade ingen profil för: " + target + "\n\n" + ModelCatalog.FormatList();
        _activeModel = profile.Name;
        // Persist till config\model.txt så valet överlever omstart.
        try
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "config"));
            File.WriteAllText(Path.Combine(ProjectRoot, "config", "model.txt"), profile.Name);
        }
        catch { /* persist är best-effort */ }
        return "Aktiv modell: " + profile.Name + " [" + profile.Role + "]\n" + profile.Description;
    }

    // === Vault-tools (BR2/BR6: Vault som AI-kontext) ===
    private static string VaultStatusTool()
    {
        var (total, lastUsed, lastCount, enabled) = VaultSearcher.Status();
        var lastUsedStr = lastUsed == DateTime.MinValue
            ? "aldrig"
            : lastUsed.ToString("yyyy-MM-dd HH:mm:ss") + " (använde " + lastCount + " noter)";
        return "Vault-status:\n" +
               "- Plats: F:\\Jarvis-clean\\vault\\\n" +
               "- Indexerade noter: " + total + "\n" +
               "- Auto-kontext: " + (enabled ? "PÅ" : "AV") + "\n" +
               "- Senaste användning: " + lastUsedStr + "\n\n" +
               "Toggle: /vault på  eller  /vault av\n" +
               "Sök: /vault sök <query>\n" +
               "Skapa not: /vault skapa <namn> = <text>";
    }

    private static string VaultToggleTool(bool on)
    {
        VaultSearcher.AutoContextEnabled = on;
        return on
            ? "Vault auto-kontext PÅ. Jarvis läser nu topp 5 vault-noter innan varje svar."
            : "Vault auto-kontext AV. Jarvis svarar utan vault-kontext (bara memory.md).";
    }

    private static string VaultSearchTool(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Skriv: /vault sök <query>. Exempel: /vault sök unification plan";
        var hits = VaultSearcher.TopMatches(query, k: 10);
        if (hits.Count == 0) return "Inga vault-träffar för: " + query;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vault-träffar för \"" + query + "\":");
        foreach (var (relPath, title, excerpt, score) in hits)
        {
            sb.AppendLine();
            sb.AppendLine("[" + score + "] " + relPath);
            sb.AppendLine(excerpt.Length > 200 ? excerpt.Substring(0, 200) + "..." : excerpt);
        }
        return sb.ToString();
    }

    // Vault-create uses the same pending file-create path as other writes.
    // The vault note is only written after the user approves the preview.
    private static string VaultCreateTool(string name, string content)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Skriv: /vault skapa <namn> = <text>";
        if (string.IsNullOrWhiteSpace(content)) return "Texten saknas. Format: /vault skapa <namn> = <text>";

        var safeName = SanitizeVaultNoteNameV1(name);
        if (string.IsNullOrEmpty(safeName)) return "Ogiltigt notnamn efter sanering: " + name;

        var relativePath = "vault/auto/" + safeName + ".md";
        var note =
            "---\n" +
            "type: auto-note\n" +
            "created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
            "tags: [auto, jarvis-saved]\n" +
            "---\n\n" +
            "# " + name + "\n\n" +
            content;

        var pendingReply = CreateProjectFileRequestTool(relativePath, note);
        return pendingReply +
               "\n\nVault-noten skrivs först efter godkännande. Vault-index uppdateras efter godkänd skrivning.";
    }

    private static string SanitizeVaultNoteNameV1(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var ch in raw.Trim())
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
            else if (ch == ' ') sb.Append('-');
        }
        var s = sb.ToString().Trim('-', '_');
        if (s.Length > 60) s = s.Substring(0, 60);
        return s;
    }

    // Read-only agent (Fas 6). Skrivverktyg är blockerade tills Fas 8 kopplar dem till PendingApprovalV1.
    private static readonly OllamaAgentHarness _ollamaAgent = new();
    public static async Task<string> RunOllamaAgentToolAsync(string task)
    {
        if (string.IsNullOrWhiteSpace(task))
            return "Skriv '/agent <vad du vill att agenten ska göra>'. Agenten är read-only — inga skrivverktyg.";

        try
        {
            // Auto-upgrade fast→coder för agent-tasks (per ModelCatalog-policyn).
            var modelForAgent = ModelCatalog.SelectForCodeTask(_activeModel);
            var result = await _ollamaAgent.RunAsync(
                endpoint: OllamaUrl,
                model: modelForAgent,
                userTask: task,
                workspaceRoot: ProjectRoot
            );
            var toolsList = result.ToolsUsed.Count == 0
                ? ""
                : "\n\n[Verktyg: " + string.Join(", ", result.ToolsUsed.Distinct()) + " · " + result.Steps + " steg]";
            return result.Answer + toolsList;
        }
        catch (Exception ex)
        {
            return "Agentkörning misslyckades: " + ex.Message;
        }
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        if (!File.Exists(DashboardPath))
        {
            MessageBox.Show("Hittar inte dashboarden:\n" + DashboardPath);
            return;
        }

        Directory.CreateDirectory(@"F:\DevCache\WebView2\Jarvis");

        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: @"F:\DevCache\WebView2\Jarvis"
        );

        await _webView.EnsureCoreWebView2Async(env);
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        // Virtual host så dashboarden kan importera Three.js från /vendor/ och graph.json
        // utan CDN-beroende. Refaktor 2026-05-10: gick från NavigateToString till virtual host
        // för att brain-vyn ska kunna använda lokal Three.js.
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "jarvis.local",
            @"F:\Jarvis-clean",
            CoreWebView2HostResourceAccessKind.Allow);

        _webView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            await AddAssistantMessage("C#-bryggan är ansluten. Jag förstår nu enklare felstavningar i kommandon.");
            await SendJarvisOverviewV1Async(showPanel: false);

            // Always-on brain: starta NeuroLinked Python-server i bakgrunden.
            // Misslyckas tyst om Python saknas — main fortsätter funka. UI-chip visar status.
            _ = Task.Run(async () =>
            {
                await _neuroLinkedBridge.StartAsync();
                try { await SendBrainStatusChipAsync(); } catch { }
            });
        };

        // Navigera till dashboard via virtual host så relativa /vendor/ och /graphify-out/ fungerar.
        _webView.CoreWebView2.Navigate("https://jarvis.local/dashboard/index.html");
    }

    // Skickar NeuroLinked-statusen till dashboarden så användaren ser om brain är redo.
    private async Task SendBrainStatusChipAsync()
    {
        if (_webView.CoreWebView2 is null) return;
        var chip = JsonSerializer.Serialize(_neuroLinkedBridge.StatusChip());
        try
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(
                "window.jarvisSetBrainStatusChip && window.jarvisSetBrainStatusChip(" + chip + ");"
            );
        }
        catch { }
    }

    // Skickar senaste använda vault-noter till brain-vyn så de kan glöda i 3D.
    private async Task SendVaultActiveNodesAsync()
    {
        var paths = VaultSearcher.LastUsedRelPaths;
        if (paths.Count == 0 || _webView?.CoreWebView2 is null) return;
        try
        {
            var json = JsonSerializer.Serialize(paths);
            await _webView.CoreWebView2.ExecuteScriptAsync(
                "window.jarvisBrainSetActiveVaultNodes && window.jarvisBrainSetActiveVaultNodes(" + json + ");"
            );
        }
        catch { }
    }

    // === Brain-vy: skicka fil-graf till dashboarden ===
    private async Task SendBrainGraphToDashboardAsync()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var graphJson = await Task.Run(() => FileGraphBuilder.BuildJsonForRootCached(ProjectRoot));
            sw.Stop();
            var script = "window.jarvisSetBrainGraphV1 && window.jarvisSetBrainGraphV1(" + graphJson + ");";
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
            if (sw.ElapsedMilliseconds > 2000)
                await AddAssistantMessage("Brain-graf byggd på " + sw.ElapsedMilliseconds + " ms (cachad i 5 min).");
        }
        catch (Exception ex)
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(
                "window.jarvisSetBrainGraphV1 && window.jarvisSetBrainGraphV1({nodes:[],edges:[]});"
            );
            await AddAssistantMessage("Brain-graf-bygge misslyckades: " + ex.Message);
        }
    }

    // Explorer-helpermetoder borttagna 2026-05-10: Project Explorer (vänster) hanterar all fil-listning.

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : "";

            var text = root.TryGetProperty("text", out var textElement)
                ? textElement.GetString() ?? ""
                : "";

            // BRAIN_GRAPH_HANDLERS — fil-graf till Brain-vyn (refaktor 2026-05-10)
            if (type == "brain_request_graph")
            {
                await SendBrainGraphToDashboardAsync();
                return;
            }
            if (type == "brain_rebuild_graph")
            {
                // Rensa cache → tvinga rebuild nästa request
                try {
                    var cache = Path.Combine(ProjectRoot, ".checkpoints", ".brain-graph-cache.json");
                    if (File.Exists(cache)) File.Delete(cache);
                } catch { }
                await SendBrainGraphToDashboardAsync();
                return;
            }

            // EXPLORER_HANDLERS borttagna 2026-05-10: Project Explorer (vänster) hanterar all fil-listning.

            // PROJECT_EXPLORER_MESSAGE_HANDLERS
            if (type == "jarvis_list_files_for_ui")
            {
                await SendProjectFilesToDashboard();
                return;
            }

            if (type == "jarvis_open_file")
            {
                var path = root.TryGetProperty("path", out var pathElement)
                    ? pathElement.GetString() ?? ""
                    : "";

                await SendProjectFileToEditor(path);
                return;
            }
            // PROJECT_EXPLORER_FOLDER_HANDLERS
            if (type == "jarvis_list_folder_for_ui")
            {
                var folderPath = root.TryGetProperty("path", out var folderPathElement)
                    ? folderPathElement.GetString() ?? ""
                    : "";

                await SendProjectFolderToDashboard(folderPath);
                return;
            }
            // Smart file-open compatibility: old dashboard message types all use one router now.
            if (type == "jarvis_open_file_smart" ||
                type == "jarvis_open_file_smart_v4" ||
                type == "jarvis_open_file_smart_v5" ||
                type == "jarvis_open_file_smart_v6" ||
                type == "jarvis_open_file_smart_v7")
            {
                var smartPath = root.TryGetProperty("path", out var smartPathElement)
                    ? smartPathElement.GetString() ?? ""
                    : "";

                var silent = root.TryGetProperty("silent", out var silentElement)
                    && silentElement.ValueKind == JsonValueKind.True;

                var openReply = await OpenProjectFileSmartAsync(smartPath);

                if (!silent)
                    await AddAssistantMessage(openReply);

                return;
            }

            // TREE_EXPLORER_AND_AUTOCOMPLETE_V7_HANDLERS:
            // Tree explorer, file opening and autocomplete must be local tools before Ollama.
            if (type == "jarvis_tree_folder_v7")
            {
                var treePath = root.TryGetProperty("path", out var treePathElement)
                    ? treePathElement.GetString() ?? ""
                    : "";

                await SendTreeFolderV7Async(treePath, refreshAutocomplete: true);
                return;
            }

            if (type == "jarvis_autocomplete_files_v7")
            {
                await SendAutocompleteFilesV7Async();
                return;
            }

            if (type == "jarvis_editor_save_pending_v1")
            {
                var editorPath = root.TryGetProperty("path", out var editorPathElement)
                    ? editorPathElement.GetString() ?? ""
                    : "";

                var editorContent = root.TryGetProperty("content", out var editorContentElement)
                    ? editorContentElement.GetString() ?? ""
                    : "";

                await AddAssistantMessage(EditorSavePendingTool(editorPath, editorContent));
                await ShowOrHidePendingApprovalPopupV1Async();
                await SendJarvisOverviewV1Async(showPanel: false);
                return;
            }

            if (type == "jarvis_pending_approval_v1")
            {
                var action = root.TryGetProperty("action", out var actionElement)
                    ? actionElement.GetString() ?? ""
                    : "";

                var approvalReply = HandlePendingApprovalDecisionV1(action);
                await AddAssistantMessage(approvalReply);
                await ShowOrHidePendingApprovalPopupV1Async();
                await SendLatestFileChangeReviewV1Async();
                await SendLatestTerminalPanelV1Async();
                await SendJarvisOverviewV1Async(showPanel: false);
                return;
            }

            if (type == "jarvis_review_file_change_v1")
            {
                var reviewPath = root.TryGetProperty("path", out var reviewPathElement)
                    ? reviewPathElement.GetString() ?? ""
                    : "";

                await ShowFileChangeReviewV1Async(reviewPath);
                return;
            }

            if (type == "jarvis_undo_last_change_v1")
            {
                var undoReply = UndoLastFileChangeRequestTool();
                await AddAssistantMessage(undoReply);
                await ShowOrHidePendingApprovalPopupV1Async();
                return;
            }

            if (type != "jarvis_chat")
            {
                await AddAssistantMessage("C# fick okänd typ: " + type);
                return;
            }

            if (await TryHandleCommandRouterV1UiAsync(text))
                return;

            if (NaturalEditTool.TryParseNaturalLanguage(text, out var naturalEditRequest))
            {
                var naturalEditReply = await NaturalEditRequestToolAsync(naturalEditRequest.RelativePath, naturalEditRequest.Instruction);
                await AddAssistantMessage(naturalEditReply);
                await ShowOrHidePendingApprovalPopupV1Async();
                await SendJarvisOverviewV1Async(showPanel: false);
                return;
            }

            // File open requests must be handled by local C# tools before Ollama gets the message.
            // File open requests must be handled by local C# tools before Ollama gets the message.
            if (TryExtractSmartOpenFileRequest(text, out var localFileOpen))
            {
                var localFileReply = await OpenProjectFileSmartAsync(localFileOpen);
                await AddAssistantMessage(localFileReply);
                return;
            }
            var reply = await HandleMessageAsync(text);
            await AddAssistantMessage(reply);
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendLatestFileChangeReviewV1Async();
            await SendLatestTerminalPanelV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
        }
        catch (Exception ex)
        {
            await AddAssistantMessage("C#-fel: " + ex.Message);
        }
    }

    private async Task<bool> TryHandleCommandRouterV1UiAsync(string text)
    {
        var routedV1 = CommandRouterV1.Parse(text);
        var routedV1Errors = CommandValidatorV1.Validate(routedV1);

        if (!routedV1.ShouldSendToOllama && routedV1Errors.Count > 0)
        {
            await AddAssistantMessage(string.Join(Environment.NewLine, routedV1Errors.Distinct()));
            return true;
        }

        if (routedV1.Intent == CommandIntent.OverviewShow)
        {
            await SendJarvisOverviewV1Async(showPanel: true);
            await AddAssistantMessage(BuildOverviewTextTool());
            return true;
        }

        if (routedV1.Intent == CommandIntent.MemoryStatus)
        {
            await AddAssistantMessage(MemoryStatus());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.ObsidianStatus)
        {
            await AddAssistantMessage(ObsidianStatusTool());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.FileOpen)
        {
            var path = routedV1.Arguments.GetValueOrDefault("path", "");
            var reply = await OpenProjectFileSmartAsync(path);
            await AddAssistantMessage(reply);
            return true;
        }

        if (routedV1.Intent == CommandIntent.FileRead)
        {
            var path = routedV1.Arguments.GetValueOrDefault("path", "");
            await AddAssistantMessage(ReadProjectFileTool("läs fil: " + path));
            return true;
        }

        if (routedV1.Intent == CommandIntent.FileCreateRequest)
        {
            var path = routedV1.Arguments.GetValueOrDefault("path", "");
            var content = routedV1.Arguments.GetValueOrDefault("text", "");
            await AddAssistantMessage(CreateProjectFileRequestTool(path, content));
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.TerminalPreview)
        {
            var command = routedV1.Arguments.GetValueOrDefault("command", "");
            await AddAssistantMessage(TerminalPreviewTool("terminal preview: " + command));
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.TerminalConfirm)
        {
            await AddAssistantMessage(TerminalConfirmRun());
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendLatestTerminalPanelV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.TerminalCancel)
        {
            await AddAssistantMessage(TerminalCancelRun());
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.TerminalShow)
        {
            await AddAssistantMessage(ShowLatestTerminalTranscriptTool());
            await SendLatestTerminalPanelV1Async();
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopStatus)
        {
            await AddAssistantMessage(DesktopStatusTool());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopEnable)
        {
            await AddAssistantMessage(DesktopEnableTool());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopDisable)
        {
            await AddAssistantMessage(DesktopDisableTool());
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopScreenshot)
        {
            await AddAssistantMessage(DesktopScreenshotTool());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopBridgeStart)
        {
            await AddAssistantMessage(await _uiTarsBridge.StartAsync());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopBridgeStop)
        {
            await AddAssistantMessage(await _uiTarsBridge.StopAsync());
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopActionRequest)
        {
            var action = routedV1.Arguments.GetValueOrDefault("action", "");
            var payload = routedV1.Arguments.GetValueOrDefault("payload", "");
            await AddAssistantMessage(DesktopActionRequestTool(action, payload));
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.DesktopVisionRequest)
        {
            var instruction = routedV1.Arguments.GetValueOrDefault("instruction", "");
            await AddAssistantMessage(await DesktopVisionRequestToolAsync(instruction));
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        if (routedV1.Intent == CommandIntent.NaturalCodeEdit)
        {
            var path = routedV1.Arguments.GetValueOrDefault("path", "");
            var instruction = routedV1.Arguments.GetValueOrDefault("instruction", "");
            await AddAssistantMessage(await NaturalEditRequestToolAsync(path, instruction));
            await ShowOrHidePendingApprovalPopupV1Async();
            await SendJarvisOverviewV1Async(showPanel: false);
            return true;
        }

        return false;
    }

    private async Task ShowOrHidePendingApprovalPopupV1Async()
    {
        if (PendingApprovalStoreV1.HasPending)
        {
            await SendPendingApprovalPopupV1Async();
            return;
        }

        await HidePendingApprovalPopupV1Async();
    }

    private async Task SendPendingApprovalPopupV1Async()
    {
        if (_webView.CoreWebView2 is null)
            return;

        var pending = PendingApprovalStoreV1.Get();
        if (pending is null)
            return;

        var payload = BuildPendingApprovalPopupPayloadV1(pending);
        var payloadJson = JsonSerializer.Serialize(payload);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisShowPendingApprovalV1(" + payloadJson + ");"
        );
    }

    private async Task HidePendingApprovalPopupV1Async()
    {
        if (_webView.CoreWebView2 is null)
            return;

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisHidePendingApprovalV1();"
        );
    }

    private async Task SendJarvisOverviewV1Async(bool showPanel)
    {
        if (_webView.CoreWebView2 is null)
            return;

        var payloadJson = JsonSerializer.Serialize(BuildJarvisOverviewPayloadV1());
        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisSetJarvisOverviewV1(" + payloadJson + ");"
        );

        if (showPanel)
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(
                "window.jarvisShowVisualPanelV1();"
            );
        }
    }

    private static Dictionary<string, string> BuildJarvisOverviewPayloadV1()
    {
        return new Dictionary<string, string>
        {
            ["memory"] = BuildMemoryOverviewLineV1(),
            ["obsidian"] = BuildObsidianOverviewLineV1(),
            ["buildStatus"] = BuildBuildStatusOverviewLineV1(),
            ["memoryChange"] = BuildMemoryChangeOverviewLineV1(),
            ["vault"] = BuildVaultOverviewLineV1(),
            ["desktop"] = "Desktop-control: " + (DesktopActionGate.Enabled ? "PÅ" : "AV") + "\n" + (_uiTarsBridge.IsAvailable() ? "UI-TARS source hittad" : "UI-TARS source saknas"),
            ["loop"] = "Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember\nAuto-agent: av. Jarvis tänker när du ber honom eller när ett lokalt state uppdateras.",
            ["architecture"] = "Fas 1: säkrare kärna. Fas 2: developer workspace. Fas 3: svensk NL-routing till validerade intents. 3D/Obsidian/NeuroLink blir paneler ovanpå detta."
        };
    }

    private static string BuildVaultOverviewLineV1()
    {
        var (total, lastUsed, lastCount, enabled) = VaultSearcher.Status();
        var status = enabled ? "PÅ" : "AV";
        if (lastUsed == DateTime.MinValue)
            return total + " noter | auto-kontext: " + status + "\nÄnnu inte använt i ett svar";
        var ago = (DateTime.Now - lastUsed).TotalMinutes;
        var when = ago < 1 ? "nyss" : ((int)ago) + " min sen";
        return total + " noter | auto-kontext: " + status + "\nSenaste svar: " + lastCount + " noter (" + when + ")";
    }

    private static string BuildBuildStatusOverviewLineV1()
    {
        if (LatestBuildStatusV1 is null)
            return "Ingen byggstatus ännu.";

        var status = LatestBuildStatusV1;
        var verdict = status.TimedOut
            ? "TIMEOUT"
            : (status.ExitCode == 0 ? "OK" : "FAIL");
        return verdict + " (exit " + status.ExitCode + ")\n" +
               status.Command + "\n" +
               status.FinishedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private static string BuildMemoryChangeOverviewLineV1()
    {
        if (LatestMemoryChangeV1 is null)
            return "Ingen minnesförändring ännu.";

        var change = LatestMemoryChangeV1;
        var preview = string.IsNullOrWhiteSpace(change.Preview) ? "(ingen preview)" : change.Preview;
        return change.Operation + " " + change.ChangedAt.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + preview;
    }

    private static string BuildMemoryOverviewLineV1()
    {
        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");
        if (!File.Exists(memoryPath))
            return "Lokalt minne: ingen memory.md ännu.";

        var lines = File.ReadAllLines(memoryPath);
        var memoryCount = lines.Count(line => line.StartsWith("## "));
        var importantCount = lines.Count(line => line.Trim().Equals("Type: important", StringComparison.OrdinalIgnoreCase));
        var projectCount = lines.Count(line => line.Trim().Equals("Type: project_fact", StringComparison.OrdinalIgnoreCase));

        return "Lokalt minne: " + memoryCount + " minnen\nViktiga: " + importantCount + "\nProjekt: " + projectCount + "\nFil: data/memory.md";
    }

    private static string BuildObsidianOverviewLineV1()
    {
        var configuredPath = ReadConfiguredObsidianVaultPathV1();
        if (string.IsNullOrWhiteSpace(configuredPath))
            return "Obsidian: inte konfigurerat.\nNuvarande läge: read-only planering. Ingen vault skrivs till.";

        if (!Directory.Exists(configuredPath))
            return "Obsidian: konfigurerad sökväg hittas inte.\n" + configuredPath + "\nIngen skrivning sker.";

        return "Obsidian: vault hittad.\n" + configuredPath + "\nNuvarande läge: read-only status, ingen sync-skrivning.";
    }

    private static Dictionary<string, string> BuildPendingApprovalPopupPayloadV1(PendingApprovalV1 pending)
    {
        var target = pending.Target.Replace("\\", "/");

        if (pending.Type == PendingApprovalTypeV1.FileCreate)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn ny fil",
                ["description"] = "Jarvis vill skapa en ny fil: " + target + ".",
                ["meta"] = "Typ: filskapande | Läge: skapa ny\nMål: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        if (pending.Type == PendingApprovalTypeV1.FileAppend)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn filskrivning",
                ["description"] = "Jarvis vill lägga till text i " + target + ".",
                ["meta"] = "Typ: filskrivning | Läge: lägg till\nMål: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        if (pending.Type == PendingApprovalTypeV1.FileWrite)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn filskrivning",
                ["description"] = "Jarvis vill ersätta innehållet i " + target + ".",
                ["meta"] = "Typ: filskrivning | Läge: skriv/ersätt\nMål: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        if (pending.Type == PendingApprovalTypeV1.FileDelete)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn filradering",
                ["description"] = "Jarvis vill radera " + target + ".",
                ["meta"] = "Typ: filradering\nMål: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        if (pending.Type == PendingApprovalTypeV1.FileUndo)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn undo",
                ["description"] = "Jarvis vill ångra senaste filändringen i " + target + ".",
                ["meta"] = "Typ: undo av filändring\nMål: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        if (pending.Type == PendingApprovalTypeV1.TerminalRun)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn terminalkörning",
                ["description"] = "Jarvis vill köra ett förhandsgranskat terminalkommando.",
                ["meta"] = "Arbetsmapp: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        if (pending.Type == PendingApprovalTypeV1.DesktopAction)
        {
            return new Dictionary<string, string>
            {
                ["type"] = pending.Type.ToString(),
                ["title"] = "Godkänn desktop-action",
                ["description"] = "Jarvis vill köra en klick/typ/scroll-action på datorn.",
                ["meta"] = "Typ: desktop-control\nMål: " + target + "\nSkapad: " + pending.CreatedAt,
                ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
            };
        }

        return new Dictionary<string, string>
        {
            ["type"] = pending.Type.ToString(),
            ["title"] = "Godkänn åtgärd",
            ["description"] = "Jarvis vill utföra en åtgärd som kräver ditt godkännande.",
            ["meta"] = "Mål: " + target + "\nSkapad: " + pending.CreatedAt,
            ["preview"] = BuildPendingApprovalPreviewV1(pending.Content)
        };
    }

    private static string HandlePendingApprovalDecisionV1(string action)
    {
        var normalized = NormalizeCommand(action);

        if (normalized is "approve" or "godkann" or "godkänn")
            return ApprovePendingApprovalV1();

        if (normalized is "cancel" or "avbryt")
            return CancelPendingApprovalV1();

        return "Okänt godkännandeval: " + action;
    }

    private static string ApprovePendingApprovalV1()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null)
            return "Det finns inget pending godkännande.";

        if (pending.Type is PendingApprovalTypeV1.FileCreate or PendingApprovalTypeV1.FileWrite or PendingApprovalTypeV1.FileAppend)
            return ApprovePendingFileWriteTool();

        if (pending.Type == PendingApprovalTypeV1.FileDelete)
            return ApprovePendingFileDeleteTool();

        if (pending.Type == PendingApprovalTypeV1.FileUndo)
            return ApprovePendingFileUndoTool();

        if (pending.Type == PendingApprovalTypeV1.TerminalRun)
            return ApprovePendingTerminalRunTool();

        if (pending.Type == PendingApprovalTypeV1.DesktopAction)
            return ApprovePendingDesktopActionTool();

        return "Den här pending-typen stöds inte av popupen ännu: " + pending.Type;
    }

    private static string CancelPendingApprovalV1()
    {
        return CancelCurrentPendingApprovalTool();
    }

    private static string CancelCurrentPendingApprovalTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null)
            return "Det finns inget pending att avbryta.";

        if (pending.Type is PendingApprovalTypeV1.FileCreate or PendingApprovalTypeV1.FileWrite or PendingApprovalTypeV1.FileAppend)
            return CancelPendingFileWriteTool();

        if (pending.Type == PendingApprovalTypeV1.FileDelete)
            return CancelPendingFileDeleteTool();

        if (pending.Type == PendingApprovalTypeV1.FileUndo)
            return CancelPendingFileUndoTool();

        if (pending.Type == PendingApprovalTypeV1.TerminalRun)
            return CancelPendingTerminalRunTool();

        if (pending.Type == PendingApprovalTypeV1.DesktopAction)
            return CancelPendingDesktopActionTool();

        PendingApprovalStoreV1.Clear();
        return "Pending åtgärd avbröts.";
    }

    private static bool IsGenericCancelCommand(string input)
    {
        var command = NormalizeCommand(input);
        return command is "avbryt" or "avbryt allt" or "cancel" or "stoppa";
    }

    private static string BuildPendingApprovalPreviewV1(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "(ingen preview)";

        return content.Length > 2000
            ? content[..2000] + "\n\n...preview kortad."
            : content;
    }

    private async Task ShowFileChangeReviewV1Async(string path)
    {
        var reviewPath = string.IsNullOrWhiteSpace(path)
            ? LatestFileChangeReviewV1?.Path ?? ""
            : path.Trim().Replace("\\", "/");

        if (string.IsNullOrWhiteSpace(reviewPath))
        {
            await AddAssistantMessage("Det finns ingen filändring att granska ännu.");
            return;
        }

        if (TryGetSafeProjectFilePath(reviewPath, mustBeExisting: true, out _, out _))
            await SendProjectFileToEditor(reviewPath);

        var reviewFolder = Path.GetDirectoryName(reviewPath)?.Replace("\\", "/") ?? "";
        if (!string.IsNullOrWhiteSpace(reviewFolder))
            await SendTreeFolderV7Async(reviewFolder, refreshAutocomplete: false);

        await SendLatestFileChangeReviewV1Async();
    }

    private async Task SendLatestFileChangeReviewV1Async()
    {
        if (_webView.CoreWebView2 is null || LatestFileChangeReviewV1 is null)
            return;

        var payload = new
        {
            path = LatestFileChangeReviewV1.Path,
            changeKind = LatestFileChangeReviewV1.ChangeKind,
            added = LatestFileChangeReviewV1.Added,
            removed = LatestFileChangeReviewV1.Removed,
            lines = LatestFileChangeReviewV1.Lines.Select(line => new
            {
                kind = line.Kind,
                text = line.Text
            }).ToList()
        };

        var payloadJson = JsonSerializer.Serialize(payload);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisShowFileChangeReviewV1(" + payloadJson + ");"
        );
    }

    private async Task SendLatestTerminalPanelV1Async()
    {
        if (_webView.CoreWebView2 is null || LatestTerminalPanelV1 is null)
            return;

        if (SentTerminalPanelVersionV1 == LatestTerminalPanelVersionV1)
            return;

        SentTerminalPanelVersionV1 = LatestTerminalPanelVersionV1;

        var payload = new
        {
            version = LatestTerminalPanelVersionV1,
            command = LatestTerminalPanelV1.Command,
            workingDirectory = LatestTerminalPanelV1.WorkingDirectory,
            exitCode = LatestTerminalPanelV1.ExitCode,
            timedOut = LatestTerminalPanelV1.TimedOut,
            summary = LatestTerminalPanelV1.Summary,
            output = LatestTerminalPanelV1.Output,
            error = LatestTerminalPanelV1.Error
        };

        var payloadJson = JsonSerializer.Serialize(payload);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisShowTerminalPanelV1(" + payloadJson + ");"
        );
    }

    private static FileChangeReviewV1 BuildFileChangeReviewV1(string path, string before, string after, string changeKind = "change")
    {
        var beforeLines = SplitFileChangeReviewLinesV1(before);
        var afterLines = SplitFileChangeReviewLinesV1(after);
        var diffLines = BuildLineDiffV1(beforeLines, afterLines);
        var added = diffLines.Count(line => line.Kind == "added");
        var removed = diffLines.Count(line => line.Kind == "removed");

        return new FileChangeReviewV1(path.Replace("\\", "/"), NormalizeFileChangeKindV1(changeKind), added, removed, diffLines);
    }

    private static string NormalizeFileChangeKindV1(string value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();

        return normalized switch
        {
            "write" => "write",
            "append" => "append",
            "delete" => "delete",
            "undo" => "undo",
            _ => "change"
        };
    }

    private static List<string> SplitFileChangeReviewLinesV1(string content)
    {
        var normalized = (content ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n').ToList();

        if (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        return lines;
    }

    private static List<FileChangeReviewLineV1> BuildLineDiffV1(List<string> beforeLines, List<string> afterLines)
    {
        var oldCount = beforeLines.Count;
        var newCount = afterLines.Count;
        var table = new int[oldCount + 1, newCount + 1];

        for (var i = oldCount - 1; i >= 0; i--)
        {
            for (var j = newCount - 1; j >= 0; j--)
            {
                if (beforeLines[i] == afterLines[j])
                    table[i, j] = table[i + 1, j + 1] + 1;
                else
                    table[i, j] = Math.Max(table[i + 1, j], table[i, j + 1]);
            }
        }

        var result = new List<FileChangeReviewLineV1>();
        var oldIndex = 0;
        var newIndex = 0;

        while (oldIndex < oldCount && newIndex < newCount)
        {
            if (beforeLines[oldIndex] == afterLines[newIndex])
            {
                result.Add(new FileChangeReviewLineV1("context", beforeLines[oldIndex]));
                oldIndex++;
                newIndex++;
                continue;
            }

            if (table[oldIndex + 1, newIndex] >= table[oldIndex, newIndex + 1])
            {
                result.Add(new FileChangeReviewLineV1("removed", beforeLines[oldIndex]));
                oldIndex++;
            }
            else
            {
                result.Add(new FileChangeReviewLineV1("added", afterLines[newIndex]));
                newIndex++;
            }
        }

        while (oldIndex < oldCount)
        {
            result.Add(new FileChangeReviewLineV1("removed", beforeLines[oldIndex]));
            oldIndex++;
        }

        while (newIndex < newCount)
        {
            result.Add(new FileChangeReviewLineV1("added", afterLines[newIndex]));
            newIndex++;
        }

        return result;
    }

    private static async Task<string> HandleMessageAsync(string input)
    {
        var text = input.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return "Skriv något först.";

        if (TryCalculate(text, out var result))
            return result;

        if (IsGenericCancelCommand(text))
            return CancelCurrentPendingApprovalTool();

        // ALIAS_ROUTER_INPUT_NORMALIZATION: translate natural phrases to local commands before routing.
        text = ExpandInputAlias(text);
        var command = NormalizeCommand(text);

        // COMMAND_ROUTER_V1_SAFE_ENTRY:
        // First safe runtime integration. Only help/status are handled here for now.
        var routedV1 = CommandRouterV1.Parse(text);
        var routedV1Errors = CommandValidatorV1.Validate(routedV1);

        if (!routedV1.ShouldSendToOllama && routedV1Errors.Count > 0)
            return string.Join(Environment.NewLine, routedV1Errors.Distinct());

        if (routedV1.Intent == CommandIntent.Help)
            return BuildHelp();

        if (routedV1.Intent == CommandIntent.Status)
            return BuildStatus();

        if (routedV1.Intent == CommandIntent.OverviewShow)
            return BuildOverviewTextTool();

        if (routedV1.Intent == CommandIntent.MemoryShow)
            return ShowMemory();

        if (routedV1.Intent == CommandIntent.MemoryImportantShow)
            return ShowMemoriesByType("important", "Viktiga minnen");

        if (routedV1.Intent == CommandIntent.MemoryProjectShow)
            return ShowMemoriesByType("project_fact", "Projektminnen");

        if (routedV1.Intent == CommandIntent.MemoryStatus)
            return MemoryStatus();

        if (routedV1.Intent == CommandIntent.ObsidianStatus)
            return ObsidianStatusTool();

        if (routedV1.Intent == CommandIntent.MemorySearch)
            return SearchMemory("sök minne: " + routedV1.Arguments.GetValueOrDefault("query", ""));

        if (routedV1.Intent == CommandIntent.MemoryArchiveSearch)
            return SearchArchive("sök arkiv: " + routedV1.Arguments.GetValueOrDefault("query", ""));

        if (routedV1.Intent == CommandIntent.FileRead)
            return ReadProjectFileTool("läs fil: " + routedV1.Arguments.GetValueOrDefault("path", ""));

        if (routedV1.Intent == CommandIntent.FileOpen)
            return "Filöppning hanteras lokalt i Jarvis filpanel och skickas inte till Ollama.";

        if (routedV1.Intent == CommandIntent.TerminalPreview)
            return TerminalPreviewTool("terminal preview: " + routedV1.Arguments.GetValueOrDefault("command", ""));

        if (routedV1.Intent == CommandIntent.TerminalConfirm)
            return TerminalConfirmRun();

        if (routedV1.Intent == CommandIntent.TerminalCancel)
            return TerminalCancelRun();

        if (routedV1.Intent == CommandIntent.TerminalShow)
            return ShowLatestTerminalTranscriptTool();

        if (routedV1.Intent == CommandIntent.CheckpointCreate)
            return CreateNamedCheckpointTool(routedV1.Arguments.GetValueOrDefault("name", ""));

        if (routedV1.Intent == CommandIntent.CheckpointList)
            return ListCheckpointsTool();

        if (routedV1.Intent == CommandIntent.CheckpointRestore)
            return RestoreCheckpointByNameTool(routedV1.Arguments.GetValueOrDefault("name", ""));

        if (routedV1.Intent == CommandIntent.BrainWindowOpen)
            return OpenBrainWindow();

        if (routedV1.Intent == CommandIntent.AgentRun)
            return await RunOllamaAgentToolAsync(routedV1.Arguments.GetValueOrDefault("task", ""));

        if (routedV1.Intent == CommandIntent.VaultStatus)
            return VaultStatusTool();

        if (routedV1.Intent == CommandIntent.VaultToggle)
            return VaultToggleTool(routedV1.Arguments.GetValueOrDefault("on", "true") == "true");

        if (routedV1.Intent == CommandIntent.VaultSearch)
            return VaultSearchTool(routedV1.Arguments.GetValueOrDefault("query", ""));

        if (routedV1.Intent == CommandIntent.VaultCreate)
            return VaultCreateTool(routedV1.Arguments.GetValueOrDefault("name", ""), routedV1.Arguments.GetValueOrDefault("text", ""));

        if (routedV1.Intent == CommandIntent.ConversationShow)
            return ConversationHistory.FormatForChat();
        if (routedV1.Intent == CommandIntent.ConversationClear)
        {
            ConversationHistory.Clear();
            return "Samtals-historik rensad. Jarvis har glömt tidigare turns (memory.md och vault är oförändrade).";
        }

        if (routedV1.Intent == CommandIntent.ProgramLaunch)
        {
            var app = routedV1.Arguments.GetValueOrDefault("app", "");
            var res = SafeAppLauncher.TryLaunch(app);
            return res.Message;
        }
        if (routedV1.Intent == CommandIntent.ProgramListAllowed)
        {
            var allowed = SafeAppLauncher.ListAllowed();
            return "Tillåtna program (whitelist):\n- " + string.Join("\n- ", allowed) +
                   "\n\nAnvänd: '/öppna program <namn>' eller 'öppna program notepad'";
        }

        if (routedV1.Intent == CommandIntent.WebSearch)
        {
            var query = routedV1.Arguments.GetValueOrDefault("query", "");
            return WebSearcher.OpenSearchInOpera(query);
        }
        if (routedV1.Intent == CommandIntent.WebFetch)
        {
            var url = routedV1.Arguments.GetValueOrDefault("url", "");
            if (string.IsNullOrWhiteSpace(url)) return "Skriv: /läs <url>. Exempel: /läs https://example.com";
            return await WebSearcher.FetchTextAsync(url, 4000);
        }

        if (routedV1.Intent == CommandIntent.ModelCatalogList)
            return ListModelCatalogTool();

        if (routedV1.Intent == CommandIntent.ModelCatalogSwitch)
            return SwitchModelCatalogTool(routedV1.Arguments.GetValueOrDefault("target", ""));

        if (routedV1.Intent == CommandIntent.BuilderStart)
            return await BuilderStartToolAsync(routedV1.Arguments.GetValueOrDefault("description", ""));

        if (routedV1.Intent == CommandIntent.BuilderAnswer)
            return BuilderAnswerTool(routedV1.Arguments.GetValueOrDefault("answer", ""));

        if (routedV1.Intent == CommandIntent.BuilderPlan)
            return await BuilderPlanToolAsync();

        if (routedV1.Intent == CommandIntent.BuilderStatus)
            return BuilderMode.Status();

        if (routedV1.Intent == CommandIntent.BuilderCancel)
            return BuilderCancelTool();

        if (routedV1.Intent == CommandIntent.NaturalCodeEdit)
            return await NaturalEditRequestToolAsync(
                routedV1.Arguments.GetValueOrDefault("path", ""),
                routedV1.Arguments.GetValueOrDefault("instruction", "")
            );

        if (NaturalEditTool.TryParseNaturalLanguage(text, out var naturalEditRequest))
            return await NaturalEditRequestToolAsync(naturalEditRequest.RelativePath, naturalEditRequest.Instruction);

        if (CommandIs(command, "visa terminal", "visa terminalen", "oppna terminal", "öppna terminal", "terminal", "terminal output", "senaste terminal", "vad stod i terminalen", "terminalpanelen", "visa terminalpanelen"))
            return ShowLatestTerminalTranscriptTool();

        // INTERNET_STATUS_EARLY_CHECK: internet questions must be answered by C#, not guessed by Ollama.
        if (command == "internetstatus" ||
            command == "internet status" ||
            command == "har jag internet" ||
            command == "ar jag online" ||
            command == "är jag online" ||
            command == "bevisa internet" ||
            command == "bevisa att jag har internet" ||
            command == "bevisa de" ||
            command == "bevisa det")
            return await InternetStatusToolAsync();

        // HARD_ACTIVE_FILE_EARLY_CHECK: active-file commands must run before fuzzy file commands.
        if (command == "aktiv fil" || command == "visa aktiv fil" || command == "vilken fil")
            return ShowActiveFileTool();

        if (command == "las filen" || command == "läs filen" || command == "visa filen" || command == "oppna filen" || command == "öppna filen")
            return ReadActiveFileTool();

        if (command.StartsWith("foresla rubrik har") || command.StartsWith("föreslå rubrik här") || command.StartsWith("foresla rubrik här"))
            return ProposeHeadingForActiveFileTool(text);

        if (CommandIs(command, "hjalp", "help"))
            return BuildHelp();

        if (CommandIs(command, "test csharp"))
            return "C#-bryggan fungerar.";

        if (CommandIs(command, "status"))
            return BuildStatus();

        if (CommandIs(command, "diskstatus", "disk status"))
            return DiskGuardStatusTool();

        if (CommandIs(command, "cachestatus", "cache status"))
            return CacheStatusTool();

        if (CommandIs(command, "rensa cache preview", "cache preview"))
            return CleanCachePreview();

        if (CommandIs(command, "rensa cache bekrafta", "rensa cache bekräfta", "cache bekrafta"))
            return CleanCacheConfirm();

        if (CommandIs(command, "kontrollera cache", "cache kontroll", "kontrollera disklagring"))
            return CacheConfigCheck();

        if (CommandStartsWith(command, "kom ihag"))
            return SaveSmartMemory(text, "note", 3, "general", 2);

        if (CommandStartsWith(command, "smart minne"))
            return SaveSmartMemory(text, "note", 3, "smart-memory", 2);

        if (CommandStartsWith(command, "viktigt minne", "viktig minne"))
            return SaveSmartMemory(text, "important", 5, "important", 2);

        if (CommandStartsWith(command, "projektminne", "projekt minne"))
            return SaveSmartMemory(text, "project_fact", 4, "project", command.StartsWith("projekt minne") ? 2 : 1);

        if (CommandStartsWith(command, "sok minne", "sok i minne"))
            return SearchMemory(text);

        if (CommandIs(command, "visa minne", "minne"))
            return ShowMemory();

        if (CommandIs(command, "minnesstatus", "minne status"))
            return MemoryStatus();

        if (CommandIs(command, "sammanfatta minne", "sammanfatt minne", "minnessammanfattning"))
            return SummarizeMemory();

        if (CommandIs(command, "visa viktiga minnen", "viktiga minnen", "visa important"))
            return ShowMemoriesByType("important", "Viktiga minnen");

        if (CommandIs(command, "visa projektminnen", "projektminnen", "visa projekt minnen"))
            return ShowMemoriesByType("project_fact", "Projektminnen");

        if (CommandStartsWith(command, "glom minne", "glom i minne"))
            return PrepareForgetMemory(text);

        if (Regex.IsMatch(command, @"^bekrafta glom\s+\d+$"))
            return ConfirmForgetMemory(command);

        if (CommandIs(command, "oppna minne", "oppna minnesfil"))
            return OpenMemoryTool();

        if (CommandStartsWith(command, "terminal preview"))
            return TerminalPreviewTool(text);

        if (CommandIs(command, "bekrafta kor", "bekräfta kör", "confirm run"))
            return TerminalConfirmRun();

        if (CommandIs(command, "avbryt kor", "avbryt kör", "cancel run"))
            return TerminalCancelRun();

        if (CommandIs(command, "visa terminal", "visa terminalen", "oppna terminal", "öppna terminal", "terminal", "terminal output", "senaste terminal", "vad stod i terminalen", "terminalpanelen", "visa terminalpanelen"))
            return ShowLatestTerminalTranscriptTool();

        if (CommandIs(command, "angra", "ångra", "angra senaste", "ångra senaste", "undo", "undo senaste", "angra filandring", "ångra filändring"))
            return UndoLastFileChangeRequestTool();

        if (CommandStartsWith(command, "lista filer"))
            return ListFilesTool(command);

        if (CommandIs(command, "oppna projektmapp", "oppna mapp", "open project folder"))
            return OpenFolderTool(ProjectRoot);

        if (CommandIs(command, "oppna dashboard", "oppna dashboardmapp"))
            return OpenFolderTool(Path.Combine(ProjectRoot, "dashboard"));

        if (CommandIs(command, "oppna app", "oppna appmapp"))
            return OpenFolderTool(Path.Combine(ProjectRoot, "app"));

        if (CommandIs(command, "test ollama"))
            return await AskOllamaAsync("Svara bara med: Ollama fungerar");

        // MEMORY_GUARD_BEFORE_OLLAMA: memory questions should use active memory.md before Ollama.
        if (CommandIs(command, "visa arkiverade minnen", "visa arkiv", "arkiv"))
            return ShowArchive();

        if (CommandStartsWith(command, "sok arkiv", "sok i arkiv"))
            return SearchArchive(text);

        if (TryAnswerFromMemoryQuestion(text, command, out var memoryAnswer))
            return memoryAnswer;

        // HARD_LOCAL_FILE_TOOLS_CHECK: file commands must never go to Ollama.
        if (CommandIs(command, "indexera projekt", "projekt index", "indexera jarvis"))
            return ProjectIndexTool();

        if (CommandStartsWith(command, "las fil", "läs fil"))
            return ReadProjectFileTool(text);

        if (CommandStartsWith(command, "skapa fil", "create file"))
            return CreateProjectFileRequestTool(text);

        if (CommandStartsWith(command, "skriv fil"))
            return WriteProjectFileTool(text, append: false);

        if (CommandStartsWith(command, "lagg till fil", "lägg till fil", "append fil"))
            return WriteProjectFileTool(text, append: true);

        if (TryExtractDeleteProjectFileRequest(text, command, out var deletePath, out var deleteError))
            return DeleteProjectFileRequestTool(deletePath, deleteError);

        if (CommandIs(command, "godkann filskrivning", "godkänn filskrivning", "godkann fil", "godkänn fil"))
            return ApprovePendingFileWriteTool();

        if (CommandIs(command, "avbryt filskrivning", "avbryt fil", "cancel file write"))
            return CancelPendingFileWriteTool();

        if (CommandIs(command, "godkann filradering", "godkänn filradering", "godkann radering", "godkänn radering"))
            return ApprovePendingFileDeleteTool();

        if (CommandIs(command, "avbryt filradering", "avbryt radering", "cancel file delete"))
            return CancelPendingFileDeleteTool();

        // HARD_LOCAL_PROJECT_INDEX_CHECK: project index command must never go to Ollama.
        if (CommandIs(command, "indexera projekt", "projekt index", "indexera jarvis"))
            return ProjectIndexTool();

        // LOCAL_COMMAND_HELP_CHECK: usability/help commands must stay local.
        if (CommandIs(command, "kommandohjalp", "kommando hjalp", "hur anvander jag jarvis", "hur", "hur gor jag"))
            return CommandHelpTool();

        if (CommandIs(command, "lista md filer", "lista markdown filer", "visa md filer"))
            return ListProjectFilesByExtension(".md", "Markdown-filer");

        if (CommandIs(command, "lista projektfiler", "visa projektfiler"))
            return ListProjectFilesCompact();

        // HARD_LOCAL_PROPOSE_CHANGE_CHECK: propose-change commands must never go directly to normal chat.
        if (CommandStartsWith(command, "foresla rubrik", "föreslå rubrik"))
            return ProposeHeadingTool(text);

        if (CommandStartsWith(command, "foresla andring", "föreslå ändring", "foreslå ändring"))
            return await ProposeChangeToolAsync(text);

        if (CommandIs(command, "avbryt andring", "avbryt ändring", "radera forslag", "radera förslag"))
            return CancelPendingChangeTool();

        if (CommandIs(command, "godkann andring", "godkänn ändring", "godkann forslag", "godkänn förslag"))
            return ApprovePendingChangeTool();

        // NATURAL_PROPOSAL_ROUTER_CHECK: normal language proposal requests should become safe pending changes.
        var naturalProposal = await TryHandleNaturalProposalAsync(text, command);
        if (naturalProposal is not null)
            return naturalProposal;

        // BRAIN_OPEN_CHECK: brain-vyn kan öppnas med naturligt språk.
        if (CommandIs(command, "brain", "öppna brain", "oppna brain", "visa brain", "hjärna", "hjarna", "öppna hjärnan", "oppna hjarnan"))
            return OpenBrainWindow();

        // LOCAL_CHECKPOINT_TOOLS_CHECK: checkpoint commands must stay local.
        if (CommandIs(command, "skapa checkpoint", "ny checkpoint", "create checkpoint"))
            return CreateCheckpointTool();

        // Natural-language named create: "skapa checkpoint innan-port" / "ny checkpoint stable-baseline"
        if (CommandStartsWith(command, "skapa checkpoint", "ny checkpoint", "create checkpoint"))
        {
            var name = ExtractCheckpointNameFromCommandV1(text, command, isRestore: false);
            return CreateNamedCheckpointTool(name);
        }

        if (CommandIs(command, "lista checkpoints", "visa checkpoints", "checkpoints"))
            return ListCheckpointsTool();

        if (CommandIs(command, "aterstall senaste checkpoint", "återställ senaste checkpoint", "rollback senaste checkpoint"))
            return RestoreLatestCheckpointTool();

        // Natural-language named restore: "återställ checkpoint innan-port"
        if (CommandStartsWith(command, "aterstall checkpoint", "återställ checkpoint", "rollback checkpoint", "restore checkpoint"))
        {
            var name = ExtractCheckpointNameFromCommandV1(text, command, isRestore: true);
            return RestoreCheckpointByNameTool(name);
        }

        // MODEL_CONFIG_TOOLS_CHECK: model commands must stay local.
        if (CommandIs(command, "visa modell", "modell", "aktuell modell"))
            return ShowModelTool();

        if (CommandIs(command, "lista modeller", "ollama modeller", "modellista"))
            return await ListOllamaModelsTool();

        if (CommandStartsWith(command, "byt modell", "andra modell", "ändra modell"))
            return SetModelTool(text);

        // ACTIVE_FILE_CONTEXT_CHECK: active file commands must stay local when possible.
        if (CommandIs(command, "aktiv fil", "visa aktiv fil", "vilken fil"))
            return ShowActiveFileTool();

        if (CommandIs(command, "las filen", "läs filen", "visa filen", "oppen filen", "öppna filen"))
            return ReadActiveFileTool();

        if (CommandStartsWith(command, "foresla rubrik har", "föreslå rubrik här", "foresla rubrik här"))
            return ProposeHeadingForActiveFileTool(text);

        return await AskOllamaAsync(text);
    }

    private static string BuildHelp()
    {
        return string.Join(Environment.NewLine, new[]
        {
            "Jarvis lokala kommandon:",
            "",
            "Slash-kommandon som är inkopplade nu:",
            "- /hjälp",
            "- /status",
            "- /översikt",
            "- /obsidian status",
            "- /minne status",
            "- /minne visa",
            "- /minne viktiga",
            "- /minne projekt",
            "- /minne sök röd",
            "- /minne arkiv sök röd",
            "- /fil öppna README.md",
            "- /fil läs docs/PROJECT_INDEX.md",
            "- /fil skapa docs/test.md = text",
            "- /edit docs/test.md = beskriv ändringen",
            "- /terminal preview dotnet build",
            "- /terminal visa",
            "- /terminal godkänn",
            "- /terminal avbryt",
            "- /desktop status | /desktop på | /desktop av",
            "- /desktop tars start | /desktop tars stop",
            "- /skärm",
            "- /desktop klick 100 200 | /desktop skriv text | /desktop hotkey ctrl+l | /desktop fråga klicka på knappen",
            "- /checkpoint skapa innan-port",
            "- /checkpoint lista",
            "- /checkpoint återställ innan-port",
            "- /brain   (öppna 3D-vyn med projekt-graf + vault)",
            "- /agent <uppgift>   (read-only agent: läs/sök/lista filer)",
            "- /modell   (lista profiler) eller /modell byt <namn>",
            "- /bygg <idé> | /bygg svar <svar> | /bygg plan | /bygg status | /bygg avbryt",
            "- /vault status | /vault sök <q> | /vault skapa <namn> = <text> | /vault på | /vault av",
            "- /sök <query>   (web-sök via DuckDuckGo, offline-graceful)",
            "- /läs <url>     (hämta+sammanfatta sida)",
            "- /öppna program <namn>   (whitelist: notepad/vscode/chrome/spotify/...)",
            "- /historik | /glöm samtal   (multi-turn context)",
            "",
            "Naturligt språk som också hålls lokalt:",
            "- översikt",
            "- obsidian status",
            "- visa mina viktiga minnen",
            "- visa projektminnen",
            "- leta efter röd i arkivet",
            "- öppna README.md",
            "- läs fil: docs/PROJECT_INDEX.md",
            "- bygg projektet men fråga först",
            "- desktop status",
            "- skärm",
            "",
            "Status och system:",
            "- status",
            "- diskstatus",
            "- cachestatus",
            "- rensa cache preview",
            "- rensa cache bekräfta",
            "- kontrollera cache",
            "- internetstatus",
            "",
            "Modell:",
            "- visa modell",
            "- lista modeller",
            "- byt modell: qwen2.5-coder:7b",
            "",
            "Minne:",
            "- kom ihåg: text",
            "- smart minne: text",
            "- viktigt minne: text",
            "- projektminne: text",
            "- visa minne",
            "- sök minne: text",
            "- minnesstatus",
            "- obsidian status",
            "- sammanfatta minne",
            "- visa viktiga minnen",
            "- visa projektminnen",
            "- öppna minne",
            "- visa arkiverade minnen",
            "- sök arkiv: text",
            "- glöm minne: text",
            "- bekräfta glöm 1",
            "",
            "Filer och Project Explorer:",
            "- lista filer",
            "- lista filer i docs",
            "- lista md filer",
            "- lista projektfiler",
            "- indexera projekt",
            "- aktiv fil",
            "- läs filen",
            "- läs fil: docs/PROJECT_INDEX.md",
            "- öppna README.md",
            "- öppna app/Program.cs",
            "- öppna mapp: docs",
            "- öppna projekt",
            "",
            "Säkra ändringsförslag:",
            "- /edit docs/test.md = gör texten tydligare",
            "- gå in i docs/test.md och gör texten tydligare",
            "- /bygg en enkel TODO-app i HTML",
            "- /bygg svar målgrupp: bara jag, en sida, mörkt tema",
            "- /bygg plan",
            "- /desktop på",
            "- /desktop fråga klicka på sökrutan",
            "- /desktop klick 100 200",
            "- /desktop av",
            "- föreslå rubrik: docs/test-agent.md = Test Agent",
            "- föreslå rubrik här: Min rubrik",
            "- föreslå ändring: docs/test-agent.md = instruktion",
            "- godkänn ändring",
            "- avbryt ändring",
            "",
            "Säker filskrivning med pending (separator: = , eller | som fallback):",
            "- skapa fil: docs/test.md = text",
            "- skriv fil: docs/test.md = text",
            "- lägg till fil: docs/test.md = text",
            "- editera i filpanelen och klicka Spara med godkännande",
            "- godkänn filskrivning",
            "- avbryt filskrivning",
            "",
            "Checkpoint:",
            "- skapa checkpoint",
            "- lista checkpoints",
            "",
            "Terminal:",
            "- terminal preview: dotnet build",
            "- bekräfta kör",
            "- avbryt kör",
            "- /terminal preview dotnet build",
            "- /terminal godkänn",
            "- /terminal avbryt",
            "",
            "Mappar och program:",
            "- öppna projektmapp",
            "- öppna dashboard",
            "- öppna app",
            "- öppna program: notepad",
            "- starta kalkylator",
            "- öppna program: paint",
            "- öppna program: explorer",
            "",
            "Viktigt:",
            "- Tomma minneskommandon ska blockeras.",
            "- Slash-kommandon går aldrig till Ollama.",
            "- Filskrivning ska gå via pending/godkännande innan direkt skrivning används.",
            "- Filnamn som README.md och Program.cs öppnas i filpanelen.",
            "- Program startas med tydligt programkommando.",
            "- Jarvis får bara jobba i F:\\Jarvis-clean.",
            "- F:\\New project är read-only reference och ska inte ändras."
        });
    }

    private static string DiskGuardStatusTool()
    {
        var lines = new List<string>();

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            if (!drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase) &&
                !drive.Name.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                continue;

            var freeGb = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            var totalGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            var usedGb = totalGb - freeGb;

            var warning = "";

            if (drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase) && freeGb < 5)
                warning = "  VARNING: C-disken börjar bli låg.";

            lines.Add(
                drive.Name + " " +
                Math.Round(usedGb, 2) + " GB använt / " +
                Math.Round(freeGb, 2) + " GB ledigt / " +
                Math.Round(totalGb, 2) + " GB totalt" +
                warning
            );
        }

        if (lines.Count == 0)
            return "Jag kunde inte läsa diskstatus.";

        return "Diskstatus:\n" + string.Join("\n", lines);
    }

    private static string CacheStatusTool()
    {
        var targets = GetSafeCacheTargets();

        var lines = new List<string>();

        foreach (var target in targets)
        {
            var exists = Directory.Exists(target.Path);

            if (!exists)
            {
                lines.Add(target.Name + ":\n  Finns inte: " + target.Path);
                continue;
            }

            var size = GetDirectorySizeSafe(target.Path);
            var sizeText = size < 0 ? "okänd storlek" : Math.Round(size / 1024.0 / 1024.0 / 1024.0, 2) + " GB";

            lines.Add(target.Name + ":\n  " + sizeText + "\n  " + target.Path);
        }

        return "Cachestatus, säkra cachemappar:\n\n" + string.Join("\n\n", lines);
    }

    private static string CleanCachePreview()
    {
        var targets = GetSafeCacheTargets();

        var lines = targets
            .Select(target => "- " + target.Name + "\n  " + target.Path)
            .ToList();

        return
            "Preview: Jarvis kan rensa dessa säkra cache/temp-mappar.\n\n" +
            string.Join("\n\n", lines) +
            "\n\nDetta ska inte röra dina dokument, bilder, downloads, projektfiler, F:\\New project eller F:\\Jarvis-clean.\n\n" +
            "För att faktiskt rensa, skriv:\n" +
            "rensa cache bekräfta";
    }

    private static string CleanCacheConfirm()
    {
        var targets = GetSafeCacheTargets();

        var results = new List<string>();

        foreach (var target in targets)
        {
            var result = ClearDirectoryContentsSafe(target.Path);
            results.Add(target.Name + ": " + result);
        }

        return
            "Cache-rensning klar.\n\n" +
            string.Join("\n", results) +
            "\n\nJag rensade bara säkra cache/temp-mappar.";
    }

    private static string CacheConfigCheck()
    {
        var variables = new[]
        {
            "TEMP",
            "TMP",
            "NUGET_PACKAGES",
            "NUGET_HTTP_CACHE_PATH",
            "DOTNET_CLI_HOME",
            "PIP_CACHE_DIR",
            "npm_config_cache",
            "HF_HOME",
            "TRANSFORMERS_CACHE",
            "OLLAMA_MODELS"
        };

        var lines = new List<string>();

        foreach (var variable in variables)
        {
            var value = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Process)
                ?? Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User)
                ?? "";

            if (string.IsNullOrWhiteSpace(value))
            {
                lines.Add(variable + ": saknas");
                continue;
            }

            var ok = value.StartsWith(@"F:\", StringComparison.OrdinalIgnoreCase)
                ? "OK på F-disken"
                : "VARNING: inte på F-disken";

            lines.Add(variable + ": " + value + "  [" + ok + "]");
        }

        return "Cache-konfiguration:\n" + string.Join("\n", lines);
    }

    private static (string Name, string Path)[] GetSafeCacheTargets()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return new[]
        {
            ("Windows user temp", Path.Combine(localAppData, "Temp")),
            ("NuGet HTTP cache", Path.Combine(localAppData, "NuGet", "v3-cache")),
            ("Old NuGet packages on C", Path.Combine(userProfile, ".nuget", "packages")),
            ("pip cache", Path.Combine(localAppData, "pip", "Cache")),
            ("npm cache", Path.Combine(appData, "npm-cache")),
            ("HuggingFace cache", Path.Combine(userProfile, ".cache", "huggingface")),
            ("Jarvis F temp", @"F:\DevCache\Temp")
        };
    }

    private static long GetDirectorySizeSafe(string path)
    {
        try
        {
            if (!Directory.Exists(path))
                return 0;

            long total = 0;
            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();

                try
                {
                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        try
                        {
                            total += new FileInfo(file).Length;
                        }
                        catch
                        {
                            // Ignore unreadable files.
                        }
                    }
                }
                catch
                {
                    // Ignore unreadable directories.
                }

                try
                {
                    foreach (var childDir in Directory.EnumerateDirectories(dir))
                        stack.Push(childDir);
                }
                catch
                {
                    // Ignore unreadable directories.
                }
            }

            return total;
        }
        catch
        {
            return -1;
        }
    }

    private static string ClearDirectoryContentsSafe(string path)
    {
        if (!Directory.Exists(path))
            return "finns inte";

        var deletedFiles = 0;
        var deletedDirs = 0;
        var failed = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    File.Delete(file);
                    deletedFiles++;
                }
                catch
                {
                    failed++;
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    deletedDirs++;
                }
                catch
                {
                    failed++;
                }
            }
        }
        catch
        {
            failed++;
        }

        return "tog bort " + deletedFiles + " filer och " + deletedDirs + " mappar. Misslyckades: " + failed;
    }
    private static async Task<string> InternetStatusToolAsync()
    {
        var ollamaOk = await ProbeTcpAsync("127.0.0.1", 11434, 500);
        var internetOk = await IsInternetOnlineCachedAsync(forceRefresh: true);

        var lines = new List<string>
        {
            "Internetstatus:",
            "- Ollama lokalt: " + (ollamaOk ? "OK, localhost:11434 svarar" : "Svarar inte"),
            "- Internet-test: " + (internetOk ? "OK, kunde nå 1.1.1.1:443" : "SAKNAS eller blockerat, kunde inte nå 1.1.1.1:443 inom timeout"),
            "",
            "Tolkning:"
        };

        if (internetOk)
        {
            lines.Add("Du verkar ha internetanslutning just nu.");
        }
        else
        {
            lines.Add("Internet verkar saknas just nu. Lokala Jarvis-funktioner fungerar ändå.");
        }

        lines.Add("");
        lines.Add("Viktigt: Detta svar kommer från C# InternetProbe, inte från Ollama-gissning.");

        return string.Join("\n", lines);
    }

    // InternetProbe-cache. Web-verktyg (väder/nyheter/sökning i framtida faser) ska kalla
    // IsInternetOnlineCachedAsync() innan de gör nätverksanrop. Cache 30 s minskar TCP-trafik
    // när flera tools efterfrågar status nära varandra. Tråd-säker via lock.
    private const int InternetProbeCacheTtlSeconds = 30;
    private static readonly object _internetProbeLock = new();
    private static DateTime _lastInternetProbeAt = DateTime.MinValue;
    private static bool _lastInternetProbeResult = false;
    private static Task<bool>? _inflightInternetProbe;

    public static async Task<bool> IsInternetOnlineCachedAsync(bool forceRefresh = false)
    {
        Task<bool>? probeTask = null;
        var now = DateTime.Now;

        lock (_internetProbeLock)
        {
            var ageOk = (now - _lastInternetProbeAt).TotalSeconds < InternetProbeCacheTtlSeconds;
            if (!forceRefresh && ageOk)
                return _lastInternetProbeResult;

            // Coalesce concurrent callers into one probe.
            if (_inflightInternetProbe is not null)
                probeTask = _inflightInternetProbe;
        }

        if (probeTask is not null)
            return await probeTask.ConfigureAwait(false);

        var fresh = ProbeTcpAsync("1.1.1.1", 443, 800);
        lock (_internetProbeLock) { _inflightInternetProbe = fresh; }

        bool result = false;
        try { result = await fresh.ConfigureAwait(false); }
        finally
        {
            lock (_internetProbeLock)
            {
                _lastInternetProbeAt = DateTime.Now;
                _lastInternetProbeResult = result;
                _inflightInternetProbe = null;
            }
        }
        return result;
    }

    // Kort hjälper för UI/agent-tools: returnerar svenskt felmeddelande om offline,
    // annars tom sträng. Pattern: var off = OfflineSkipMessageOrEmpty(); if (off != "") return off;
    public static async Task<string> OfflineSkipMessageOrEmptyAsync()
    {
        var online = await IsInternetOnlineCachedAsync().ConfigureAwait(false);
        return online ? "" : "Internet saknas just nu, hoppar över.";
    }

    private static async Task<bool> ProbeTcpAsync(string host, int port, int timeoutMs)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();

            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeoutMs);

            var finished = await Task.WhenAny(connectTask, timeoutTask);

            if (finished != connectTask)
                return false;

            await connectTask;
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
    private static string BuildStatus()
    {
        return
            "Jarvis status:\n" +
            "- Safe dashboard: aktiv\n" +
            "- C#-brygga: aktiv\n" +
            "- Smart Memory: aktivt, memory.md\n" +
            "- Översiktspanel: aktiv\n" +
            "- Obsidian: " + (string.IsNullOrWhiteSpace(ReadConfiguredObsidianVaultPathV1()) ? "inte konfigurerat" : "konfigurerat read-only") + "\n" +
            "- Felstavnings-tolerans: aktiv\n" +
            "- Ollama-modell: " + GetActiveOllamaModel() + "\n" +
            "- NeuroLinked: avstängd\n" +
            "- 3D/WebGL: avstängt\n" +
            "- Projektmapp: " + ProjectRoot;
    }

    private static string BuildOverviewTextTool()
    {
        return
            "Öppnade Jarvis Översikt.\n\n" +
            BuildMemoryOverviewLineV1() + "\n\n" +
            BuildObsidianOverviewLineV1() + "\n\n" +
            "Jarvis-loop:\n" +
            "Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember\n\n" +
            "Viktigt: detta är en säker statuspanel. Den kör ingen bakgrundsagent och skriver inte till Obsidian.";
    }

    private static string ObsidianStatusTool()
    {
        var configuredPath = ReadConfiguredObsidianVaultPathV1();

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return
                "Obsidian-status:\n" +
                "- Vault: inte konfigurerat\n" +
                "- Läge: read-only planering\n" +
                "- Skrivning/sync: av\n\n" +
                "Nästa säkra steg blir att lägga till en explicit config med vault-sökväg och sedan bara läsa/söka tills sync går via pending approval.";
        }

        if (!Directory.Exists(configuredPath))
        {
            return
                "Obsidian-status:\n" +
                "- Vault-sökväg finns i config men hittas inte:\n" +
                configuredPath + "\n" +
                "- Skrivning/sync: av";
        }

        return
            "Obsidian-status:\n" +
            "- Vault hittad: " + configuredPath + "\n" +
            "- Läge: read-only status\n" +
            "- Skrivning/sync: av tills PendingApproval-flöde finns.";
    }

    private static string ReadConfiguredObsidianVaultPathV1()
    {
        var configPath = Path.Combine(ProjectRoot, "config", "obsidian_path.txt");

        if (!File.Exists(configPath))
            return "";

        var configuredPath = File.ReadAllText(configPath).Trim();
        if (string.IsNullOrWhiteSpace(configuredPath))
            return "";

        if (!Path.IsPathFullyQualified(configuredPath))
            return "";

        return configuredPath;
    }

    private static string SaveSmartMemory(string input, string type, int importance, string tags, int skipWords)
    {
        var text = ExtractValue(input, skipWords);

        if (string.IsNullOrWhiteSpace(text))
            return "Skriv så här: smart minne: något viktigt";

        if (LooksLikeActualSecret(text))
            return "Jag sparade inte detta eftersom det verkar innehålla en faktisk hemlighet, token, API-nyckel eller ett lösenord.";

        Directory.CreateDirectory(Path.Combine(ProjectRoot, "data"));

        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
        {
            File.WriteAllText(
                memoryPath,
                "# Jarvis lokalt minne" + Environment.NewLine + Environment.NewLine
            );
        }

        var entry =
            "## " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + Environment.NewLine +
            "Type: " + type + Environment.NewLine +
            "Importance: " + importance + "/5" + Environment.NewLine +
            "Tags: " + tags + Environment.NewLine + Environment.NewLine +
            "Text:" + Environment.NewLine +
            text + Environment.NewLine + Environment.NewLine;

        File.AppendAllText(memoryPath, entry);
        RecordMemoryChangeV1("sparat (" + type + ")", text);

        // Auto-promote till vault: skapa även en fil i vault/auto/<datum>-<topic>.md
        // så minnet syns i Brain-vyn och blir sökbart av VaultSearcher (BR2/BR6).
        try
        {
            var vaultDir = Path.Combine(ProjectRoot, "vault", "auto");
            Directory.CreateDirectory(vaultDir);
            var topic = SanitizeVaultNoteNameV1(text);
            if (string.IsNullOrEmpty(topic)) topic = "memory";
            if (topic.Length > 40) topic = topic.Substring(0, 40);
            var fileName = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + topic + ".md";
            var vaultPath = Path.Combine(vaultDir, fileName);
            var fm =
                "---\n" +
                "type: auto-memory\n" +
                "memory_type: " + type + "\n" +
                "importance: " + importance + "/5\n" +
                "tags: [" + tags + ", auto-promoted]\n" +
                "created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                "---\n\n# " + topic + "\n\n" + text + "\n";
            File.WriteAllText(vaultPath, fm);
            VaultSearcher.InvalidateIndex();
        }
        catch { /* vault-promotion är best-effort, ska inte blocka memory.md-write */ }

        return $"Smart Memory sparat i memory.md + vault/auto/. Type: {type}, Importance: {importance}/5.";
    }

    private static string ExtractValue(string input, int skipWords)
    {
        var colon = input.IndexOf(':');
        if (colon >= 0)
            return input[(colon + 1)..].Trim();

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= skipWords)
            return "";

        return string.Join(" ", parts.Skip(skipWords)).Trim();
    }

    private static bool LooksLikeActualSecret(string text)
    {
        var lower = text.ToLowerInvariant();

        var dangerousPatterns = new[]
        {
            @"password\s*[:=]",
            @"lösenord\s*[:=]",
            @"losenord\s*[:=]",
            @"api[_ -]?key\s*[:=]",
            @"secret\s*[:=]",
            @"token\s*[:=]",
            @"private[_ -]?key\s*[:=]",
            @"sk-[a-zA-Z0-9]{20,}",
            @"ghp_[a-zA-Z0-9]{20,}"
        };

        return dangerousPatterns.Any(pattern => Regex.IsMatch(lower, pattern));
    }

    private static string SearchMemory(string input)
    {
        var query = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(query))
            return "Skriv så här: sök minne: Jarvis";

        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Det finns inget lokalt minne ännu.";

        var content = File.ReadAllText(memoryPath);

        if (string.IsNullOrWhiteSpace(content))
            return "Det finns inget lokalt minne ännu.";

        var entries = Regex.Split(content, @"(?m)^## ")
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Where(entry => entry.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        if (entries.Count == 0)
            return "Inga träffar i memory.md för: " + query;

        return "Träffar i memory.md:\n\n" + string.Join("\n\n---\n\n", entries);
    }

    private static string ShowMemory()
    {
        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Det finns inget lokalt minne ännu.";

        var content = File.ReadAllText(memoryPath).Trim();

        if (string.IsNullOrWhiteSpace(content))
            return "Det finns inget lokalt minne ännu.";

        var maxLength = 3000;
        if (content.Length > maxLength)
            content = content[^maxLength..];

        return "Lokalt minne från memory.md:\n" + content;
    }

    private static string ShowMemoriesByType(string type, string title)
    {
        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Det finns inget lokalt minne ännu.";

        var content = File.ReadAllText(memoryPath);

        if (string.IsNullOrWhiteSpace(content))
            return "Det finns inget lokalt minne ännu.";

        var entries = Regex.Split(content, @"(?m)^## ")
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Where(entry => entry.Contains("Type: " + type, StringComparison.OrdinalIgnoreCase))
            .TakeLast(20)
            .ToList();

        if (entries.Count == 0)
            return "Inga minnen hittades för type: " + type;

        return title + ":\n\n" + string.Join("\n\n---\n\n", entries);
    }
    private static string SummarizeMemory()
    {
        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Det finns inget lokalt minne ännu.";

        var content = File.ReadAllText(memoryPath);

        if (string.IsNullOrWhiteSpace(content))
            return "Det finns inget lokalt minne ännu.";

        var entries = Regex.Split(content, @"(?m)^## ")
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Where(entry => entry.Contains("Type:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var total = entries.Count;
        var important = entries.Count(entry => entry.Contains("Type: important", StringComparison.OrdinalIgnoreCase));
        var project = entries.Count(entry => entry.Contains("Type: project_fact", StringComparison.OrdinalIgnoreCase));
        var notes = entries.Count(entry => entry.Contains("Type: note", StringComparison.OrdinalIgnoreCase));

        var latest = entries
            .TakeLast(5)
            .Select(entry =>
            {
                var lines = entry.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .ToList();

                var date = lines.FirstOrDefault() ?? "okänt datum";
                var type = lines.FirstOrDefault(line => line.StartsWith("Type:", StringComparison.OrdinalIgnoreCase)) ?? "Type: okänd";
                var importance = lines.FirstOrDefault(line => line.StartsWith("Importance:", StringComparison.OrdinalIgnoreCase)) ?? "Importance: okänd";

                var textIndex = lines.FindIndex(line => line.Equals("Text:", StringComparison.OrdinalIgnoreCase));
                var text = textIndex >= 0 && textIndex + 1 < lines.Count ? lines[textIndex + 1] : "(ingen text)";

                if (text.Length > 120)
                    text = text[..120] + "...";

                return "- " + date + " | " + type + " | " + importance + " | " + text;
            })
            .ToList();

        return
            "Minnessammanfattning:\n" +
            "- Totalt antal smarta minnen: " + total + "\n" +
            "- Viktiga minnen: " + important + "\n" +
            "- Projektminnen: " + project + "\n" +
            "- Vanliga notes: " + notes + "\n\n" +
            "Senaste minnen:\n" +
            (latest.Count > 0 ? string.Join("\n", latest) : "Inga smarta minnen hittades.");
    }
    private static string MemoryStatus()
    {
        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Minnesfil finns inte ännu.";

        var lines = File.ReadAllLines(memoryPath);

        var memoryCount = lines.Count(line => line.StartsWith("## "));
        var importantCount = lines.Count(line => line.Trim().Equals("Type: important", StringComparison.OrdinalIgnoreCase));
        var projectCount = lines.Count(line => line.Trim().Equals("Type: project_fact", StringComparison.OrdinalIgnoreCase));

        return
            "Smart Memory status:\n" +
            "- Antal minnen: " + memoryCount + "\n" +
            "- Viktiga minnen: " + importantCount + "\n" +
            "- Projektminnen: " + projectCount + "\n" +
            "- Fil: " + memoryPath;
    }

    private static string PrepareForgetMemory(string input)
    {
        var query = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(query))
            return "Skriv så här: glöm minne: något att söka efter";

        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Det finns inget lokalt minne ännu.";

        var content = File.ReadAllText(memoryPath);

        var matches = ParseMemoryBlocks(content)
            .Where(block => block.FullBlock.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        PendingForgetMatches = matches;

        if (matches.Count == 0)
            return "Jag hittade inga minnen som matchar: " + query;

        var lines = matches
            .Select((block, index) => (index + 1) + ". " + block.Preview)
            .ToList();

        return
            "Jag hittade dessa minnen:\n\n" +
            string.Join("\n\n", lines) +
            "\n\nFör att arkivera ett minne, skriv exakt:\n" +
            "bekräfta glöm 1";
    }

    private static string ConfirmForgetMemory(string command)
    {
        var match = Regex.Match(command, @"^bekrafta glom\s+(\d+)$");

        if (!match.Success)
            return "Skriv så här: bekräfta glöm 1";

        var number = int.Parse(match.Groups[1].Value);
        var index = number - 1;

        if (PendingForgetMatches.Count == 0)
            return "Det finns ingen aktiv glöm-lista. Skriv först: glöm minne: text";

        if (index < 0 || index >= PendingForgetMatches.Count)
            return "Ogiltigt nummer. Välj ett nummer från listan.";

        var selected = PendingForgetMatches[index];

        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");
        var archivePath = Path.Combine(ProjectRoot, "data", "memory_archive.md");

        if (!File.Exists(memoryPath))
            return "Minnesfilen finns inte längre.";

        var content = File.ReadAllText(memoryPath);
        var position = content.IndexOf(selected.FullBlock, StringComparison.Ordinal);

        if (position < 0)
            return "Jag kunde inte hitta exakt minnesblock i memory.md. Försök söka igen.";

        var updated = content.Remove(position, selected.FullBlock.Length).TrimEnd() + Environment.NewLine + Environment.NewLine;
        File.WriteAllText(memoryPath, updated);
        RecordMemoryChangeV1("arkiverat", selected.Preview);

        if (!File.Exists(archivePath))
        {
            File.WriteAllText(
                archivePath,
                "# Jarvis arkiverade minnen" + Environment.NewLine + Environment.NewLine
            );
        }

        var archiveEntry =
            "## Archived " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + Environment.NewLine +
            selected.FullBlock.Trim() + Environment.NewLine + Environment.NewLine;

        File.AppendAllText(archivePath, archiveEntry);

        PendingForgetMatches.Clear();

        return "Minnet är arkiverat i memory_archive.md och borttaget från aktivt memory.md.";
    }

    private static List<MemoryBlock> ParseMemoryBlocks(string content)
    {
        return Regex.Matches(content, @"(?ms)^## .+?(?=^## |\z)")
            .Select(match =>
            {
                var fullBlock = match.Value;
                var preview = FormatMemoryPreview(fullBlock);
                return new MemoryBlock(fullBlock, preview);
            })
            .ToList();
    }

    private static string FormatMemoryPreview(string block)
    {
        var lines = block
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Take(8)
            .ToList();

        var preview = string.Join(" | ", lines);

        if (preview.Length > 450)
            preview = preview[..450] + "...";

        return preview;
    }
    private static string OpenMemoryTool()
    {
        Directory.CreateDirectory(Path.Combine(ProjectRoot, "data"));

        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
        {
            File.WriteAllText(
                memoryPath,
                "# Jarvis lokalt minne" + Environment.NewLine + Environment.NewLine
            );
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "notepad.exe",
            Arguments = "\"" + memoryPath + "\"",
            UseShellExecute = true
        });

        return "Öppnar minnesfil: " + memoryPath;
    }

    private static string CommandHelpTool()
    {
        return
            "Så använder du Jarvis:\n\n" +
            "Jarvis-kommandon skriver du i Jarvis-fönstret, inte i PowerShell.\n" +
            "PowerShell används bara för build/start/kodkommandon.\n\n" +

            "Vanliga kommandon:\n" +
            "- hjälp\n" +
            "- kommandohjälp\n" +
            "- status\n" +
            "- diskstatus\n" +
            "- cachestatus\n" +
            "- kontrollera cache\n\n" +

            "Minne:\n" +
            "- smart minne: text\n" +
            "- viktigt minne: text\n" +
            "- projektminne: text\n" +
            "- visa minne\n" +
            "- sök minne: text\n" +
            "- sammanfatta minne\n" +
            "- glöm minne: text\n" +
            "- bekräfta glöm 1\n\n" +

            "Filer och projekt:\n" +
            "- lista filer\n" +
            "- lista filer i docs\n" +
            "- lista md filer\n" +
            "- lista projektfiler\n" +
            "- indexera projekt\n" +
            "- läs fil: docs/PROJECT_INDEX.md\n" +
            "- skriv fil: docs/test.md = text\n" +
            "- lägg till fil: docs/test.md = text\n\n" +

            "Viktigt:\n" +
            "- Använd relativa sökvägar, till exempel docs/test.md\n" +
            "- Använd inte F:\\Jarvis-clean\\docs\\test.md i Jarvis-chatten\n" +
            "- Jarvis får bara jobba i F:\\Jarvis-clean\n" +
            "- F:\\New project är bara referens och ska inte ändras.";
    }

    private static string ListProjectFilesByExtension(string extension, string title)
    {
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints"
        };

        try
        {
            var files = Directory.GetFiles(ProjectRoot, "*" + extension, SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return !parts.Any(part => excludedDirs.Contains(part));
                })
                .Select(file => Path.GetRelativePath(ProjectRoot, file))
                .OrderBy(x => x)
                .Take(120)
                .ToList();

            if (files.Count == 0)
                return "Hittade inga " + extension + "-filer.";

            return title + ":\n" + string.Join("\n", files.Select(file => "- " + file));
        }
        catch (Exception ex)
        {
            return "Kunde inte lista filer: " + ex.Message;
        }
    }

    private static string ListProjectFilesCompact()
    {
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs"
        };

        try
        {
            var files = Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (parts.Any(part => excludedDirs.Contains(part)))
                        return false;

                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return allowedExtensions.Contains(Path.GetExtension(file));
                })
                .Select(file => Path.GetRelativePath(ProjectRoot, file))
                .OrderBy(x => x)
                .Take(150)
                .ToList();

            if (files.Count == 0)
                return "Hittade inga projektfiler.";

            return "Projektfiler i F:\\Jarvis-clean:\n" + string.Join("\n", files.Select(file => "- " + file));
        }
        catch (Exception ex)
        {
            return "Kunde inte lista projektfiler: " + ex.Message;
        }
    }
    private static string ProjectIndexTool()
    {
        var indexPath = Path.Combine(ProjectRoot, "docs", "PROJECT_INDEX.md");

        try
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "docs"));

            var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints", ".tmp", ".vs", ".nuget", "backups", "data"
            };

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs"
            };

            var directories = Directory.GetDirectories(ProjectRoot, "*", SearchOption.AllDirectories)
                .Where(dir =>
                {
                    var parts = Path.GetRelativePath(ProjectRoot, dir)
                        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    return !parts.Any(part => excludedDirs.Contains(part));
                })
                .Select(dir => Path.GetRelativePath(ProjectRoot, dir))
                .OrderBy(x => x)
                .Take(120)
                .ToList();

            var files = Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (parts.Any(part => excludedDirs.Contains(part)))
                        return false;

                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return allowedExtensions.Contains(Path.GetExtension(file));
                })
                .Select(file => Path.GetRelativePath(ProjectRoot, file))
                .OrderBy(x => x)
                .Take(250)
                .ToList();

            var importantFiles = files
                .Where(file =>
                    file.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("AGENTS.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("BUILD_PLAN.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("CURRENT_STATE.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("MASTER_PLAN.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("TODO_NEXT.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("RELEASE_STATUS.md", StringComparison.OrdinalIgnoreCase) ||
                    file.Equals("OFFLINE_PLAN.md", StringComparison.OrdinalIgnoreCase) ||
                    file.StartsWith("docs", StringComparison.OrdinalIgnoreCase) ||
                    file.StartsWith("app", StringComparison.OrdinalIgnoreCase) ||
                    file.StartsWith("dashboard", StringComparison.OrdinalIgnoreCase)
                )
                .Take(80)
                .ToList();

            var content =
                "# PROJECT_INDEX.md — Jarvis project index" + Environment.NewLine + Environment.NewLine +
                "Senast uppdaterad: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + Environment.NewLine +
                "## Syfte" + Environment.NewLine + Environment.NewLine +
                "Detta index hjälper Jarvis att förstå projektstrukturen i F:\\Jarvis-clean." + Environment.NewLine + Environment.NewLine +
                "## Regler" + Environment.NewLine + Environment.NewLine +
                "- Indexet gäller bara F:\\Jarvis-clean." + Environment.NewLine +
                "- F:\\New project ska inte ändras." + Environment.NewLine +
                "- Tunga mappar som bin, obj, dist, .git och node_modules exkluderas." + Environment.NewLine + Environment.NewLine +
                "## Viktiga mappar" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, directories.Select(d => "- " + d)) + Environment.NewLine + Environment.NewLine +
                "## Viktiga filer" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, importantFiles.Select(f => "- " + f)) + Environment.NewLine + Environment.NewLine +
                "## Alla indexerade filer" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, files.Select(f => "- " + f)) + Environment.NewLine + Environment.NewLine +
                "## Nästa steg" + Environment.NewLine + Environment.NewLine +
                "- Använd `läs fil: docs/PROJECT_INDEX.md` för att visa indexet." + Environment.NewLine +
                "- Nästa Offline Codex-fas blir: föreslå ändring utan att skriva direkt." + Environment.NewLine;

            File.WriteAllText(indexPath, content);

            return "Projekt-index skapat: " + indexPath;
        }
        catch (Exception ex)
        {
            return "Kunde inte skapa projekt-index: " + ex.Message;
        }
    }
    private static string CreateCheckpointTool() => CreateNamedCheckpointTool(null);

    private static string CreateNamedCheckpointTool(string? userName)
    {
        try
        {
            var checkpointRoot = Path.Combine(ProjectRoot, ".checkpoints");
            Directory.CreateDirectory(checkpointRoot);

            var sanitizedName = SanitizeCheckpointNameV1(userName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var checkpointName = string.IsNullOrEmpty(sanitizedName)
                ? timestamp
                : timestamp + "_" + sanitizedName;
            var checkpointPath = Path.Combine(checkpointRoot, checkpointName);
            Directory.CreateDirectory(checkpointPath);

            var files = GetCheckpointFiles();

            var copied = 0;
            var failed = 0;

            foreach (var sourceFile in files)
            {
                try
                {
                    var relative = Path.GetRelativePath(ProjectRoot, sourceFile);
                    var targetFile = Path.Combine(checkpointPath, relative);

                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    copied++;
                }
                catch
                {
                    failed++;
                }
            }

            var manifest =
                "# Checkpoint " + checkpointName + Environment.NewLine + Environment.NewLine +
                "Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                "ProjectRoot: " + ProjectRoot + Environment.NewLine +
                "Copied files: " + copied + Environment.NewLine +
                "Failed files: " + failed + Environment.NewLine;

            File.WriteAllText(Path.Combine(checkpointPath, "CHECKPOINT.md"), manifest);

            return
                "Checkpoint skapad:\n" +
                checkpointPath + "\n\n" +
                "Kopierade filer: " + copied + "\n" +
                "Misslyckades: " + failed;
        }
        catch (Exception ex)
        {
            return "Kunde inte skapa checkpoint: " + ex.Message;
        }
    }

    private static string ListCheckpointsTool()
    {
        var checkpointRoot = Path.Combine(ProjectRoot, ".checkpoints");

        if (!Directory.Exists(checkpointRoot))
            return "Det finns inga checkpoints ännu.";

        var checkpoints = Directory.GetDirectories(checkpointRoot)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderByDescending(name => name)
            .Take(20)
            .ToList();

        if (checkpoints.Count == 0)
            return "Det finns inga checkpoints ännu.";

        return "Checkpoints:\n" + string.Join("\n", checkpoints.Select(name => "- " + name));
    }

    // Tillåter bara säkra tecken i checkpoint-namn så path-traversal eller specialtecken inte slinker in.
    private static string SanitizeCheckpointNameV1(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "";
        var trimmed = rawName.Trim();
        var allowed = new System.Text.StringBuilder();
        foreach (var ch in trimmed)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
                allowed.Append(ch);
            else if (ch == ' ')
                allowed.Append('-');
            // alla andra tecken (inkl. /, \, :, ., #) tas helt enkelt bort
        }
        var cleaned = allowed.ToString().Trim('-', '_');
        if (cleaned.Length > 40) cleaned = cleaned.Substring(0, 40);
        return cleaned;
    }

    // Hittar checkpoint-mapp som matchar ett angivet namn (med eller utan timestamp-prefix).
    // Returnerar null om ingen unik match hittas.
    private static string? ResolveCheckpointByNameV1(string name)
    {
        var checkpointRoot = Path.Combine(ProjectRoot, ".checkpoints");
        if (!Directory.Exists(checkpointRoot)) return null;
        var sanitized = SanitizeCheckpointNameV1(name);
        if (string.IsNullOrEmpty(sanitized)) return null;

        var dirs = Directory.GetDirectories(checkpointRoot)
            .Select(d => new { Path = d, Name = Path.GetFileName(d) ?? "" })
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList();

        // Exakt matchning på <timestamp>_<sanitized> eller bara <sanitized>
        var exact = dirs.FirstOrDefault(x =>
            x.Name.Equals(sanitized, StringComparison.OrdinalIgnoreCase) ||
            x.Name.EndsWith("_" + sanitized, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.Path;

        // Substring-matchning: ett namn som "innan" matchar "20260509_120000_innan-port".
        var partial = dirs.Where(x =>
            x.Name.IndexOf(sanitized, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        if (partial.Count == 1) return partial[0].Path;
        return null;
    }

    private static string RestoreCheckpointByNameTool(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return RestoreLatestCheckpointTool();

        var resolved = ResolveCheckpointByNameV1(userName);
        if (resolved is null)
            return "Hittar ingen unik checkpoint som matchar: " + userName + "\nKör 'lista checkpoints' för att se alla namn.";

        return RestoreCheckpointFromPathV1(resolved);
    }

    private static string RestoreLatestCheckpointTool()
    {
        var checkpointRoot = Path.Combine(ProjectRoot, ".checkpoints");

        if (!Directory.Exists(checkpointRoot))
            return "Det finns inga checkpoints att återställa.";

        var latest = Directory.GetDirectories(checkpointRoot)
            .OrderByDescending(path => Path.GetFileName(path))
            .FirstOrDefault();

        if (latest is null)
            return "Det finns inga checkpoints att återställa.";

        return RestoreCheckpointFromPathV1(latest);
    }

    // Delad återställnings-logik för "senaste" och "namngiven" checkpoint.
    private static string RestoreCheckpointFromPathV1(string checkpointPath)
    {
        try
        {
            var restored = 0;
            var failed = 0;

            var files = Directory.GetFiles(checkpointPath, "*", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).Equals("CHECKPOINT.md", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var checkpointFile in files)
            {
                try
                {
                    var relative = Path.GetRelativePath(checkpointPath, checkpointFile);
                    var targetFile = Path.Combine(ProjectRoot, relative);

                    var fullTarget = Path.GetFullPath(targetFile);
                    var fullRoot = Path.GetFullPath(ProjectRoot);

                    if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        failed++;
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(fullTarget)!);
                    File.Copy(checkpointFile, fullTarget, overwrite: true);
                    restored++;
                }
                catch
                {
                    failed++;
                }
            }

            return
                "Checkpoint återställd:\n" +
                checkpointPath + "\n\n" +
                "Återställda filer: " + restored + "\n" +
                "Misslyckades: " + failed + "\n\n" +
                "Tips: kör dotnet build efter återställning om kodfiler ändrades.";
        }
        catch (Exception ex)
        {
            return "Kunde inte återställa checkpoint: " + ex.Message;
        }
    }

    private static List<string> GetCheckpointFiles()
    {
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints", ".checkpoints"
        };

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs"
        };

        return Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories)
            .Where(file =>
            {
                var relative = Path.GetRelativePath(ProjectRoot, file);
                var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (parts.Any(part => excludedDirs.Contains(part)))
                    return false;

                if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                    return false;

                return allowedExtensions.Contains(Path.GetExtension(file));
            })
            .ToList();
    }
    private static string ApprovePendingChangeTool()
    {
        var pendingPath = Path.Combine(ProjectRoot, "docs", "PENDING_CHANGE.md");

        if (!File.Exists(pendingPath))
            return "Det finns inget aktivt ändringsförslag att godkänna.";

        try
        {
            var pending = File.ReadAllText(pendingPath);

            var typeMatch = Regex.Match(pending, @"(?m)^Change type:\s*(.+)$");
            var targetMatch = Regex.Match(pending, @"(?m)^Target file:\s*(.+)$");

            if (!typeMatch.Success)
                return "Kan inte godkänna: PENDING_CHANGE.md saknar Change type.";

            if (!targetMatch.Success)
                return "Kan inte godkänna: PENDING_CHANGE.md saknar Target file.";

            var changeType = typeMatch.Groups[1].Value.Trim();
            var relativePath = targetMatch.Groups[1].Value.Trim();

            if (!changeType.Equals("heading", StringComparison.OrdinalIgnoreCase))
                return "Kan bara godkänna säkra heading-förslag just nu. Avbryt detta förslag eller skapa ett nytt med: föreslå rubrik";

            var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
            if (!safe)
                return error;

            var headingMatch = Regex.Match(
                pending,
                @"(?ms)## Föreslagen text att lägga överst\s*```md\s*(.*?)\s*```"
            );

            if (!headingMatch.Success)
            {
                headingMatch = Regex.Match(pending, @"(?ms)```md\s*(.*?)\s*```");
            }

            if (!headingMatch.Success)
                return "Kan inte godkänna: hittade ingen föreslagen markdown-text.";

            var heading = headingMatch.Groups[1].Value.Trim();

            if (string.IsNullOrWhiteSpace(heading))
                return "Kan inte godkänna: rubriken är tom.";

            if (!heading.StartsWith("# "))
                return "Kan inte godkänna: förslaget måste vara en markdown-rubrik som börjar med # ";

            var currentContent = File.ReadAllText(fullPath);

            var archiveDir = Path.Combine(ProjectRoot, "docs", "change_archive");
            Directory.CreateDirectory(archiveDir);

            var safeName = relativePath
                .Replace("\\", "_")
                .Replace("/", "_")
                .Replace(":", "_");

            var backupPath = Path.Combine(
                archiveDir,
                "backup_before_approved_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + safeName
            );

            File.WriteAllText(backupPath, currentContent);

            if (currentContent.TrimStart().StartsWith(heading, StringComparison.OrdinalIgnoreCase))
            {
                var alreadyArchivePath = Path.Combine(
                    archiveDir,
                    "PENDING_CHANGE_already_applied_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md"
                );

                File.Move(pendingPath, alreadyArchivePath, overwrite: true);

                return
                    "Rubriken fanns redan högst upp. Originalfilen ändrades inte.\n" +
                    "Förslaget arkiverades här: " + alreadyArchivePath;
            }

            var newContent = heading + Environment.NewLine + Environment.NewLine + currentContent;
            File.WriteAllText(fullPath, newContent);

            var approvedArchivePath = Path.Combine(
                archiveDir,
                "PENDING_CHANGE_approved_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md"
            );

            File.Move(pendingPath, approvedArchivePath, overwrite: true);

            return
                "Ändring godkänd och applicerad.\n" +
                "Fil uppdaterad: " + fullPath + "\n" +
                "Backup skapad: " + backupPath + "\n" +
                "Förslag arkiverat: " + approvedArchivePath;
        }
        catch (Exception ex)
        {
            return "Kunde inte godkänna ändringen: " + ex.Message;
        }
    }
    private static string CancelPendingChangeTool()
    {
        var pendingPath = Path.Combine(ProjectRoot, "docs", "PENDING_CHANGE.md");

        if (!File.Exists(pendingPath))
            return "Det finns inget aktivt ändringsförslag att avbryta.";

        var archiveDir = Path.Combine(ProjectRoot, "docs", "change_archive");
        Directory.CreateDirectory(archiveDir);

        var archivePath = Path.Combine(
            archiveDir,
            "PENDING_CHANGE_cancelled_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md"
        );

        File.Move(pendingPath, archivePath, overwrite: true);

        return "Ändringsförslaget är avbrutet och arkiverat här: " + archivePath;
    }
    private static string ProposeHeadingTool(string input)
    {
        var raw = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(raw))
            return "Skriv så här: föreslå rubrik: docs/test-agent.md = Test Agent";

        var parts = CommandRouterV1.SplitFileCommandArguments(raw, 2);

        if (parts.Length < 2)
            return "Du måste använda = mellan fil och rubrik. Exempel: föreslå rubrik: docs/test-agent.md = Test Agent";

        var relativePath = parts[0].Trim();
        var heading = parts[1].Trim().TrimStart('#').Trim();

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Filnamn saknas.";

        if (string.IsNullOrWhiteSpace(heading))
            return "Rubrik saknas.";

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        try
        {
            var currentContent = File.ReadAllText(fullPath);

            var proposedTop = "# " + heading;
            var alreadyHasHeading = currentContent.TrimStart().StartsWith("# " + heading, StringComparison.OrdinalIgnoreCase);

            var suggestion = alreadyHasHeading
                ? "Filen verkar redan börja med denna rubrik:\n\n" + proposedTop
                : "Lägg till denna rad högst upp i filen:\n\n```md\n" + proposedTop + "\n```\n\nOriginalfilen ska fortfarande inte ändras förrän du godkänner.";

            Directory.CreateDirectory(Path.Combine(ProjectRoot, "docs"));

            var pendingPath = Path.Combine(ProjectRoot, "docs", "PENDING_CHANGE.md");

            var pendingContent =
                "# PENDING_CHANGE.md — Föreslagen ändring" + Environment.NewLine + Environment.NewLine +
                "Status: pending" + Environment.NewLine +
                "Change type: heading" + Environment.NewLine +
                "Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                "Target file: " + relativePath + Environment.NewLine +
                "Full path: " + fullPath + Environment.NewLine + Environment.NewLine +
                "## Instruktion" + Environment.NewLine + Environment.NewLine +
                "Lägg till rubrik högst upp: " + heading + Environment.NewLine + Environment.NewLine +
                "## Viktigt" + Environment.NewLine + Environment.NewLine +
                "Originalfilen har INTE ändrats. Detta är bara ett säkert lokalt förslag." + Environment.NewLine + Environment.NewLine +
                "## Föreslagen ändring" + Environment.NewLine + Environment.NewLine +
                suggestion + Environment.NewLine + Environment.NewLine +
                "## Föreslagen text att lägga överst" + Environment.NewLine + Environment.NewLine +
                "```md" + Environment.NewLine +
                proposedTop + Environment.NewLine +
                "```" + Environment.NewLine + Environment.NewLine +
                "## Nästa steg" + Environment.NewLine + Environment.NewLine +
                "- Granska förslaget." + Environment.NewLine +
                "- Senare bygger vi: godkänn ändring / avbryt ändring." + Environment.NewLine;

            File.WriteAllText(pendingPath, pendingContent);

            return "Säkert rubrikförslag skapat utan att ändra originalfilen: " + pendingPath;
        }
        catch (Exception ex)
        {
            return "Kunde inte skapa rubrikförslag: " + ex.Message;
        }
    }
    private static async Task<string?> TryHandleNaturalProposalAsync(string originalText, string command)
    {
        var wantsProposal =
            command.Contains("foresla") ||
            command.Contains("forsla") ||
            command.Contains("forslag") ||
            command.Contains("ide") ||
            command.Contains("förslag");

        if (!wantsProposal)
            return null;

        var fileMatch = Regex.Match(
            originalText,
            @"(?i)([a-z0-9_\-\\/]+?\.(md|txt|json|cs|html|css|js|ps1))"
        );

        if (!fileMatch.Success)
        {
            return
                "Jag kan föreslå saker säkert, men jag behöver veta vilken fil det gäller.\n\n" +
                "Exempel:\n" +
                "föreslå ide: docs/test-agent.md\n\n" +
                "eller:\n" +
                "kan du föreslå en bättre rubrik för docs/test-agent.md";
        }

        var relativePath = fileMatch.Groups[1].Value.Replace("\\", "/").Trim();

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out _, out var error);
        if (!safe)
            return error;

        var instruction = originalText;

        instruction = Regex.Replace(instruction, @"(?i)föreslå", "", RegexOptions.IgnoreCase);
        instruction = Regex.Replace(instruction, @"(?i)foresla", "", RegexOptions.IgnoreCase);
        instruction = Regex.Replace(instruction, @"(?i)förslå", "", RegexOptions.IgnoreCase);
        instruction = Regex.Replace(instruction, @"(?i)forsla", "", RegexOptions.IgnoreCase);
        instruction = Regex.Replace(instruction, @"(?i)förslag", "", RegexOptions.IgnoreCase);
        instruction = Regex.Replace(instruction, @"(?i)forslag", "", RegexOptions.IgnoreCase);
        instruction = instruction.Replace(relativePath, "", StringComparison.OrdinalIgnoreCase);
        instruction = instruction.Replace("|", " ");
        instruction = instruction.Replace(":", " ");
        instruction = Regex.Replace(instruction, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(instruction) || instruction.Length < 4)
        {
            instruction =
                "Föreslå en relevant förbättring för filen. " +
                "Hitta inte på externa länkar, nya beroenden eller filer som inte finns. " +
                "Föreslå bara en liten säker ändring.";
        }

        var syntheticCommand = "föreslå ändring: " + relativePath + " | " + instruction;

        return await ProposeChangeToolAsync(syntheticCommand);
    }
    private static async Task<string> ProposeChangeToolAsync(string input)
    {
        var raw = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(raw))
            return "Skriv så här: föreslå ändring: docs/test-agent.md = lägg till en rubrik";

        var parts = CommandRouterV1.SplitFileCommandArguments(raw, 2);

        if (parts.Length < 2)
            return "Du måste använda = mellan fil och instruktion. Exempel: föreslå ändring: docs/test-agent.md = lägg till en rubrik";

        var relativePath = parts[0].Trim();
        var instruction = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Filnamn saknas.";

        if (string.IsNullOrWhiteSpace(instruction))
            return "Instruktion saknas.";

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        try
        {
            var currentContent = File.ReadAllText(fullPath);

            var excerpt = currentContent;
            if (excerpt.Length > 6000)
                excerpt = excerpt[..6000] + "\n\n...filen är längre, bara första delen skickades till modellen.";

            var prompt =
                "Du är Jarvis Offline Codex-läge. Skapa bara ett ändringsförslag på svenska.\n\n" +
                "VIKTIGT:\n" +
                "- Originalfilen ska INTE ändras.\n" +
                "- Säg inte att ändringen redan är gjord.\n" +
                "- Föreslå exakt vad användaren bör lägga till/ändra.\n" +
                "- Håll svaret kort och konkret.\n\n" +
                "Målfil: " + relativePath + "\n\n" +
                "Instruktion:\n" + instruction + "\n\n" +
                "Nuvarande innehåll:\n```text\n" + excerpt + "\n```";

            var suggestion = await AskOllamaAsync(prompt);

            Directory.CreateDirectory(Path.Combine(ProjectRoot, "docs"));

            var pendingPath = Path.Combine(ProjectRoot, "docs", "PENDING_CHANGE.md");

            var pendingContent =
                "# PENDING_CHANGE.md — Föreslagen ändring" + Environment.NewLine + Environment.NewLine +
                "Status: pending" + Environment.NewLine +
                "Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                "Target file: " + relativePath + Environment.NewLine +
                "Full path: " + fullPath + Environment.NewLine + Environment.NewLine +
                "## Instruktion" + Environment.NewLine + Environment.NewLine +
                instruction + Environment.NewLine + Environment.NewLine +
                "## Viktigt" + Environment.NewLine + Environment.NewLine +
                "Originalfilen har INTE ändrats. Detta är bara ett förslag." + Environment.NewLine + Environment.NewLine +
                "## Jarvis förslag" + Environment.NewLine + Environment.NewLine +
                suggestion + Environment.NewLine + Environment.NewLine +
                "## Nästa steg" + Environment.NewLine + Environment.NewLine +
                "- Granska förslaget." + Environment.NewLine +
                "- Senare bygger vi: godkänn ändring / avbryt ändring." + Environment.NewLine;

            File.WriteAllText(pendingPath, pendingContent);

            return "Ändringsförslag skapat utan att ändra originalfilen: " + pendingPath;
        }
        catch (Exception ex)
        {
            return "Kunde inte skapa ändringsförslag: " + ex.Message;
        }
    }
    private static string ActiveFilePath()
    {
        return Path.Combine(ProjectRoot, "config", "active_file.txt");
    }

    private static string SetActiveFile(string relativePath)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "config"));
            File.WriteAllText(ActiveFilePath(), relativePath.Trim());
            return relativePath.Trim();
        }
        catch
        {
            return relativePath.Trim();
        }
    }

    private static string GetActiveFile()
    {
        try
        {
            var path = ActiveFilePath();

            if (!File.Exists(path))
                return "";

            return File.ReadAllText(path).Trim();
        }
        catch
        {
            return "";
        }
    }

    private static string ShowActiveFileTool()
    {
        var active = GetActiveFile();

        if (string.IsNullOrWhiteSpace(active))
            return "Ingen aktiv fil ännu. Läs en fil först, till exempel: läs fil: docs/test-agent.md";

        return "Aktiv fil:\n" + active;
    }

    private static string ReadActiveFileTool()
    {
        var active = GetActiveFile();

        if (string.IsNullOrWhiteSpace(active))
            return "Ingen aktiv fil ännu. Läs en fil först, till exempel: läs fil: docs/test-agent.md";

        return ReadProjectFileTool("läs fil: " + active);
    }

    private static string ProposeHeadingForActiveFileTool(string input)
    {
        var active = GetActiveFile();

        if (string.IsNullOrWhiteSpace(active))
            return "Ingen aktiv fil ännu. Läs en fil först, till exempel: läs fil: docs/test-agent.md";

        var heading = ExtractValue(input, 3);

        if (string.IsNullOrWhiteSpace(heading))
            return "Skriv så här: föreslå rubrik här: Min rubrik";

        return ProposeHeadingTool("föreslå rubrik: " + active + " | " + heading);
    }
    private static string ReadProjectFileTool(string input)
    {
        var relativePath = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Skriv så här: läs fil: docs/SESSION_LOG.md";

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        try
        {
            var content = File.ReadAllText(fullPath);

            if (content.Length > 8000)
                content = content[..8000] + "\n\n...filen är längre, visar bara första delen.";

            SetActiveFile(relativePath);
            return "Aktiv fil satt till: " + relativePath + "\n\nInnehåll i " + fullPath + ":\n\n" + content;
        }
        catch (Exception ex)
        {
            return "Kunde inte läsa filen: " + ex.Message;
        }
    }

    private static string DesktopStatusTool()
    {
        return DesktopActionGate.Status() + "\n\n" + _uiTarsBridge.Status();
    }

    private static string DesktopEnableTool()
    {
        return DesktopActionGate.Enable() + "\n\n" +
               "Kommandon:\n" +
               "- /skärm\n" +
               "- /desktop klick 100 200\n" +
               "- /desktop dubbelklick 100 200\n" +
               "- /desktop högerklick 100 200\n" +
               "- /desktop drag 100 200 300 400\n" +
               "- /desktop skriv text\n" +
               "- /desktop hotkey ctrl+l\n" +
               "- /desktop scroll down 3\n" +
               "- /desktop fråga klicka på knappen\n\n" +
               "UI-TARS Desktop subprocess kan startas med: /desktop tars start\n" +
               "Varje action blir pending preview innan den körs.";
    }

    private static string DesktopDisableTool()
    {
        if (PendingApprovalStoreV1.Get()?.Type == PendingApprovalTypeV1.DesktopAction)
            PendingApprovalStoreV1.Clear();

        return DesktopActionGate.Disable();
    }

    private static string DesktopScreenshotTool()
    {
        if (!DesktopActionGate.Enabled)
            return "Desktop-control är AV. Starta med: /desktop på";

        var capture = ScreenCapture.CapturePrimaryToFile();
        if (!capture.Ok)
            return "Kunde inte ta screenshot: " + capture.Error;

        DesktopActionGate.Log("SCREENSHOT", capture.Path);
        return "Screenshot sparad:\n" + capture.Path + "\n" + capture.Width + "x" + capture.Height;
    }

    private static string DesktopActionRequestTool(string action, string payload)
    {
        if (!DesktopActionGate.Enabled)
            return "Desktop-control är AV. Starta med: /desktop på";

        if (PendingApprovalStoreV1.HasPending)
            return "Det finns redan en pending åtgärd. Godkänn eller avbryt den innan du skapar en desktop-action.";

        if (!DesktopActionRequestV1.TryCreateManual(action, payload, out var request, out var error))
            return error;

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.DesktopAction,
            Target = request.Kind.ToString(),
            Content = request.ToJson(),
            RequiresUserApproval = true
        });

        return
            "Pending desktop-action skapad. Inget har körts ännu.\n" +
            "Godkänn eller avbryt i popupen.\n\n" +
            request.Preview();
    }

    private static async Task<string> DesktopVisionRequestToolAsync(string instruction)
    {
        instruction = (instruction ?? "").Trim();

        if (!DesktopActionGate.Enabled)
            return "Desktop-control är AV. Starta med: /desktop på";

        if (string.IsNullOrWhiteSpace(instruction))
            return "Skriv: /desktop fråga <vad Jarvis ska göra på skärmen>";

        if (PendingApprovalStoreV1.HasPending)
            return "Det finns redan en pending åtgärd. Godkänn eller avbryt den innan UI-TARS får föreslå en action.";

        var capture = ScreenCapture.CapturePrimaryToFile();
        if (!capture.Ok)
            return "Kunde inte ta screenshot för UI-TARS: " + capture.Error;

        var result = await _uiTarsBridge.ProposeActionAsync(instruction, capture);
        if (!result.Ok || result.Action is null)
            return result.Message + "\n\nScreenshot sparad för manuell felsökning:\n" + capture.Path;

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.DesktopAction,
            Target = result.Action.Kind.ToString(),
            Content = result.Action.ToJson(),
            RequiresUserApproval = true
        });

        return
            result.Message + "\n\n" +
            "Pending desktop-action skapad. Inget har körts ännu.\n" +
            "Godkänn bara om koordinater/action ser rätt ut.\n\n" +
            result.Action.Preview();
    }

    private static string ApprovePendingDesktopActionTool()
    {
        var pending = PendingApprovalStoreV1.Get();
        if (pending is null || pending.Type != PendingApprovalTypeV1.DesktopAction)
            return "Det finns ingen pending desktop-action.";

        var action = DesktopActionRequestV1.FromJson(pending.Content);
        if (action is null)
        {
            PendingApprovalStoreV1.Clear();
            return "Pending desktop-action kunde inte läsas. Den avbröts.";
        }

        var result = DesktopActionExecutor.Execute(action);
        PendingApprovalStoreV1.Clear();
        return result;
    }

    private static string CancelPendingDesktopActionTool()
    {
        var pending = PendingApprovalStoreV1.Get();
        if (pending is null || pending.Type != PendingApprovalTypeV1.DesktopAction)
            return "Det finns ingen pending desktop-action att avbryta.";

        PendingApprovalStoreV1.Clear();
        return "Pending desktop-action avbröts. Inget klick/typ/scroll kördes.";
    }

    private static async Task<string> BuilderStartToolAsync(string description)
    {
        description = (description ?? "").Trim();

        if (string.IsNullOrWhiteSpace(description))
            return "Skriv: /bygg <vad du vill bygga>. Exempel: /bygg en enkel TODO-app i HTML";

        var systemPrompt =
            "Du är Jarvis BuilderMode. Du hjälper användaren bygga rätt sak säkert.\n" +
            "Ställ bara 3-5 konkreta klargörande frågor på svenska. Ingen plan ännu.";
        var questionsResult = await AskOllamaRawModelAsync(
            ModelCatalog.Smart.Name,
            systemPrompt,
            BuilderMode.BuildQuestionPrompt(description)
        );

        var questions = questionsResult.Ok
            ? questionsResult.Text
            : BuilderMode.FallbackQuestions(description);

        var session = BuilderMode.Start(description, questions);

        return
            "BuilderMode startat. Ingen fil har skapats.\n\n" +
            "Idé:\n" + session.Description + "\n\n" +
            "Frågor:\n" + session.Questions + "\n\n" +
            "Svara med:\n" +
            "/bygg svar <dina svar>\n\n" +
            "När du vill skapa en sparbar plan:\n" +
            "/bygg plan";
    }

    private static string BuilderAnswerTool(string answer)
    {
        answer = (answer ?? "").Trim();

        if (string.IsNullOrWhiteSpace(answer))
            return "Skriv: /bygg svar <dina svar på frågorna>";

        if (!BuilderMode.HasActiveSession)
            return "Det finns ingen aktiv builder-session. Starta med: /bygg <beskrivning>";

        if (!BuilderMode.AddAnswer(answer))
            return "Kunde inte spara builder-svaret.";

        return
            "Builder-svar sparat i aktiv session.\n\n" +
            BuilderMode.Status() + "\n\n" +
            "När du är redo: /bygg plan";
    }

    private static async Task<string> BuilderPlanToolAsync()
    {
        var session = BuilderMode.Current;
        if (session is null)
            return "Det finns ingen aktiv builder-session. Starta med: /bygg <beskrivning>";

        if (PendingApprovalStoreV1.HasPending)
            return "Det finns redan en pending åtgärd. Godkänn eller avbryt den innan BuilderMode skapar en plan-preview.";

        var planResult = await AskOllamaRawModelAsync(
            ModelCatalog.Smart.Name,
            "Du är Jarvis BuilderMode. Skapa en konkret svensk implementationplan i markdown. Skriv bara planens innehåll.",
            BuilderMode.BuildPlanPrompt(session)
        );

        if (!planResult.Ok)
            return planResult.Text;

        var planContent = BuilderMode.BuildPlanMarkdown(session, planResult.Text);
        var relativePath = BuilderMode.PlanRelativePath(session);
        var pendingReply = CreateProjectFileRequestTool(relativePath, planContent);

        return
            "BuilderMode har skapat en pending PLAN.md-preview. Ingen fil har skrivits än.\n\n" +
            pendingReply;
    }

    private static string BuilderCancelTool()
    {
        if (!BuilderMode.HasActiveSession)
            return "Det finns ingen aktiv builder-session att avbryta.";

        BuilderMode.Clear();
        return "BuilderMode avbröts. Ingen fil skrevs.";
    }

    private static async Task<string> NaturalEditRequestToolAsync(string relativePath, string instruction)
    {
        relativePath = (relativePath ?? "").Trim().Trim('"').Replace("\\", "/");
        instruction = (instruction ?? "").Trim();

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Skriv: /edit app/Program.cs = beskriv ändringen";

        if (string.IsNullOrWhiteSpace(instruction))
            return "Ändringsinstruktion saknas. Exempel: /edit docs/test.md = lägg till en tydlig rubrik";

        var extension = Path.GetExtension(relativePath);
        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: NaturalEditTool får bara föreslå ändringar i säkra textfiler: .md, .txt, .json, .cs, .html, .css, .js, .ps1";

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        if (PendingApprovalStoreV1.HasPending)
            return "Det finns redan en pending åtgärd. Godkänn eller avbryt den innan NaturalEditTool skapar en ny edit-preview.";

        string currentContent;
        try
        {
            currentContent = File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return "Kunde inte läsa filen för NaturalEditTool: " + ex.Message;
        }

        if (currentContent.Length > NaturalEditTool.MaxEditableChars)
        {
            return
                "Blockerat: filen är för stor för NaturalEditTool första passet (" + currentContent.Length + " tecken).\n" +
                "Max just nu: " + NaturalEditTool.MaxEditableChars + " tecken. Använd ett mindre, mer specifikt edit-kommando eller filpanelen.";
        }

        var systemPrompt =
            "Du är Jarvis NaturalEditTool. Du får en fil och en ändringsinstruktion.\n" +
            "Returnera ENDAST det fullständiga nya filinnehållet.\n" +
            "Ingen markdown-förklaring, inga kommentarer utanför filinnehållet, ingen diff.";
        var userPrompt = NaturalEditTool.BuildPrompt(relativePath, currentContent, instruction);
        var generated = await AskOllamaRawModelAsync(ModelCatalog.Coder.Name, systemPrompt, userPrompt);

        if (!generated.Ok)
            return generated.Text;

        var proposedContent = NaturalEditTool.CleanFullFileContent(generated.Text);
        if (string.IsNullOrWhiteSpace(proposedContent))
            return "NaturalEditTool fick tomt svar från modellen. Ingen pending ändring skapades.";

        if (string.Equals(currentContent.TrimEnd(), proposedContent.TrimEnd(), StringComparison.Ordinal))
            return "NaturalEditTool föreslog ingen faktisk ändring. Ingen pending ändring skapades.";

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileWrite,
            Target = relativePath,
            Content = proposedContent,
            RequiresUserApproval = true
        });

        return
            "NaturalEditTool skapade en pending edit-preview. Inget har skrivits till disk ännu.\n" +
            "Godkänn eller avbryt i popupen.\n\n" +
            "Fil: " + relativePath + "\n" +
            "Full sökväg: " + fullPath + "\n" +
            "Modell: " + ModelCatalog.Coder.Name + "\n\n" +
            "Instruktion:\n" + instruction + "\n\n" +
            "Preview av nytt innehåll:\n" + BuildPendingApprovalPreviewV1(proposedContent);
    }

    private static string WriteProjectFileTool(string input, bool append)
    {
        var raw = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(raw))
            return append
                ? "Skriv så här: lägg till fil: docs/test.md = text"
                : "Skriv så här: skriv fil: docs/test.md = text";

        var parts = CommandRouterV1.SplitFileCommandArguments(raw, 2);

        if (parts.Length < 2)
            return "Du måste använda = mellan filnamn och text. Exempel: skriv fil: docs/test.md = hej";

        var relativePath = parts[0].Trim();
        var text = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Filnamn saknas.";

        if (string.IsNullOrWhiteSpace(text))
            return "Text saknas.";

        // HARD_WRITE_EXTENSION_GUARD: even if read tools allow more filetypes, writing is limited.
        var writeExtension = Path.GetExtension(relativePath);

        if (!IsWritableProjectFileExtension(writeExtension))
        {
            return "Blockerat: Jarvis får bara skriva dessa filtyper: .md, .txt, .json, .cs, .html, .css, .js, .ps1. Denna filtyp är read-only: " + writeExtension;
        }

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
        {
            if (error.StartsWith("Filen finns inte:", StringComparison.OrdinalIgnoreCase))
            {
                return
                    error + "\n" +
                    "Jag skapar inte nya filer via skriv/lägg-till ännu. Använd en befintlig fil, eller vänta på en separat säker skapa-funktion: /fil skapa.";
            }

            return error;
        }

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = append ? PendingApprovalTypeV1.FileAppend : PendingApprovalTypeV1.FileWrite,
            Target = relativePath.Replace("\\", "/"),
            Content = text,
            RequiresUserApproval = true
        });

        var preview = BuildPendingApprovalPreviewV1(text);

        return
            "Pending filskrivning skapad. Inget har skrivits till disk ännu.\n" +
            "Godkänn eller avbryt i popupen, eller använd textkommandona nedan.\n\n" +
            "Läge: " + (append ? "lägg till" : "skriv/ersätt") + "\n" +
            "Fil: " + relativePath.Replace("\\", "/") + "\n" +
            "Full sökväg: " + fullPath + "\n\n" +
            "Preview:\n" + preview + "\n\n" +
            "Godkänn med: godkänn filskrivning\n" +
            "Avbryt med: avbryt filskrivning";
    }

    private static string CreateProjectFileRequestTool(string input)
    {
        var raw = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(raw))
            return "Skriv så här: skapa fil: docs/test.md = text";

        var parts = CommandRouterV1.SplitFileCommandArguments(raw, 2);

        if (parts.Length < 2)
            return "Du måste använda = mellan filnamn och text. Exempel: skapa fil: docs/test.md = hej";

        return CreateProjectFileRequestTool(parts[0].Trim(), parts[1].Trim());
    }

    private static string CreateProjectFileRequestTool(string relativePath, string text)
    {
        relativePath = (relativePath ?? "").Trim().Trim('"').Replace("\\", "/");

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Filnamn saknas.";

        if (string.IsNullOrWhiteSpace(text))
            return "Text saknas.";

        var extension = Path.GetExtension(relativePath);
        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: Jarvis får bara skapa dessa filtyper: .md, .txt, .json, .cs, .html, .css, .js, .ps1. Denna filtyp är read-only: " + extension;

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: false, out var fullPath, out var error);
        if (!safe)
            return error;

        if (File.Exists(fullPath))
            return "Filen finns redan: " + fullPath + "\nAnvänd skriv/ersätt eller filpanelens Spara med godkännande om du vill ändra den.";

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileCreate,
            Target = relativePath,
            Content = text,
            RequiresUserApproval = true
        });

        return
            "Pending filskapande skapad. Inget har skrivits till disk ännu.\n" +
            "Godkänn eller avbryt i popupen, eller använd textkommandona nedan.\n\n" +
            "Läge: skapa ny fil\n" +
            "Fil: " + relativePath + "\n" +
            "Full sökväg: " + fullPath + "\n\n" +
            "Preview:\n" + BuildPendingApprovalPreviewV1(text) + "\n\n" +
            "Godkänn med: godkänn filskrivning\n" +
            "Avbryt med: avbryt filskrivning";
    }

    private static string EditorSavePendingTool(string relativePath, string content)
    {
        relativePath = (relativePath ?? "").Trim().Trim('"').Replace("\\", "/");

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Ingen aktiv fil att spara.";

        var extension = Path.GetExtension(relativePath);
        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: filpanelen får bara spara säkra textfiler: " + extension;

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileWrite,
            Target = relativePath,
            Content = content ?? "",
            RequiresUserApproval = true
        });

        return
            "Pending sparning från filpanelen skapad. Inget har skrivits till disk ännu.\n" +
            "Godkänn eller avbryt i popupen, eller använd textkommandona nedan.\n\n" +
            "Läge: skriv/ersätt från filpanelen\n" +
            "Fil: " + relativePath + "\n" +
            "Full sökväg: " + fullPath + "\n\n" +
            "Preview:\n" + BuildPendingApprovalPreviewV1(content ?? "") + "\n\n" +
            "Godkänn med: godkänn filskrivning\n" +
            "Avbryt med: avbryt filskrivning";
    }

    private static bool TryExtractDeleteProjectFileRequest(string input, string command, out string relativePath, out string error)
    {
        relativePath = "";
        error = "";

        if (CommandStartsWith(command, "radera fil", "ta bort fil", "delete file"))
        {
            var skipWords = command.StartsWith("ta bort fil", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
            relativePath = CommandRouterV1.SplitFileCommandArguments(ExtractValue(input, skipWords), 2)[0].Trim();

            if (string.IsNullOrWhiteSpace(relativePath))
                error = "Skriv så här: radera fil: docs/test.md";

            return true;
        }

        var deleteIntent =
            command.StartsWith("radera ", StringComparison.OrdinalIgnoreCase) ||
            command.StartsWith("ta bort ", StringComparison.OrdinalIgnoreCase) ||
            command.StartsWith("delete ", StringComparison.OrdinalIgnoreCase);

        if (!deleteIntent)
            return false;

        var fileMatch = Regex.Match(
            input,
            @"(?i)\b([a-z0-9_\-\.]+(?:[\\/][a-z0-9_\-\.]+)*\.(md|txt|json|cs|html|css|js|ps1))\b"
        );

        if (!fileMatch.Success)
            return false;

        relativePath = fileMatch.Groups[1].Value.Replace("\\", "/").Trim();
        return true;
    }

    private static string DeleteProjectFileRequestTool(string relativePath, string validationError)
    {
        if (!string.IsNullOrWhiteSpace(validationError))
            return validationError;

        relativePath = CommandRouterV1.SplitFileCommandArguments(relativePath ?? "", 2)[0].Trim().Trim('"').Replace("\\", "/");

        if (string.IsNullOrWhiteSpace(relativePath))
            return "Skriv så här: radera fil: docs/test.md";

        var extension = Path.GetExtension(relativePath);
        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: filradering är bara tillåten för säkra textfiler: " + extension;

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        string preview;
        try
        {
            preview = File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            return "Kunde inte skapa raderingspreview: " + ex.Message;
        }

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileDelete,
            Target = relativePath,
            Content = preview,
            RequiresUserApproval = true
        });

        return
            "Pending filradering skapad. Inget har raderats ännu.\n" +
            "Godkänn eller avbryt i popupen, eller använd textkommandona nedan.\n\n" +
            "Fil: " + relativePath + "\n" +
            "Full sökväg: " + fullPath + "\n\n" +
            "Preview av filen som raderas:\n" + BuildPendingApprovalPreviewV1(preview) + "\n\n" +
            "Godkänn med: godkänn filradering\n" +
            "Avbryt med: avbryt filradering";
    }

    private static bool IsWritableProjectFileExtension(string extension)
    {
        var writableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1"
        };

        return writableExtensions.Contains(extension);
    }

    private static bool AllowedExtensionForFileTool(string extension, bool writeMode, out string error)
    {
        error = "";

        var readableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1", ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props", ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs",
            ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props",
            ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv",
            ".vbs"
        };

        var writableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1"
        };

        if (writeMode)
        {
            if (!writableExtensions.Contains(extension))
            {
                error =
                    "Blockerat: Jarvis får bara skriva dessa filtyper: " +
                    ".md, .txt, .json, .cs, .html, .css, .js, .ps1";
                return false;
            }

            return true;
        }

        if (!readableExtensions.Contains(extension))
        {
            error =
                "Blockerat: Jarvis får bara läsa säkra projektfiler, till exempel: " +
                ".md, .txt, .json, .cs, .html, .css, .js, .ps1, .csproj, .sln, .xml, .yml, .yaml, .config, .log, .csv";
            return false;
        }

        return true;
    }
    private static bool TryGetSafeProjectFilePath(string relativePath, bool mustBeExisting, out string fullPath, out string error)
    {
        fullPath = "";
        error = "";

        relativePath = relativePath.Trim().Trim('"');

        if (Path.IsPathRooted(relativePath))
        {
            error = "Blockerat: använd relativ sökväg inom F:\\Jarvis-clean, inte full sökväg.";
            return false;
        }

        if (relativePath.Contains(".."))
        {
            error = "Blockerat: sökvägen får inte innehålla ..";
            return false;
        }

        var blockedParts = new[]
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints", ".tmp"
        };

        var normalizedRelative = relativePath.Replace('\\', '/').ToLowerInvariant();
        var pathParts = normalizedRelative.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (blockedParts.Any(part => pathParts.Contains(part)))
        {
            error = "Blockerat: Jarvis får inte läsa/skriva i bin, obj, dist, .git eller node_modules.";
            return false;
        }

        var extension = Path.GetExtension(relativePath);
        var writeMode = !mustBeExisting;

        if (!AllowedExtensionForFileTool(extension, writeMode, out var extensionError))
        {
            error = extensionError;
            return false;
        }

        fullPath = Path.GetFullPath(Path.Combine(ProjectRoot, relativePath));
        var fullRoot = Path.GetFullPath(ProjectRoot);

        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            error = "Blockerat: filen måste ligga i F:\\Jarvis-clean.";
            return false;
        }

        if (mustBeExisting && !File.Exists(fullPath))
        {
            error = "Filen finns inte: " + fullPath;
            return false;
        }

        return true;
    }

    private static void InvalidateVaultIndexIfTargetIsVaultPathV1(string relativePath)
    {
        var normalized = (relativePath ?? "").Replace("\\", "/").TrimStart('/');
        if (normalized.StartsWith("vault/", StringComparison.OrdinalIgnoreCase))
            VaultSearcher.InvalidateIndex();
    }

    private static string ApprovePendingFileWriteTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null ||
            pending.Type is not PendingApprovalTypeV1.FileCreate and not PendingApprovalTypeV1.FileWrite and not PendingApprovalTypeV1.FileAppend)
        {
            return "Det finns ingen pending filskrivning. Skapa först en preview med: skriv fil: docs/test.md = text";
        }

        var extension = Path.GetExtension(pending.Target);
        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: pending filskrivning har otillåten filtyp: " + extension;

        var mustBeExisting = pending.Type != PendingApprovalTypeV1.FileCreate;
        var safe = TryGetSafeProjectFilePath(pending.Target, mustBeExisting: mustBeExisting, out var fullPath, out var error);
        if (!safe)
            return error;

        try
        {
            if (pending.Type == PendingApprovalTypeV1.FileCreate && File.Exists(fullPath))
            {
                PendingApprovalStoreV1.Clear();
                return "Filen finns redan och skapades därför inte: " + fullPath;
            }

            var beforeContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "";
            LatestFileUndoV1 = pending.Type == PendingApprovalTypeV1.FileCreate
                ? new FileUndoSnapshotV1(
                    Path: pending.Target.Replace("\\", "/"),
                    BeforeContent: "",
                    ExistedBefore: false,
                    Description: "Ångra senaste filskapande"
                )
                : new FileUndoSnapshotV1(
                    Path: pending.Target.Replace("\\", "/"),
                    BeforeContent: beforeContent,
                    ExistedBefore: true,
                    Description: pending.Type == PendingApprovalTypeV1.FileAppend ? "Ångra senaste filappend" : "Ångra senaste filskrivning"
                );

            if (pending.Type == PendingApprovalTypeV1.FileAppend)
            {
                File.AppendAllText(fullPath, Environment.NewLine + pending.Content + Environment.NewLine);
            }
            else
            {
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(fullPath, pending.Content + Environment.NewLine);
            }

            var afterContent = File.ReadAllText(fullPath);
            var changeKind = pending.Type == PendingApprovalTypeV1.FileCreate ? "create" : pending.Type == PendingApprovalTypeV1.FileAppend ? "append" : "write";
            LatestFileChangeReviewV1 = BuildFileChangeReviewV1(pending.Target, beforeContent, afterContent, changeKind);

            InvalidateVaultIndexIfTargetIsVaultPathV1(pending.Target);
            SetActiveFile(pending.Target);
            PendingApprovalStoreV1.Clear();

            return pending.Type switch
            {
                PendingApprovalTypeV1.FileCreate => "Godkänt. Fil skapad: " + fullPath,
                PendingApprovalTypeV1.FileAppend => "Godkänt. Text tillagd i fil: " + fullPath,
                _ => "Godkänt. Fil skriven: " + fullPath
            };
        }
        catch (Exception ex)
        {
            return "Kunde inte slutföra filskrivningen: " + ex.Message;
        }
    }

    private static string CancelPendingFileWriteTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null ||
            pending.Type is not PendingApprovalTypeV1.FileCreate and not PendingApprovalTypeV1.FileWrite and not PendingApprovalTypeV1.FileAppend)
        {
            return "Det finns ingen pending filskrivning att avbryta.";
        }

        PendingApprovalStoreV1.Clear();
        return "Pending filskrivning avbröts. Inget skrevs till disk.";
    }

    private static string ApprovePendingFileDeleteTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.FileDelete)
            return "Det finns ingen pending filradering. Skapa först en preview med: radera fil: docs/test.md";

        var extension = Path.GetExtension(pending.Target);
        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: pending filradering har otillåten filtyp: " + extension;

        var safe = TryGetSafeProjectFilePath(pending.Target, mustBeExisting: true, out var fullPath, out var error);
        if (!safe)
            return error;

        try
        {
            var beforeContent = File.ReadAllText(fullPath);
            LatestFileUndoV1 = new FileUndoSnapshotV1(
                pending.Target.Replace("\\", "/"),
                beforeContent,
                true,
                "Ångra senaste filradering"
            );
            File.Delete(fullPath);
            LatestFileChangeReviewV1 = BuildFileChangeReviewV1(pending.Target, beforeContent, "", "delete");
            InvalidateVaultIndexIfTargetIsVaultPathV1(pending.Target);
            PendingApprovalStoreV1.Clear();

            return "Godkänt. Fil raderad: " + fullPath;
        }
        catch (Exception ex)
        {
            return "Kunde inte slutföra filraderingen: " + ex.Message;
        }
    }

    private static string CancelPendingFileDeleteTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.FileDelete)
            return "Det finns ingen pending filradering att avbryta.";

        PendingApprovalStoreV1.Clear();
        return "Pending filradering avbröts. Inget raderades från disk.";
    }

    private static string UndoLastFileChangeRequestTool()
    {
        if (LatestFileUndoV1 is null)
            return "Det finns ingen filändring att ångra ännu.";

        var snapshot = LatestFileUndoV1;
        var extension = Path.GetExtension(snapshot.Path);

        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: undo stöder bara säkra textfiler: " + extension;

        var safe = TryGetSafeProjectFilePath(snapshot.Path, mustBeExisting: false, out var fullPath, out var error);
        if (!safe)
            return error;

        if (PendingApprovalStoreV1.HasPending)
            return "Det finns redan en pending åtgärd. Godkänn eller avbryt den innan du skapar en undo-preview.";

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.FileUndo,
            Target = snapshot.Path,
            Content = snapshot.ExistedBefore
                ? snapshot.BeforeContent
                : "Undo skulle radera filen eftersom den inte fanns innan ändringen.",
            RequiresUserApproval = true
        });

        return
            "Pending undo skapad. Inget har ändrats ännu.\n" +
            "Godkänn eller avbryt i popupen.\n\n" +
            "Åtgärd: " + snapshot.Description + "\n" +
            "Fil: " + snapshot.Path + "\n" +
            "Full sökväg: " + fullPath + "\n\n" +
            "Preview av innehållet efter undo:\n" + BuildPendingApprovalPreviewV1(snapshot.BeforeContent);
    }

    private static string ApprovePendingFileUndoTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.FileUndo)
            return "Det finns ingen pending undo. Klicka Ångra efter en godkänd filändring först.";

        if (LatestFileUndoV1 is null ||
            !string.Equals(LatestFileUndoV1.Path, pending.Target, StringComparison.OrdinalIgnoreCase))
        {
            PendingApprovalStoreV1.Clear();
            return "Undo-snapshot saknas eller matchar inte längre. Ingen fil ändrades.";
        }

        var snapshot = LatestFileUndoV1;
        var extension = Path.GetExtension(snapshot.Path);

        if (!IsWritableProjectFileExtension(extension))
            return "Blockerat: pending undo har otillåten filtyp: " + extension;

        var safe = TryGetSafeProjectFilePath(snapshot.Path, mustBeExisting: false, out var fullPath, out var error);
        if (!safe)
            return error;

        try
        {
            var currentContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "";

            if (snapshot.ExistedBefore)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, snapshot.BeforeContent);
                LatestFileChangeReviewV1 = BuildFileChangeReviewV1(snapshot.Path, currentContent, snapshot.BeforeContent, "undo");
                InvalidateVaultIndexIfTargetIsVaultPathV1(snapshot.Path);
                SetActiveFile(snapshot.Path);
            }
            else
            {
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                LatestFileChangeReviewV1 = BuildFileChangeReviewV1(snapshot.Path, currentContent, "", "undo");
                InvalidateVaultIndexIfTargetIsVaultPathV1(snapshot.Path);
            }

            LatestFileUndoV1 = null;
            PendingApprovalStoreV1.Clear();

            return "Godkänt. Senaste filändringen ångrades: " + fullPath;
        }
        catch (Exception ex)
        {
            return "Kunde inte ångra filändringen: " + ex.Message;
        }
    }

    private static string CancelPendingFileUndoTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.FileUndo)
            return "Det finns ingen pending undo att avbryta.";

        PendingApprovalStoreV1.Clear();
        return "Pending undo avbröts. Inget ändrades på disk.";
    }

    private static string OpenFolderTool(string folder)
    {
        var fullTarget = Path.GetFullPath(folder);
        var fullRoot = Path.GetFullPath(ProjectRoot);

        if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return "Blockerat: Jarvis får bara öppna mappar i F:\\Jarvis-clean.";

        if (!Directory.Exists(fullTarget))
            return "Mappen finns inte: " + fullTarget;

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = fullTarget,
            UseShellExecute = true
        });

        return "Öppnar mapp: " + fullTarget;
    }

    private static string TerminalPreviewTool(string input)
    {
        var commandText = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(commandText))
            return "Skriv så här: terminal preview: dotnet build";

        var allowed = TryBuildSafeTerminalCommand(commandText.Trim(), out var command, out var workingDir, out var error);

        if (!allowed)
            return error;

        if (PendingApprovalStoreV1.HasPending)
            return "Det finns redan en pending åtgärd. Godkänn eller avbryt den innan du skapar en terminal-preview.";

        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.TerminalRun,
            Target = workingDir,
            Content = command,
            RequiresUserApproval = true
        });

        return
            "Pending terminalkörning skapad. Inget har körts ännu.\n" +
            "Godkänn eller avbryt i popupen, eller använd textkommandona nedan.\n\n" +
            "Working directory:\n" +
            workingDir + "\n\n" +
            "Kommando:\n" +
            command + "\n\n" +
            "Godkänn med: bekräfta kör\n" +
            "Avbryt med: avbryt kör";
    }

    private static string TerminalConfirmRun()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.TerminalRun)
            return "Det finns inget terminalkommando att köra. Skriv först: terminal preview: dotnet build";

        return ApprovePendingTerminalRunTool();
    }

    private static string TerminalCancelRun()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.TerminalRun)
            return "Det finns inget pending terminalkommando att avbryta.";

        return CancelPendingTerminalRunTool();
    }

    private static string ApprovePendingTerminalRunTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.TerminalRun)
            return "Det finns inget pending terminalkommando att köra. Skriv först: terminal preview: dotnet build";

        var command = pending.Content.Trim();
        var workingDir = pending.Target.Trim();

        if (string.IsNullOrWhiteSpace(command))
            return "Pending terminalkommando saknar kommando.";

        if (string.IsNullOrWhiteSpace(workingDir))
            return "Pending terminalkommando saknar arbetsmapp.";

        var fullWorkingDir = Path.GetFullPath(workingDir);
        var fullRoot = Path.GetFullPath(ProjectRoot);

        var relativeToRoot = Path.GetRelativePath(fullRoot, fullWorkingDir);
        if (relativeToRoot.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativeToRoot))
            return "Blockerat: terminalkommandon får bara köras inom F:\\Jarvis-clean.";

        if (!Directory.Exists(fullWorkingDir))
            return "Terminalarbetsmappen finns inte: " + fullWorkingDir;

        PendingApprovalStoreV1.Clear();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command " + QuotePowerShellCommand(command),
                WorkingDirectory = fullWorkingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process is null)
                return "Kunde inte starta PowerShell-processen.";

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    outputBuilder.AppendLine(args.Data);
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    errorBuilder.AppendLine(args.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(TerminalRunTimeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { }

                var timedOutOutput = outputBuilder.ToString();
                var timedOutError = errorBuilder.ToString();
                var timedOutSummary = BuildTerminalRunSummary(-1, timedOutOutput, timedOutError);
                StoreLatestTerminalPanelV1(command, fullWorkingDir, -1, timedOut: true, timedOutSummary, timedOutOutput, timedOutError);

                return
                    "Terminal stoppad efter 120 sekunder.\n\n" +
                    "Kommando:\n" + command + "\n\n" +
                    "Sammanfattning:\n" + timedOutSummary + "\n\n" +
                    "Terminalpanelen har uppdaterats med outputen som hann komma.";
            }

            process.WaitForExit();

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var summary = BuildTerminalRunSummary(process.ExitCode, output, error);
            StoreLatestTerminalPanelV1(command, fullWorkingDir, process.ExitCode, timedOut: false, summary, output, error);

            return BuildTerminalRunResult(command, process.ExitCode, summary);
        }
        catch (Exception ex)
        {
            return "Terminal-fel: " + ex.Message;
        }
    }

    private static string CancelPendingTerminalRunTool()
    {
        var pending = PendingApprovalStoreV1.Get();

        if (pending is null || pending.Type != PendingApprovalTypeV1.TerminalRun)
            return "Det finns inget pending terminalkommando att avbryta.";

        PendingApprovalStoreV1.Clear();
        return "Pending terminalkörning avbröts. Inget kördes.";
    }

    private static bool TryBuildSafeTerminalCommand(string requested, out string command, out string workingDir, out string error)
    {
        command = "";
        workingDir = Path.Combine(ProjectRoot, "app");
        error = "";

        var normalized = NormalizeCommand(requested);

        if (normalized is "dotnet build" or "build")
        {
            command = "dotnet build";
            return true;
        }

        if (normalized is "dotnet publish" or "publish")
        {
            command = @"dotnet publish ""F:\Jarvis-clean\app\JarvisClean.csproj"" -c Release -o ""F:\Jarvis-clean\dist"" --no-self-contained";
            return true;
        }

        if (normalized is "dotnet test" or "test")
        {
            command = "dotnet test";
            return true;
        }

        if (normalized is "ollama list" or "lista modeller")
        {
            workingDir = ProjectRoot;
            command = "ollama list";
            return true;
        }

        if (normalized is "git status")
        {
            workingDir = ProjectRoot;
            command = "git status";
            return true;
        }

        if (normalized is "get current state" or "visa current state")
        {
            workingDir = ProjectRoot;
            command = @"Get-Content ""F:\Jarvis-clean\CURRENT_STATE.md"" | Select-Object -Last 80";
            return true;
        }

        error =
            "Blockerat: detta terminalkommando är inte tillåtet ännu.\n\n" +
            "Tillåtna kommandon just nu:\n" +
            "- terminal preview: dotnet build\n" +
            "- terminal preview: dotnet publish\n" +
            "- terminal preview: dotnet test\n" +
            "- terminal preview: ollama list\n" +
            "- terminal preview: git status\n" +
            "- terminal preview: get current state";

        return false;
    }

    private static string QuotePowerShellCommand(string command)
    {
        return "\"" + command.Replace("\"", "\\\"") + "\"";
    }

    private static void StoreLatestTerminalPanelV1(string command, string workingDirectory, int exitCode, bool timedOut, string summary, string output, string error)
    {
        LatestTerminalPanelV1 = new TerminalPanelV1(
            command,
            workingDirectory,
            exitCode,
            timedOut,
            summary,
            output,
            error
        );

        LatestTerminalPanelVersionV1++;

        // Plocka ut byggrelaterade kommandon (dotnet build/publish/test) för Översikt-cellen "Senaste bygge".
        // Övriga kommandon (git status, ollama list, get current state) skriver inte build-status.
        if (IsDotnetBuildLikeCommandV1(command))
        {
            LatestBuildStatusV1 = new BuildStatusV1(command, exitCode, timedOut, DateTime.Now);
        }
    }

    private static bool IsDotnetBuildLikeCommandV1(string command)
    {
        var trimmed = (command ?? "").Trim();
        if (trimmed.Length == 0) return false;
        var lower = trimmed.ToLowerInvariant();
        return lower.StartsWith("dotnet build") ||
               lower.StartsWith("dotnet publish") ||
               lower.StartsWith("dotnet test") ||
               lower.StartsWith("dotnet run");
    }

    // Anropas efter varje skrivning till data\memory.md så Översikt visar färsk minnesstatus.
    private static void RecordMemoryChangeV1(string operation, string preview)
    {
        var clean = (preview ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        if (clean.Length > 80) clean = clean.Substring(0, 80) + "...";
        LatestMemoryChangeV1 = new MemoryChangeV1(operation ?? "skriv", clean, DateTime.Now);
    }

    private static string BuildTerminalRunResult(string command, int exitCode, string summary)
    {
        return
            "Terminal klart: exit code " + exitCode + "\n\n" +
            "Kommando:\n" + command + "\n\n" +
            "Sammanfattning:\n" + summary + "\n\n" +
            "Terminalpanelen har uppdaterats med full output.";
    }

    private static string BuildTerminalRunSummary(int exitCode, string output, string error)
    {
        var combined = (output ?? "") + "\n" + (error ?? "");
        var warningCodes = Regex.Matches(combined, @"\bwarning\s+([A-Z]+\d+)", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value.ToUpperInvariant())
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        var errorCodes = Regex.Matches(combined, @"\berror\s+([A-Z]+\d+)", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value.ToUpperInvariant())
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        var summary = new StringBuilder();

        if (combined.Contains("Build succeeded.", StringComparison.OrdinalIgnoreCase))
            summary.AppendLine("- Build succeeded.");
        else if (combined.Contains("Build FAILED.", StringComparison.OrdinalIgnoreCase))
            summary.AppendLine("- Build FAILED.");
        else if (exitCode == 0)
            summary.AppendLine("- Kommandot kördes klart.");
        else if (exitCode < 0)
            summary.AppendLine("- Kommandot stoppades innan det blev klart.");
        else
            summary.AppendLine("- Kommandot avslutades med felkod.");

        summary.AppendLine("- Varningar: " + (warningCodes.Count == 0 ? "0" : string.Join(", ", warningCodes)));
        summary.AppendLine("- Felkoder: " + (errorCodes.Count == 0 ? "0" : string.Join(", ", errorCodes)));
        summary.AppendLine("- Stderr: " + (string.IsNullOrWhiteSpace(error) ? "tomt" : "har innehåll"));

        return summary.ToString().TrimEnd();
    }

    private static string ShowLatestTerminalTranscriptTool()
    {
        if (LatestTerminalPanelV1 is null)
            return "Det finns ingen terminaloutput ännu. Kör först till exempel: terminal preview: dotnet build";

        var transcript = new StringBuilder();
        transcript.AppendLine("Senaste terminalkörning:");
        transcript.AppendLine();
        transcript.AppendLine("Kommando:");
        transcript.AppendLine(LatestTerminalPanelV1.Command);
        transcript.AppendLine();
        transcript.AppendLine("Working directory:");
        transcript.AppendLine(LatestTerminalPanelV1.WorkingDirectory);
        transcript.AppendLine();
        transcript.AppendLine("Exit code: " + LatestTerminalPanelV1.ExitCode);
        transcript.AppendLine("Timeout: " + (LatestTerminalPanelV1.TimedOut ? "ja" : "nej"));
        transcript.AppendLine();
        transcript.AppendLine("Sammanfattning:");
        transcript.AppendLine(LatestTerminalPanelV1.Summary);
        transcript.AppendLine();
        transcript.AppendLine("Output:");
        transcript.AppendLine("Full output finns i Terminal-panelen. Använd knappen Terminal för att visa, kopiera eller rensa den.");

        return transcript.ToString().TrimEnd();
    }

    private static string TrimTerminalOutput(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(tomt)";

        value = value.Trim();

        if (value.Length > TerminalOutputPreviewMaxLength)
        {
            var headLength = Math.Min(TerminalOutputPreviewHeadLength, value.Length);
            var tailStart = Math.Max(headLength, value.Length - TerminalOutputPreviewTailLength);

            return value[..headLength] +
                "\n\n...output kortad (" + value.Length + " tecken totalt)...\n\n" +
                value[tailStart..];
        }

        return value;
    }
    private static string ListFilesTool(string command)
    {
        string target = ProjectRoot;

        if (command.Contains("app"))
            target = Path.Combine(ProjectRoot, "app");
        else if (command.Contains("dashboard"))
            target = Path.Combine(ProjectRoot, "dashboard");
        else if (command.Contains("tests"))
            target = Path.Combine(ProjectRoot, "tests");
        else if (command.Contains("docs"))
            target = Path.Combine(ProjectRoot, "docs");
        else if (command.Contains("data"))
            target = Path.Combine(ProjectRoot, "data");

        var fullTarget = Path.GetFullPath(target);
        var fullRoot = Path.GetFullPath(ProjectRoot);

        if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return "Blockerat: Jarvis får bara lista filer i F:\\Jarvis-clean.";

        if (!Directory.Exists(fullTarget))
            return "Mappen finns inte: " + fullTarget;

        var dirs = Directory.GetDirectories(fullTarget)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => "[MAPP] " + x);

        var files = Directory.GetFiles(fullTarget)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => "[FIL] " + x);

        var items = dirs.Concat(files).Take(80).ToList();

        if (items.Count == 0)
            return "Inga filer hittades i: " + fullTarget;

        return "Filer i " + fullTarget + ":\n" + string.Join("\n", items);
    }

    private static bool TryCalculate(string input, out string result)
    {
        result = "";

        var compact = NormalizeCommand(input)
            .Replace(" ", "")
            .Replace("rakna", "")
            .Replace("calc", "");

        var match = Regex.Match(compact, @"^(-?\d+(?:[,.]\d+)?)([+\-*/])(-?\d+(?:[,.]\d+)?)$");

        if (!match.Success)
            return false;

        var a = double.Parse(match.Groups[1].Value.Replace(",", "."), CultureInfo.InvariantCulture);
        var op = match.Groups[2].Value;
        var b = double.Parse(match.Groups[3].Value.Replace(",", "."), CultureInfo.InvariantCulture);

        double value = op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" when b != 0 => a / b,
            "/" => double.NaN,
            _ => double.NaN
        };

        result = double.IsNaN(value)
            ? "Kan inte dela med 0."
            : value.ToString(CultureInfo.InvariantCulture);

        return true;
    }

    private static bool TryAnswerFromMemoryQuestion(string originalText, string command, out string answer)
    {
        answer = "";

        if (command.Contains("favorit farg") || command.Contains("favoritfarg"))
        {
            var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

            if (!File.Exists(memoryPath))
            {
                answer = "Jag hittar ingen aktiv favoritfärg i memory.md.";
                return true;
            }

            var lines = File.ReadAllLines(memoryPath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.StartsWith("#"))
                .Where(line =>
                {
                    var normalized = NormalizeCommand(line);
                    return normalized.Contains("favorit farg") || normalized.Contains("favoritfarg");
                })
                .ToList();

            if (lines.Count == 0)
            {
                answer = "Jag hittar ingen aktiv favoritfärg i memory.md.";
                return true;
            }

            answer = "I aktivt minne står det: " + lines.Last();
            return true;
        }

        if (command.StartsWith("vad kommer du ihag om") || command.StartsWith("vad vet du om"))
        {
            var query = ExtractQueryAfterOm(originalText);

            if (string.IsNullOrWhiteSpace(query))
            {
                answer = MemoryStatus() + Environment.NewLine + Environment.NewLine + ShowMemory();
                return true;
            }

            answer = SearchMemory("sök minne: " + query);
            return true;
        }

        if (CommandIs(command, "vad kommer du ihag", "vad minns du"))
        {
            answer = MemoryStatus() + Environment.NewLine + Environment.NewLine + ShowMemory();
            return true;
        }

        return false;
    }

    private static string ExtractQueryAfterOm(string text)
    {
        var match = Regex.Match(text, @"\bom\b\s*(.+)$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }

    private static string ShowArchive()
    {
        var archivePath = Path.Combine(ProjectRoot, "data", "memory_archive.md");

        if (!File.Exists(archivePath))
            return "Det finns inget minnesarkiv ännu.";

        var content = File.ReadAllText(archivePath).Trim();

        if (string.IsNullOrWhiteSpace(content))
            return "Minnesarkivet är tomt.";

        var maxLength = 3000;
        if (content.Length > maxLength)
            content = content[^maxLength..];

        return "Arkiverade minnen från memory_archive.md:\n" + content;
    }

    private static string SearchArchive(string input)
    {
        var query = ExtractValue(input, 2);

        if (string.IsNullOrWhiteSpace(query))
            return "Skriv så här: sök arkiv: röd";

        var archivePath = Path.Combine(ProjectRoot, "data", "memory_archive.md");

        if (!File.Exists(archivePath))
            return "Det finns inget minnesarkiv ännu.";

        var content = File.ReadAllText(archivePath);

        if (string.IsNullOrWhiteSpace(content))
            return "Minnesarkivet är tomt.";

        var entries = Regex.Split(content, @"(?m)^## ")
            .Select(entry => entry.Trim())
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Where(entry => entry.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        if (entries.Count == 0)
            return "Inga träffar i memory_archive.md för: " + query;

        return "Träffar i memory_archive.md:\n\n" + string.Join("\n\n---\n\n", entries);
    }
    private static string BuildMemoryContext()
    {
        var memoryPath = Path.Combine(ProjectRoot, "data", "memory.md");

        if (!File.Exists(memoryPath))
            return "Inget lokalt minne finns ännu.";

        var content = File.ReadAllText(memoryPath).Trim();

        if (string.IsNullOrWhiteSpace(content))
            return "Inget lokalt minne finns ännu.";

        var maxLength = 2500;
        if (content.Length > maxLength)
            content = content[^maxLength..];

        return content;
    }

    private static string GetActiveOllamaModel()
    {
        var configDir = Path.Combine(ProjectRoot, "config");
        var modelPath = Path.Combine(configDir, "model.txt");

        try
        {
            Directory.CreateDirectory(configDir);

            if (!File.Exists(modelPath))
            {
                File.WriteAllText(modelPath, OllamaModel);
                return OllamaModel;
            }

            var model = File.ReadAllText(modelPath).Trim();

            if (string.IsNullOrWhiteSpace(model))
            {
                File.WriteAllText(modelPath, OllamaModel);
                return OllamaModel;
            }

            return model;
        }
        catch
        {
            return OllamaModel;
        }
    }

    private static string ShowModelTool()
    {
        var model = GetActiveOllamaModel();
        var modelPath = Path.Combine(ProjectRoot, "config", "model.txt");

        return
            "Aktiv Ollama-modell:\n" +
            model + "\n\n" +
            "Config-fil:\n" +
            modelPath + "\n\n" +
            "Byt med exempel:\n" +
            "byt modell: qwen2.5-coder:7b";
    }

    private static string SetModelTool(string input)
    {
        var model = ExtractValue(input, 2).Trim();

        if (string.IsNullOrWhiteSpace(model))
            return "Skriv så här: byt modell: qwen2.5-coder:7b";

        if (model.Contains(" ") || model.Contains("\\") || model.Contains("/") || model.Contains("\""))
            return "Blockerat: modellnamnet ser ogiltigt ut. Använd exakt namn från: lista modeller";

        var configDir = Path.Combine(ProjectRoot, "config");
        Directory.CreateDirectory(configDir);

        var modelPath = Path.Combine(configDir, "model.txt");
        File.WriteAllText(modelPath, model);

        return
            "Modell ändrad till:\n" +
            model + "\n\n" +
            "Starta gärna om Jarvis om ett svar redan håller på att köras.\n" +
            "Kontrollera med: visa modell";
    }

    private static async Task<string> ListOllamaModelsTool()
    {
        try
        {
            using var response = await Http.GetAsync("http://127.0.0.1:11434/api/tags");

            if (!response.IsSuccessStatusCode)
                return "Ollama svarade med felkod: " + (int)response.StatusCode;

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            if (!json.RootElement.TryGetProperty("models", out var models))
                return "Ollama svarade, men Jarvis kunde inte läsa modellistan.";

            var names = models.EnumerateArray()
                .Select(model => model.TryGetProperty("name", out var name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name)
                .ToList();

            if (names.Count == 0)
                return "Inga Ollama-modeller hittades.";

            return
                "Installerade Ollama-modeller:\n" +
                string.Join("\n", names.Select(name => "- " + name)) +
                "\n\nByt modell med:\n" +
                "byt modell: modellnamn";
        }
        catch
        {
            return "Kunde inte ansluta till Ollama. Starta Ollama och försök igen.";
        }
    }
    private static async Task<string> AskOllamaAsync(string text)
    {
        try
        {
            // Vault-kontext: läs topp 5 mest relevanta vault-noter och injicera i system-prompt.
            var vaultContext = VaultSearcher.BuildContextPrefix(text, k: 5);
            _ = _instance?.SendVaultActiveNodesAsync();
            var memoryContext = BuildMemoryContext();

            // ModelRouter: auto-välj modell baserat på query + dialog-djup.
            // Manuellt val (annat än fast) respekteras alltid.
            var pick = ModelRouter.PickModelForQuery(text, ConversationHistory.Count, _activeModel);
            var chosenModel = pick.Model;

            var systemContent =
                "Du är Jarvis. Svara kort, tydligt och på svenska. Du kör lokalt via Ollama.\n\n" +
                (string.IsNullOrEmpty(vaultContext) ? "" : vaultContext + "\n") +
                "Lokalt minne från memory.md:\n" + memoryContext;

            // Multi-turn: bygg messages = system + history + denna user-message
            ConversationHistory.AddUser(text);
            var messages = new List<object> { new { role = "system", content = systemContent } };
            messages.AddRange(ConversationHistory.AsOllamaMessages());

            var payload = new
            {
                model = chosenModel,
                stream = false,
                keep_alive = "10m",
                messages = messages
            };

            using var response = await Http.PostAsJsonAsync(OllamaUrl, payload);

            if (!response.IsSuccessStatusCode)
                return $"Ollama svarade med fel: {(int)response.StatusCode} {response.ReasonPhrase}";

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            if (json.RootElement.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                var reply = content.GetString();
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    var trimmed = reply.Trim();
                    // Spara assistant-svar i historiken för multi-turn-context
                    ConversationHistory.AddAssistant(trimmed);
                    // Prepend modell-badge så användaren ser vilken modell som svarade
                    return ModelRouter.BadgeForModel(chosenModel) + " " + trimmed;
                }
            }

            return "Ollama svarade, men Jarvis kunde inte läsa svaret.";
        }
        catch (TaskCanceledException)
        {
            return "Ollama tog för lång tid. Håll modellen varm med: ollama run qwen2.5-coder:1.5b";
        }
        catch (HttpRequestException)
        {
            return "Kunde inte ansluta till Ollama. Starta Ollama och försök igen.";
        }
        catch (Exception ex)
        {
            return "Ollama-fel: " + ex.Message;
        }
    }

    private static async Task<(bool Ok, string Text)> AskOllamaRawModelAsync(string model, string systemContent, string userContent)
    {
        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemContent },
                new { role = "user", content = userContent }
            };

            var payload = new
            {
                model,
                stream = false,
                keep_alive = "10m",
                messages
            };

            using var response = await Http.PostAsJsonAsync(OllamaUrl, payload);

            if (!response.IsSuccessStatusCode)
                return (false, $"Ollama svarade med fel: {(int)response.StatusCode} {response.ReasonPhrase}");

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            if (json.RootElement.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                var reply = content.GetString();
                if (!string.IsNullOrWhiteSpace(reply))
                    return (true, reply.Trim());
            }

            return (false, "Ollama svarade, men Jarvis kunde inte läsa NaturalEditTool-svaret.");
        }
        catch (TaskCanceledException)
        {
            return (false, "Ollama tog för lång tid under NaturalEditTool. Håll coder-modellen varm med: ollama run qwen2.5-coder:7b");
        }
        catch (HttpRequestException)
        {
            return (false, "Kunde inte ansluta till Ollama. Starta Ollama och försök igen.");
        }
        catch (Exception ex)
        {
            return (false, "Ollama-fel: " + ex.Message);
        }
    }

    private static string ExpandInputAlias(string input)
    {
        var command = NormalizeCommand(input);

        var exactAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Help
            ["kommandon"] = "kommandohjalp",
            ["vad kan du"] = "kommandohjalp",
            ["vad kan du gora"] = "kommandohjalp",
            ["vad kan du göra"] = "kommandohjalp",
            ["hur anvander jag jarvis"] = "kommandohjalp",
            ["hur använder jag jarvis"] = "kommandohjalp",
            ["hur funkar jarvis"] = "kommandohjalp",

            // Internet
            ["internet"] = "internetstatus",
            ["internetstatus"] = "internetstatus",
            ["internet status"] = "internetstatus",
            ["natstatus"] = "internetstatus",
            ["nätstatus"] = "internetstatus",
            ["webbstatus"] = "internetstatus",
            ["online status"] = "internetstatus",
            ["har jag internet"] = "internetstatus",
            ["har jag nat"] = "internetstatus",
            ["har jag nät"] = "internetstatus",
            ["funkar internet"] = "internetstatus",
            ["fungerar internet"] = "internetstatus",
            ["testa internet"] = "internetstatus",
            ["kolla internet"] = "internetstatus",
            ["kontrollera internet"] = "internetstatus",
            ["ar jag online"] = "internetstatus",
            ["är jag online"] = "internetstatus",
            ["ar du online"] = "internetstatus",
            ["är du online"] = "internetstatus",
            ["ar du uppkopplad"] = "internetstatus",
            ["är du uppkopplad"] = "internetstatus",
            ["kan du na internet"] = "internetstatus",
            ["kan du nå internet"] = "internetstatus",
            ["kan du soka internet"] = "internetstatus",
            ["kan du söka internet"] = "internetstatus",
            ["bevisa internet"] = "internetstatus",
            ["bevisa de"] = "internetstatus",
            ["bevisa det"] = "internetstatus",

            // Memory views
            ["visa mitt minne"] = "visa minne",
            ["visa minnet"] = "visa minne",
            ["visa mina minnen"] = "visa minne",
            ["visa mina viktiga minnen"] = "visa viktiga minnen",
            ["visa mina projektminnen"] = "visa projektminnen",
            ["visa projekt minnen"] = "visa projektminnen",

            // Disk/cache
            ["hur mycket plats har jag"] = "diskstatus",
            ["plats pa disken"] = "diskstatus",
            ["plats på disken"] = "diskstatus",
            ["kolla disken"] = "diskstatus",
            ["c disken"] = "diskstatus",
            ["hur mar c disken"] = "diskstatus",
            ["hur mår c disken"] = "diskstatus",
            ["rensa c disk"] = "rensa cache preview",
            ["rensa c disken"] = "rensa cache preview",
            ["vad kan rensas"] = "rensa cache preview",
            ["visa cache"] = "cachestatus",

            // Model
            ["modell"] = "visa modell",
            ["vilken modell"] = "visa modell",
            ["vilken modell anvander du"] = "visa modell",
            ["vilken modell använder du"] = "visa modell",
            ["aktiv modell"] = "visa modell",
            ["visa llm"] = "visa modell",
            ["vilken llm"] = "visa modell",
            ["ollama modeller"] = "lista modeller",
            ["vilka modeller finns"] = "lista modeller",
            ["visa modeller"] = "lista modeller",

            // Active file
            ["aktiv fil"] = "aktiv fil",
            ["visa aktiv fil"] = "aktiv fil",
            ["vilken fil"] = "aktiv fil",
            ["vilken fil ar aktiv"] = "aktiv fil",
            ["vilken fil är aktiv"] = "aktiv fil",
            ["läs filen"] = "las filen",
            ["las filen"] = "las filen",
            ["visa filen"] = "las filen",
            ["öppna filen"] = "las filen",
            ["oppna filen"] = "las filen",
            ["läs en annan fil"] = "las en annan fil",
            ["las en annan fil"] = "las en annan fil",
            ["annan fil"] = "las en annan fil",
            ["byt fil"] = "las en annan fil",

            // Checkpoint
            ["skapa backup"] = "skapa checkpoint",
            ["ta backup"] = "skapa checkpoint",
            ["gör backup"] = "skapa checkpoint",
            ["gor backup"] = "skapa checkpoint",
            ["spara checkpoint"] = "skapa checkpoint",
            ["visa backups"] = "lista checkpoints",
            ["visa checkpoint"] = "lista checkpoints",
            ["backups"] = "lista checkpoints",
            ["rollback"] = "aterstall senaste checkpoint",
            ["återställ"] = "aterstall senaste checkpoint",
            ["aterstall"] = "aterstall senaste checkpoint",

            // Pending changes
            ["godkann"] = "godkann andring",
            ["godkänn"] = "godkann andring",
            ["godkann forslag"] = "godkann andring",
            ["godkänn förslag"] = "godkann andring",
            ["acceptera forslag"] = "godkann andring",
            ["acceptera förslag"] = "godkann andring",
            ["stoppa forslag"] = "avbryt andring",
            ["stoppa förslag"] = "avbryt andring",

            // Project
            ["indexera"] = "indexera projekt",
            ["skapa projektindex"] = "indexera projekt",
            ["projektindex"] = "indexera projekt",
            ["visa projekt"] = "lista projektfiler",
            ["visa alla filer"] = "lista projektfiler",
            ["alla filer"] = "lista projektfiler",
            ["markdown filer"] = "lista md filer",
            ["md filer"] = "lista md filer",

            // Terminal preview aliases
            ["bygg projektet"] = "terminal preview: dotnet build",
            ["bygg projektet men fraga forst"] = "terminal preview: dotnet build",
            ["bygg projektet men fråga först"] = "terminal preview: dotnet build",
            ["kor build"] = "terminal preview: dotnet build",
            ["kör build"] = "terminal preview: dotnet build"
        };

        if (exactAliases.TryGetValue(command, out var exact))
            return exact;

        var archiveSearch = Regex.Match(
            input,
            @"^\s*(leta|sök|sok|hitta)\s+(?:efter\s+)?(.+?)\s+i\s+arkivet\s*$",
            RegexOptions.IgnoreCase
        );

        if (archiveSearch.Success)
            return "sok arkiv: " + archiveSearch.Groups[2].Value.Trim();

        var prefixAliases = new (string Alias, string Canonical)[]
        {
            // Memory
            ("kom ihag", "kom ihag:"),
            ("kom ihåg", "kom ihag:"),
            ("spara minne", "smart minne:"),
            ("spara i minnet", "smart minne:"),
            ("lagg i minnet", "smart minne:"),
            ("lägg i minnet", "smart minne:"),
            ("minns detta", "smart minne:"),
            ("kom ihag detta", "smart minne:"),
            ("kom ihåg detta", "smart minne:"),
            ("viktigt", "viktigt minne:"),
            ("spara viktigt", "viktigt minne:"),
            ("projekt fakta", "projektminne:"),
            ("projektfakta", "projektminne:"),
            ("spara projektminne", "projektminne:"),
            ("sok minne", "sok minne:"),
            ("sök minne", "sok minne:"),
            ("sok i minnet", "sok minne:"),
            ("sök i minnet", "sok minne:"),
            ("hitta minne", "sok minne:"),
            ("glom minne", "glom minne:"),
            ("glöm minne", "glom minne:"),
            ("glom", "glom minne:"),
            ("glöm", "glom minne:"),

            // Files
            ("las fil", "las fil:"),
            ("läs fil", "las fil:"),
            ("oppna fil", "las fil:"),
            ("öppna fil", "las fil:"),
            ("visa fil", "las fil:"),
            ("read file", "las fil:"),
            ("skapa fil", "skriv fil:"),
            ("ny fil", "skriv fil:"),
            ("skriv till fil", "lagg till fil:"),
            ("lägg till i fil", "lagg till fil:"),
            ("lagg till i fil", "lagg till fil:"),
            ("append file", "lagg till fil:"),

            // Proposals
            ("foresla rubrik", "foresla rubrik:"),
            ("föreslå rubrik", "foresla rubrik:"),
            ("foresla ide", "foresla andring:"),
            ("föreslå ide", "foresla andring:"),
            ("foresla idé", "foresla andring:"),
            ("föreslå idé", "foresla andring:"),
            ("foresla andring", "foresla andring:"),
            ("föreslå ändring", "foresla andring:"),
            ("ge forslag for", "foresla andring:"),
            ("ge förslag för", "foresla andring:"),

            // Model
            ("byt modell", "byt modell:"),
            ("andra modell", "byt modell:"),
            ("ändra modell", "byt modell:"),
            ("byt llm", "byt modell:"),
            ("anvand modell", "byt modell:"),
            ("använd modell", "byt modell:"),

            // Active file proposal
            ("foresla rubrik har", "foresla rubrik har:"),
            ("föreslå rubrik här", "foresla rubrik har:")
        };

        foreach (var (alias, canonical) in prefixAliases)
        {
            var mapped = TryExpandAliasPrefix(input, command, alias, canonical);
            if (mapped is not null)
                return mapped;
        }

        return input;
    }

    private static string? TryExpandAliasPrefix(string originalInput, string normalizedCommand, string alias, string canonical)
    {
        var normalizedAlias = NormalizeCommand(alias);

        if (normalizedCommand == normalizedAlias)
            return canonical.TrimEnd(':');

        if (!normalizedCommand.StartsWith(normalizedAlias + " ") &&
            !normalizedCommand.StartsWith(normalizedAlias + ":"))
            return null;

        var tail = "";

        if (originalInput.Length > alias.Length)
            tail = originalInput[alias.Length..].TrimStart(' ', ':');

        if (string.IsNullOrWhiteSpace(tail))
            return canonical.TrimEnd(':');

        if (canonical.EndsWith(":"))
            return canonical + " " + tail;

        return canonical + " " + tail;
    }
    private static string NormalizeCommand(string value)
    {
        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");

        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized;
    }

    private static bool CommandIs(string command, params string[] expected)
    {
        var head = CommandHead(command);

        foreach (var option in expected)
        {
            var target = NormalizeCommand(option).TrimEnd(':');

            if (head == target)
                return true;

            if (head.Replace(" ", "") == target.Replace(" ", ""))
                return true;

            if (Levenshtein(head, target) <= AllowedDistance(target))
                return true;
        }

        return false;
    }

    // Plockar ut checkpoint-namnet efter "skapa checkpoint" / "återställ checkpoint" från råinput.
    // Vi använder råinput (inte normalized command) så användaren får behålla å/ä/ö i namnet,
    // men SanitizeCheckpointNameV1 strippar dem senare till säkra tecken.
    private static string ExtractCheckpointNameFromCommandV1(string rawInput, string normalizedCommand, bool isRestore)
    {
        var raw = (rawInput ?? "").Trim();
        if (raw.Length == 0) return "";

        // Skip-listor: vi tar bort de första 2 orden (verb + "checkpoint") från raw.
        // Exempel: "skapa checkpoint innan-port" -> "innan-port"
        //          "återställ checkpoint stable" -> "stable"
        var parts = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 2) return "";
        var tail = string.Join(" ", parts.Skip(2)).Trim();

        // Om naturligt språk lade till "till"/"named"/"som" före namnet, ta bort det.
        var prefixesToStrip = new[] { "till ", "named ", "som ", "med namn ", "kallad " };
        foreach (var p in prefixesToStrip)
        {
            if (tail.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                tail = tail.Substring(p.Length).Trim();
                break;
            }
        }
        return tail;
    }

    private static bool CommandStartsWith(string command, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            var target = NormalizeCommand(prefix).TrimEnd(':');

            if (command == target || command.StartsWith(target + ":") || command.StartsWith(target + " "))
                return true;

            var head = CommandHead(command);

            if (Levenshtein(head, target) <= AllowedDistance(target))
                return true;
        }

        return false;
    }

    private static string CommandHead(string command)
    {
        var colon = command.IndexOf(':');
        if (colon >= 0)
            return command[..colon].Trim();

        return command.Trim();
    }

    private static int AllowedDistance(string target)
    {
        if (target.Length <= 5) return 1;
        if (target.Length <= 12) return 2;
        return 3;
    }

    private static int Levenshtein(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var costs = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            costs[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            var previous = costs[0];
            costs[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var temp = costs[j];
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;

                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    previous + cost
                );

                previous = temp;
            }
        }

        return costs[b.Length];
    }

    private async Task SendProjectFilesToDashboard()
    {
        if (_webView.CoreWebView2 is null)
            return;

        var files = GetProjectFilesForUi();
        var filesJson = JsonSerializer.Serialize(files);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisSetFileList(" + filesJson + ");"
        );

}

    private static List<string> GetProjectFilesForUi()
    {
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints"
        };

        var readableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1",
            ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props",
            ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv",
            ".vbs"
        };

        try
        {
            return Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (parts.Any(part => excludedDirs.Contains(part)))
                        return false;

                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return readableExtensions.Contains(Path.GetExtension(file));
                })
                .Select(file => Path.GetRelativePath(ProjectRoot, file))
                .OrderBy(file => file)
                .Take(200)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }
    private async Task SendProjectFolderToDashboard(string relativeFolder)
    {
        if (_webView.CoreWebView2 is null)
            return;

        var safe = TryGetSafeProjectFolderPath(relativeFolder, out var fullFolder, out var cleanRelative, out var error);

        if (!safe)
        {
            await AddAssistantMessage(error);
            return;
        }

        var items = GetProjectFolderItemsForUi(fullFolder);
        var itemsJson = JsonSerializer.Serialize(items);
        var pathJson = JsonSerializer.Serialize(cleanRelative);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisSetFolderItems(" + itemsJson + ", " + pathJson + ");"
        );

}

    private static bool TryGetSafeProjectFolderPath(string relativeFolder, out string fullFolder, out string cleanRelative, out string error)
    {
        fullFolder = "";
        cleanRelative = "";
        error = "";

        relativeFolder = (relativeFolder ?? "").Trim().Trim('"').Replace("\\", "/");

        if (Path.IsPathRooted(relativeFolder))
        {
            error = "Blockerat: använd relativ mapp inom F:\\Jarvis-clean.";
            return false;
        }

        if (relativeFolder.Contains(".."))
        {
            error = "Blockerat: mappen får inte innehålla ..";
            return false;
        }

        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints"
        };

        var parts = relativeFolder
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (parts.Any(part => excludedDirs.Contains(part)))
        {
            error = "Blockerat: den mappen är inte tillåten i filpanelen.";
            return false;
        }

        cleanRelative = string.Join("/", parts);
        fullFolder = Path.GetFullPath(Path.Combine(ProjectRoot, cleanRelative));

        var fullRoot = Path.GetFullPath(ProjectRoot);

        if (!fullFolder.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            error = "Blockerat: mappen måste ligga i F:\\Jarvis-clean.";
            return false;
        }

        if (!Directory.Exists(fullFolder))
        {
            error = "Mappen finns inte: " + cleanRelative;
            return false;
        }

        return true;
    }

    private static List<object> GetProjectFolderItemsForUi(string fullFolder)
    {
        var excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget", "backups", "data", ".tmp", ".checkpoints"
        };

        var readableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1",
            ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props",
            ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv",
            ".vbs"
        };

        var items = new List<object>();

        try
        {
            var folders = Directory.GetDirectories(fullFolder)
                .Where(dir => !excludedDirs.Contains(Path.GetFileName(dir)))
                .Select(dir =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, dir).Replace("\\", "/");
                    return new
                    {
                        type = "folder",
                        name = Path.GetFileName(dir),
                        path = relative
                    };
                })
                .OrderBy(item => item.name)
                .ToList();

            var files = Directory.GetFiles(fullFolder)
                .Where(file =>
                {
                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return readableExtensions.Contains(Path.GetExtension(file));
                })
                .Select(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file).Replace("\\", "/");
                    return new
                    {
                        type = "file",
                        name = Path.GetFileName(file),
                        path = relative
                    };
                })
                .OrderBy(item => item.name)
                .ToList();

            items.AddRange(folders);
            items.AddRange(files);
        }
        catch
        {
            // Returnera det som gick att läsa.
        }

        return items;
    }
    private async Task<string> OpenProjectFileSmartAsync(string requestedPathOrName)
    {
        var result = ResolveProjectFileRequest(requestedPathOrName);

        if (!result.Success)
            return result.Message;

        await SendProjectFileToEditor(result.RelativePath);

        var folder = Path.GetDirectoryName(result.RelativePath)?.Replace("\\", "/") ?? "";
        await SendTreeFolderV7Async(folder, refreshAutocomplete: false);

        return "Öppnade i filpanelen: " + result.RelativePath;
    }

    private sealed record SmartFileResolveResult(bool Success, string RelativePath, string Message);

    private static SmartFileResolveResult ResolveProjectFileRequest(string requestedPathOrName)
    {
        var requested = (requestedPathOrName ?? "").Trim().Trim('"').Replace("\\", "/");

        if (string.IsNullOrWhiteSpace(requested))
            return new SmartFileResolveResult(false, "", "Säg vilken fil du vill öppna, till exempel: öppna program.cs");

        if (Path.IsPathRooted(requested) || requested.Contains(".."))
            return new SmartFileResolveResult(false, "", "Blockerat: använd filnamn eller relativ sökväg inom F:\\Jarvis-clean.");

        if (requested.Contains("/"))
        {
            var safe = TryGetSafeProjectFilePath(requested, mustBeExisting: true, out _, out var error);

            if (!safe)
                return new SmartFileResolveResult(false, "", error);

            return new SmartFileResolveResult(true, requested, "");
        }

        var allFiles = GetReadableProjectFilesForV7();
        var currentFolder = GetCurrentTreeFolderV7();

        if (!string.IsNullOrWhiteSpace(currentFolder))
        {
            var localMatches = allFiles
                .Where(file => file.StartsWith(currentFolder + "/", StringComparison.OrdinalIgnoreCase))
                .Where(file => Path.GetFileName(file).Equals(requested, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (localMatches.Count == 1)
                return new SmartFileResolveResult(true, localMatches[0], "");
        }

        var exactMatches = allFiles
            .Where(file => Path.GetFileName(file).Equals(requested, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exactMatches.Count == 1)
            return new SmartFileResolveResult(true, exactMatches[0], "");

        if (exactMatches.Count > 1)
        {
            return new SmartFileResolveResult(
                false,
                "",
                "Jag hittade flera filer med det namnet. Välj via TAB-förslag eller skriv exakt sökväg:\n" +
                string.Join("\n", exactMatches.Select(file => "- " + file))
            );
        }

        var requestedLoose = NormalizeLooseFileName(requested);

        var fuzzyMatches = allFiles
            .Where(file =>
            {
                var fileName = Path.GetFileName(file);
                var stem = Path.GetFileNameWithoutExtension(fileName);

                return NormalizeLooseFileName(fileName).Contains(requestedLoose)
                    || NormalizeLooseFileName(stem).Contains(requestedLoose)
                    || requestedLoose.Contains(NormalizeLooseFileName(stem));
            })
            .Take(10)
            .ToList();

        if (fuzzyMatches.Count == 1)
            return new SmartFileResolveResult(true, fuzzyMatches[0], "");

        if (fuzzyMatches.Count > 1)
        {
            return new SmartFileResolveResult(
                false,
                "",
                "Jag hittade flera möjliga filer. Välj via TAB-förslag eller skriv exakt sökväg:\n" +
                string.Join("\n", fuzzyMatches.Select(file => "- " + file))
            );
        }

        return new SmartFileResolveResult(false, "", "Jag hittade ingen fil som matchar: " + requested + "\nTesta TAB-förslag eller klicka filen i Project Explorer.");
    }

    private static bool TryExtractSmartOpenFileRequest(string input, out string requestedFile)
    {
        requestedFile = "";

        var trimmed = (input ?? "").Trim();

        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("/", StringComparison.Ordinal))
            return false;

        if (IsRouterOnlyBeforeSmartOpenV1(trimmed))
            return false;

        var fileMatch = Regex.Match(
            trimmed,
            @"(?i)([a-z0-9_\-\.]+(?:[\\/][a-z0-9_\-\.]+)*\.(md|txt|json|cs|html|css|js|ps1|csproj|sln|xml|yml|yaml|config|props|targets|editorconfig|gitignore|lock|log|csv|vbs))"
        );

        if (!fileMatch.Success)
            return false;

        var candidate = fileMatch.Groups[1].Value.Trim().Trim('"');

        var hasOpenIntent =
            Regex.IsMatch(trimmed, @"(?i)\b(öppna|oppna|visa|läs|las|open|read)\b") ||
            Regex.IsMatch(trimmed, @"(?i)\b(koden|kod|filen|fil)\b");

        var onlyFileName = trimmed.Equals(candidate, StringComparison.OrdinalIgnoreCase);

        if (!hasOpenIntent && !onlyFileName)
            return false;

        requestedFile = candidate;
        return true;
    }

    private static bool IsRouterOnlyBeforeSmartOpenV1(string input)
    {
        var compact = Regex.Replace(NormalizeCommand(input), @"[^a-z0-9]", "");
        var exact = new[]
        {
            "terminal",
            "terminalpanelen",
            "visaterminal",
            "oppnaterminal",
            "visaterminalpanelen",
            "vadstoditerminalen",
            "senasteterminal",
            "terminaloutput",
            "avbryt",
            "avbrytallt",
            "cancel",
            "stoppa"
        };

        if (exact.Any(value => string.Equals(value, compact, StringComparison.OrdinalIgnoreCase)))
            return true;

        var prefixes = new[]
        {
            "skrivfil",
            "laggtillfil",
            "appendfil",
            "foreslaandring",
            "foreslarubrik",
            "godkannfilskrivning",
            "avbrytfilskrivning",
            "terminalpreview",
            "terminalvisa",
            "terminalshow",
            "terminaloutput",
            "bekraftakor",
            "avbrytkor",
            "raderafil",
            "tabortfil",
            "deletefile"
        };

        return prefixes.Any(prefix => compact.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeLooseFileName(string value)
    {
        return Regex.Replace((value ?? "").ToLowerInvariant(), "[^a-z0-9åäö]", "")
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");
    }

    private async Task SendTreeFolderV7Async(string relativeFolder, bool refreshAutocomplete)
    {
        if (_webView.CoreWebView2 is null)
            return;

        var safe = TryGetSafeTreeFolderV7(relativeFolder, out var fullFolder, out var cleanRelative, out var error);

        if (!safe)
        {
            await AddAssistantMessage(error);
            return;
        }

        SetCurrentTreeFolderV7(cleanRelative);

        var items = GetTreeFolderItemsV7(fullFolder);
        var itemsJson = JsonSerializer.Serialize(items);
        var pathJson = JsonSerializer.Serialize(cleanRelative);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisSetTreeFolderV7(" + pathJson + ", " + itemsJson + ");"
        );

        if (refreshAutocomplete)
            await SendAutocompleteFilesV7Async();
    }

    private async Task SendAutocompleteFilesV7Async()
    {
        if (_webView.CoreWebView2 is null)
            return;

        var files = GetReadableProjectFilesForV7();
        var folders = GetReadableProjectFoldersForV7();

        var filesJson = JsonSerializer.Serialize(files);
        var foldersJson = JsonSerializer.Serialize(folders);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            "window.jarvisSetAllFilesV7(" + filesJson + ", " + foldersJson + ");"
        );
    }

    private static bool TryGetSafeTreeFolderV7(string relativeFolder, out string fullFolder, out string cleanRelative, out string error)
    {
        fullFolder = "";
        cleanRelative = "";
        error = "";

        relativeFolder = (relativeFolder ?? "").Trim().Trim('"').Replace("\\", "/");

        if (Path.IsPathRooted(relativeFolder))
        {
            error = "Blockerat: använd relativ mapp inom F:\\Jarvis-clean.";
            return false;
        }

        if (relativeFolder.Contains(".."))
        {
            error = "Blockerat: mappen får inte innehålla ..";
            return false;
        }

        var parts = relativeFolder
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (parts.Any(IsExcludedProjectDirV7))
        {
            error = "Blockerat: den mappen är inte tillåten i Project Explorer.";
            return false;
        }

        cleanRelative = string.Join("/", parts);
        fullFolder = Path.GetFullPath(Path.Combine(ProjectRoot, cleanRelative));

        var fullRoot = Path.GetFullPath(ProjectRoot);

        if (!fullFolder.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            error = "Blockerat: mappen måste ligga i F:\\Jarvis-clean.";
            return false;
        }

        if (!Directory.Exists(fullFolder))
        {
            error = "Mappen finns inte: " + cleanRelative;
            return false;
        }

        return true;
    }

    private static List<object> GetTreeFolderItemsV7(string fullFolder)
    {
        var readableExtensions = ReadableExtensionsV7();
        var items = new List<object>();

        try
        {
            var folders = Directory.GetDirectories(fullFolder)
                .Where(dir => !IsExcludedProjectDirV7(Path.GetFileName(dir)))
                .Select(dir =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, dir).Replace("\\", "/");
                    return new
                    {
                        type = "folder",
                        name = Path.GetFileName(dir),
                        path = relative
                    };
                })
                .OrderBy(item => item.name)
                .Cast<object>();

            var files = Directory.GetFiles(fullFolder)
                .Where(file =>
                {
                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return readableExtensions.Contains(Path.GetExtension(file));
                })
                .Select(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file).Replace("\\", "/");
                    return new
                    {
                        type = "file",
                        name = Path.GetFileName(file),
                        path = relative
                    };
                })
                .OrderBy(item => item.name)
                .Cast<object>();

            items.AddRange(folders);
            items.AddRange(files);
        }
        catch
        {
            // Returnera det som kunde läsas.
        }

        return items;
    }

    private static List<string> GetReadableProjectFilesForV7()
    {
        var readableExtensions = ReadableExtensionsV7();

        try
        {
            return Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories)
                .Where(file =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, file);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (parts.Any(IsExcludedProjectDirV7))
                        return false;

                    if (Path.GetFileName(file).Equals("build-error.txt", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return readableExtensions.Contains(Path.GetExtension(file));
                })
                .Select(file => Path.GetRelativePath(ProjectRoot, file).Replace("\\", "/"))
                .OrderBy(file => file)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<string> GetReadableProjectFoldersForV7()
    {
        try
        {
            return Directory.GetDirectories(ProjectRoot, "*", SearchOption.AllDirectories)
                .Where(dir =>
                {
                    var relative = Path.GetRelativePath(ProjectRoot, dir);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    return !parts.Any(IsExcludedProjectDirV7);
                })
                .Select(dir => Path.GetRelativePath(ProjectRoot, dir).Replace("\\", "/"))
                .OrderBy(dir => dir)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static HashSet<string> ReadableExtensionsV7()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1",
            ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props",
            ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv",
            ".vbs"
        };
    }

    private static bool IsExcludedProjectDirV7(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget",
            "backups", "data", ".tmp", ".checkpoints"
        };

        return excluded.Contains(name);
    }

    private static string CurrentTreeFolderV7Path()
    {
        return Path.Combine(ProjectRoot, "config", "current_tree_folder_v7.txt");
    }

    private static void SetCurrentTreeFolderV7(string relativeFolder)
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(ProjectRoot, "config"));
            File.WriteAllText(CurrentTreeFolderV7Path(), (relativeFolder ?? "").Trim().Replace("\\", "/"));
        }
        catch
        {
            // Convenience state only.
        }
    }

    private static string GetCurrentTreeFolderV7()
    {
        try
        {
            var path = CurrentTreeFolderV7Path();

            if (!File.Exists(path))
                return "";

            return File.ReadAllText(path).Trim().Replace("\\", "/");
        }
        catch
        {
            return "";
        }
    }
    private async Task SendProjectFileToEditor(string relativePath)
    {
        if (_webView.CoreWebView2 is null)
            return;

        var safe = TryGetSafeProjectFilePath(relativePath, mustBeExisting: true, out var fullPath, out var error);

        if (!safe)
        {
            await AddAssistantMessage(error);
            return;
        }

        try
        {
            var content = File.ReadAllText(fullPath);
            var isEditorContentTruncated = false;

            if (content.Length > 200000)
            {
                content = content[..200000] + "\n\n...filen är mycket lång, visar bara första delen.";
                isEditorContentTruncated = true;
            }

            var cleanRelative = relativePath.Replace("\\", "/").Trim();
            SetActiveFile(cleanRelative);

            var pathJson = JsonSerializer.Serialize(cleanRelative);
            var contentJson = JsonSerializer.Serialize(content);
            var truncatedJson = JsonSerializer.Serialize(isEditorContentTruncated);

            await _webView.CoreWebView2.ExecuteScriptAsync(
                "window.jarvisSetEditorFile(" + pathJson + ", " + contentJson + ", true, " + truncatedJson + ");"
            );

}
        catch (Exception ex)
        {
            await AddAssistantMessage("Kunde inte öppna filen i mittenpanelen: " + ex.Message);
        }
    }

    private static bool TryExtractOpenFileRequest(string input, out string relativePath)
    {
        relativePath = "";

        var match = Regex.Match(
            input,
            @"(?i)^\s*(läs fil|las fil|öppna fil|oppna fil|visa fil|open file)\s*:?\s*(.+?)\s*$"
        );

        if (!match.Success)
            return false;

        relativePath = match.Groups[2].Value.Trim().Trim('"');
        return !string.IsNullOrWhiteSpace(relativePath);
    }

    private static bool TryExtractOpenFolderRequest(string input, out string relativeFolder)
    {
        relativeFolder = "";

        var match = Regex.Match(
            input,
            @"(?i)^\s*(öppna mapp|oppna mapp|visa mapp|gå till mapp|ga till mapp|open folder|öppna folder|oppna folder)\s*:?\s*(.*?)\s*$"
        );

        if (!match.Success)
            return false;

        relativeFolder = match.Groups[2].Value.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(relativeFolder))
            relativeFolder = "";

        return true;
    }
    private async Task AddAssistantMessage(string message)
    {
        if (_webView.CoreWebView2 is null)
            return;

        var roleJson = JsonSerializer.Serialize("assistant");
        var textJson = JsonSerializer.Serialize(message);

        await _webView.CoreWebView2.ExecuteScriptAsync(
            $"window.jarvisAddMessage({roleJson}, {textJson});"
        );
    }
}



































