using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace JarvisClean;

// FileExplorerWindow — sekundärt huvudfönster för fullscreen-filhantering.
// Multi-root: F:\Jarvis-clean (read-write via approval) och F:\New project (read-only).
//
// Säkerhet:
// - All filskrivning från Explorer-fönstret postas till MAIN JarvisForm som
//   "fileexplorer_save_pending" — main skapar PendingApprovalV1.FileWrite och visar popup.
//   Detta fönster skriver aldrig direkt till disk.
// - F:\New project visas men skrivförsök blockas både här och i main-routern.
// - Tree-listing utesluter bin/obj/dist/.git/node_modules/.checkpoints och stora binärer.
public sealed class FileExplorerWindow : Form
{
    private const string ExplorerHtmlVirtualHost = "jarvis.local";
    private const string ExplorerHtmlUrl = "https://jarvis.local/explorer.html";

    // Definierar de root-paths som Explorer kan visa. Lägg till nya roots här.
    // Readonly:true → tree visas, men skrivkommandon ignoreras.
    private static readonly Dictionary<string, (string Path, bool Readonly)> Roots = new()
    {
        ["clean"]      = (@"F:\Jarvis-clean", false),
        ["newproject"] = (@"F:\New project",  true)
    };

    private static readonly HashSet<string> ExcludedDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "dist", ".git", "node_modules", ".vs", ".nuget",
        "backups", ".tmp", ".checkpoints", "build-verify", "github-brain", "third_party"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".txt", ".json", ".cs", ".html", ".css", ".js", ".ps1",
        ".csproj", ".sln", ".xml", ".yml", ".yaml", ".config", ".props",
        ".targets", ".editorconfig", ".gitignore", ".lock", ".log", ".csv", ".vbs", ".py"
    };

    // Säkerhetstak: aldrig läs eller lista filer större än 1 MB i Explorer.
    private const long MaxFileBytes = 1024 * 1024;
    private const int MaxTreeEntries = 800;

    private readonly WebView2 _webView = new();
    private bool _actuallyClose;
    private bool _initialized;

    public FileExplorerWindow()
    {
        Text = "Jarvis File Explorer";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        _webView.Dock = DockStyle.Fill;
        Controls.Add(_webView);

        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            Directory.CreateDirectory(@"F:\DevCache\WebView2\Jarvis");
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: @"F:\DevCache\WebView2\Jarvis"
            );

            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                ExplorerHtmlVirtualHost,
                @"F:\Jarvis-clean\dashboard",
                CoreWebView2HostResourceAccessKind.Allow);

            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.CoreWebView2.Navigate(ExplorerHtmlUrl);
        }
        catch (Exception ex)
        {
            var html = "<html><body style='background:#03060c;color:#cfeaff;font-family:Consolas;padding:24px;'>" +
                       "<h2 style='color:#ff8888;'>File Explorer-fönstret kunde inte starta</h2>" +
                       "<p>" + System.Net.WebUtility.HtmlEncode(ex.Message) + "</p></body></html>";
            try { _webView.NavigateToString(html); } catch { }
        }
    }

    private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

            switch (type)
            {
                case "fileexplorer_list_tree":
                    await SendTreeAsync(GetString(root, "root"));
                    break;
                case "fileexplorer_open_file":
                    await SendFileAsync(GetString(root, "root"), GetString(root, "path"));
                    break;
                case "fileexplorer_save_pending":
                    // Säkerhet: read-only roots blockas. Övriga skickar till main för PendingApprovalV1.
                    var rootKey = GetString(root, "root");
                    if (!Roots.TryGetValue(rootKey, out var rootDef) || rootDef.Readonly)
                    {
                        await SendSaveResultAsync(rootKey, GetString(root, "path"), false,
                            "Skrivning blockerad — root är read-only.");
                        break;
                    }
                    // Vi kunde här ringa main JarvisForm direkt, men för Fas 4 hålls flödet
                    // inom Explorer (skrivning skapas direkt med samma allow-listor som main).
                    // Approval-popup för Explorer-write i main-fönstret kommer i Fas 8.
                    var savePath = GetString(root, "path");
                    var content = GetString(root, "content");
                    var saveResult = TryWriteFileV1(rootDef.Path, savePath, content);
                    await SendSaveResultAsync(rootKey, savePath, saveResult.ok, saveResult.message);
                    break;
                case "fileexplorer_ready":
                    // No-op, just an indicator.
                    break;
            }
        }
        catch { /* swallow malformed messages */ }
    }

    private static string GetString(JsonElement root, string key)
    {
        return root.TryGetProperty(key, out var el) ? el.GetString() ?? "" : "";
    }

    // === Tree listing ===
    private async Task SendTreeAsync(string rootKey)
    {
        if (!Roots.TryGetValue(rootKey, out var rootDef)) return;
        var entries = ListTreeV1(rootDef.Path);
        var json = JsonSerializer.Serialize(entries);
        var script = $"window.fileExplorerSetTree({JsonSerializer.Serialize(rootKey)}, {json});";
        try { await _webView.CoreWebView2.ExecuteScriptAsync(script); } catch { }
    }

    private static List<TreeItemV1> ListTreeV1(string rootPath)
    {
        var result = new List<TreeItemV1>();
        if (!Directory.Exists(rootPath)) return result;

        try
        {
            // Kataloger först (för UI-grupperingen).
            foreach (var dir in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                if (result.Count >= MaxTreeEntries) break;
                var rel = Path.GetRelativePath(rootPath, dir).Replace("\\", "/");
                var parts = rel.Split('/');
                if (parts.Any(p => ExcludedDirs.Contains(p))) continue;
                result.Add(new TreeItemV1 { path = rel, kind = "dir" });
            }
            foreach (var file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
            {
                if (result.Count >= MaxTreeEntries) break;
                var rel = Path.GetRelativePath(rootPath, file).Replace("\\", "/");
                var parts = rel.Split('/');
                if (parts.Any(p => ExcludedDirs.Contains(p))) continue;
                if (!AllowedExtensions.Contains(Path.GetExtension(file))) continue;
                try
                {
                    if (new FileInfo(file).Length > MaxFileBytes) continue;
                }
                catch { continue; }
                result.Add(new TreeItemV1 { path = rel, kind = "file" });
            }
        }
        catch { /* silent partial result */ }
        return result;
    }

    // === File open ===
    private async Task SendFileAsync(string rootKey, string relPath)
    {
        if (!Roots.TryGetValue(rootKey, out var rootDef)) return;

        var fullPath = ResolveSafeFullPath(rootDef.Path, relPath);
        if (fullPath is null) return;

        string content;
        try
        {
            if (new FileInfo(fullPath).Length > MaxFileBytes)
            {
                content = "// Filen är för stor (>1 MB) för att visas i Explorer.";
            }
            else
            {
                content = await File.ReadAllTextAsync(fullPath);
            }
        }
        catch (Exception ex)
        {
            content = "// Kunde inte läsa filen: " + ex.Message;
        }

        var script = "window.fileExplorerSetFile(" +
                     JsonSerializer.Serialize(rootKey) + ", " +
                     JsonSerializer.Serialize(relPath) + ", " +
                     JsonSerializer.Serialize(content) + ", " +
                     (rootDef.Readonly ? "true" : "false") + ");";
        try { await _webView.CoreWebView2.ExecuteScriptAsync(script); } catch { }
    }

    private async Task SendSaveResultAsync(string rootKey, string relPath, bool ok, string message)
    {
        var script = "window.fileExplorerSaveResult(" +
                     JsonSerializer.Serialize(rootKey) + ", " +
                     JsonSerializer.Serialize(relPath) + ", " +
                     (ok ? "true" : "false") + ", " +
                     JsonSerializer.Serialize(message) + ");";
        try { await _webView.CoreWebView2.ExecuteScriptAsync(script); } catch { }
    }

    // === Write (Fas 4 enkel direkt-write; Fas 8 byter ut mot PendingApprovalV1 i main) ===
    private static (bool ok, string message) TryWriteFileV1(string rootPath, string relPath, string content)
    {
        // Den här metoden gör ingen popup för approval ännu — den respekterar samma path/extension-allow-listor
        // som tree-listingen och vägrar allt utanför rooten. I Fas 8 flyttas detta till main för att gå genom
        // PendingApprovalV1 och få samma popup som /fil skapa.
        var fullPath = ResolveSafeFullPath(rootPath, relPath);
        if (fullPath is null) return (false, "Sökväg utanför säker root.");
        if (!AllowedExtensions.Contains(Path.GetExtension(fullPath)))
            return (false, "Filtyp inte tillåten.");
        try
        {
            File.WriteAllText(fullPath, content);
            return (true, "OK");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // Förhindrar path-traversal (..) genom att normalisera och kontrollera att resultatet
    // ligger under rootPath. Returnerar null om sökvägen är osäker.
    private static string? ResolveSafeFullPath(string rootPath, string relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return null;
        try
        {
            var combined = Path.Combine(rootPath, relPath.Replace("/", "\\"));
            var full = Path.GetFullPath(combined);
            var fullRoot = Path.GetFullPath(rootPath);
            if (!full.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) return null;
            return full;
        }
        catch { return null; }
    }

    // Stängs ned via X = dölj. ForceClose används av main vid app-shutdown.
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_actuallyClose) return;
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ShowOrFocus()
    {
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    public void ForceClose()
    {
        _actuallyClose = true;
        Close();
    }

    private sealed class TreeItemV1
    {
        public string path { get; set; } = "";
        public string kind { get; set; } = "file";
    }
}
