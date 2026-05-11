using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Text.Json;

namespace JarvisClean;

// BrainWindow — separat fönster för 3D-visualisering av Jarvis hjärnregioner.
// Fas 3 (statisk): laddar dashboard\brain.html via virtuell host så Three.js-importer från
// dashboard\vendor\ resolverar med relativa URL:er. Ingen Python-server används än;
// auto-start av neurolinked/run.py kommer i Fas 5.
//
// Säkerhet:
// - Inga filändringar görs här. PendingApprovalV1 hanteras fortsatt av main JarvisForm.
// - Stänga Brain-fönstret döljer det istället för att förstöra (snabb återöppning).
// - Om Three.js eller WebGL fallerar visar brain.html en fallback-panel.
public sealed class BrainWindow : Form
{
    private const string BrainHtmlVirtualHost = "jarvis.local";
    private const string BrainHtmlUrl = "https://jarvis.local/brain.html";
    private static readonly string DashboardFolder = @"F:\Jarvis-clean\dashboard";

    private readonly WebView2 _webView = new();
    private bool _actuallyClose;
    private bool _initialized;

    public BrainWindow()
    {
        Text = "Jarvis Brain";
        Width = 1200;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        _webView.Dock = DockStyle.Fill;
        Controls.Add(_webView);

        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    // Initialisera WebView2 lazy så main-fönstret inte blockeras vid app-start.
    private async void OnLoad(object? sender, EventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            // Delad user-data-folder med main-fönstret så cache och cookies delas.
            Directory.CreateDirectory(@"F:\DevCache\WebView2\Jarvis");
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: @"F:\DevCache\WebView2\Jarvis"
            );

            await _webView.EnsureCoreWebView2Async(env);

            // SetVirtualHostNameToFolderMapping: gör att brain.html kan importera
            // /vendor/three.module.js som "https://jarvis.local/vendor/three.module.js".
            // Allow = read-only access; appen kan inte skriva via denna mapping.
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                BrainHtmlVirtualHost,
                DashboardFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            // Mappa även projektrot för graphify-out\graph.json (read-only).
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "jarvis.local",
                @"F:\Jarvis-clean",
                CoreWebView2HostResourceAccessKind.Allow);

            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webView.CoreWebView2.Navigate(BrainHtmlUrl);
        }
        catch (Exception ex)
        {
            // Fallback om WebView2-runtime eller mapping fallerar — visa fel i fönstret istället för att krascha.
            var html = "<html><body style='background:#03060c;color:#cfeaff;font-family:Consolas;padding:24px;'>" +
                       "<h2 style='color:#ff8888;'>Brain-fönstret kunde inte starta</h2>" +
                       "<p>" + System.Net.WebUtility.HtmlEncode(ex.Message) + "</p>" +
                       "<p style='opacity:0.7;'>Main-fönstret fortsätter fungera.</p></body></html>";
            try { _webView.NavigateToString(html); } catch { /* sista utvägen — bara svälja */ }
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Brain-fönstret postar bara informativa meddelanden; ingen filändring sker härifrån.
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : "";
            // Just nu loggar vi inget — main-fönstret behöver inte veta brain-events i Fas 3.
            // I Fas 5+ kan vi vidarebefordra knowledge-events till main-bridgen.
            _ = type;
        }
        catch { /* tysta JSON-fel; brain-meddelanden är best-effort */ }
    }

    // Stäng-knapp döljer fönstret istället för att förstöra det. Återöppning blir snabb.
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

    // Anropas av JarvisForm vid app-shutdown så fönstret faktiskt stängs.
    public void ForceClose()
    {
        _actuallyClose = true;
        Close();
    }
}
