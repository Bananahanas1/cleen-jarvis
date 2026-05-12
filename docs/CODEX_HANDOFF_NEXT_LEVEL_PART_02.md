# CODEX_HANDOFF_NEXT_LEVEL PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


    public static async Task<string> FetchAndSummarizeAsync(string url, int maxChars = 3000)
    {
        if (!await Program.JarvisForm.IsInternetOnlineCachedAsync()) return "Internet saknas.";
        try
        {
            var html = await Http.GetStringAsync(url);
            var text = StripHtml(html);
            return text.Length > maxChars ? text.Substring(0, maxChars) + "..." : text;
        }
        catch (Exception ex) { return "Fel: " + ex.Message; }
    }

    private static string StripHtml(string s) => Regex.Replace(s, @"<[^>]+>", " ").Replace("&nbsp;", " ").Replace("&amp;", "&").Trim();
}
```

**Slash i CommandRouterV1**:
- `/sök <query>` → WebSearcher.SearchAsync, formatera som lista i chat
- `/läs <url>` → FetchAndSummarizeAsync

**Ollama-integration** (auto-search):
- I `AskOllamaAsync`: om query matchar `^(vad|vem|när|var|hur)\b` → kalla `WebSearcher.SearchAsync(query, 3)` async, prepend topp-3-snippets i system-prompt

## Spår D — Desktop-kontroll via UI-TARS

### D1. UI-TARS-bridge (~3h)

**Studera först**: `F:\UI-TARS-desktop-main\multimodal\agent-tars\core\` — kolla hur HTTP-API ser ut.

**Ny fil**: `app\Bridges\UiTarsBridge.cs` (mönster från `NeuroLinkedBridge.cs`):

```csharp
namespace JarvisClean;

public sealed class UiTarsBridge
{
    private const string UiTarsRoot = @"F:\UI-TARS-desktop-main";
    private Process? _process;

    public bool IsAvailable() => Directory.Exists(UiTarsRoot) && File.Exists(Path.Combine(UiTarsRoot, "package.json"));

    public async Task<bool> StartAsync()
    {
        // Kör: pnpm --filter agent-tars start (eller motsvarande)
        // Vänta på "Server listening on port 9999"
        // Returnera true/false
    }

    public async Task<string?> SendInstructionAsync(string instruction)
    {
        // POST http://localhost:9999/instruct  body { task: instruction }
        // Returnera JSON-svar med actions tagna
    }

    public async Task StopAsync() { /* kill process */ }
}
```

**Säkerhet**: bara starta vid `/desktop på`, default OFF.

### D2. Öppna program (säker subset, ~1h)

**Ny fil**: `app\Desktop\SafeAppLauncher.cs`

```csharp
namespace JarvisClean;

// Whitelist av program som får öppnas via Jarvis. Allt annat blockeras.
public static class SafeAppLauncher
{
    private static readonly Dictionary<string, string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        ["notepad"] = "notepad.exe",
        ["vscode"] = @"C:\Users\banan\AppData\Local\Programs\Microsoft VS Code\Code.exe",
        ["chrome"] = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        ["edge"] = "msedge.exe",
        ["explorer"] = "explorer.exe",
        ["spotify"] = @"C:\Users\banan\AppData\Roaming\Spotify\Spotify.exe",
        ["calc"] = "calc.exe"
    };

    public static string TryLaunch(string appName)
    {
        if (!Whitelist.TryGetValue(appName.Trim().ToLowerInvariant(), out var path))
            return "Programmet '" + appName + "' är inte whitelistat. Tillåtna: " + string.Join(", ", Whitelist.Keys);
        try
        {
            System.Diagnostics.Process.Start(path);
            return "Öppnar " + appName + ".";
        }
        catch (Exception ex) { return "Kunde inte öppna: " + ex.Message; }
    }
}
```

**Slash**: `/öppna program <namn>` + naturligt "öppna notepad".

### D3. Skärm-capture (~2h)

**Ny fil**: `app\Desktop\ScreenCapture.cs`

```csharp
using System.Drawing;
using System.Drawing.Imaging;
namespace JarvisClean;

public static class ScreenCapture
{
    public static string? CaptureToFile()
    {
        try
        {
            var bounds = Screen.PrimaryScreen!.Bounds;
            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(0, 0, 0, 0, bounds.Size);
            var dir = Path.Combine(@"F:\Jarvis-clean", "data", "screenshots");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
            bmp.Save(path, ImageFormat.Png);
            return path;
        }
        catch { return null; }
    }
}
```

**Slash**: `/skärm` → tar screenshot, sparar, postar fil-path till chat.

### D5. System-wide search & open (~2-3h)

**Önskemål från användaren 2026-05-10**: "söka i hela datorn och öppna efter sökta program eller fil".

**Två strategier — välj:**

**Strategi A — Windows Search API** (rekommenderat, ingen extra installation):
- Använd `System.Data.OleDb` med Windows Search Service connection string
- Query: `SELECT TOP 50 System.ItemPathDisplay, System.FileName FROM SYSTEMINDEX WHERE System.FileName LIKE '%query%'`
- Snabbt, indexat, ingår i Windows
- Begränsning: bara filer i indexerade mappar (Documents, Desktop, etc — kan utökas i Windows-inställningar)

**Strategi B — Everything CLI** (snabbast men kräver Everything-installation):
- Detect Everything via `C:\Program Files\Everything\Everything.exe`
- Kalla `Everything.exe -search <query> -filename -no-result-list -get-result-count`
- Eller HTTP-API: `http://localhost:9999/?search=<query>&json=1` om HTTP server är aktiverad
- Mycket snabbare än Windows Search men inte default-installerat

**Ny fil**: `app/Desktop/SystemSearch.cs`

```csharp
using System.Data.OleDb;
using System.Diagnostics;
namespace JarvisClean;

public static class SystemSearch
{
    public sealed record SearchHit(string Path, string Name, string Kind);

    // Strategi A: Windows Search via OleDb
    public static List<SearchHit> SearchIndexed(string query, int max = 50)
    {
        var hits = new List<SearchHit>();
        if (string.IsNullOrWhiteSpace(query)) return hits;
        try
        {
            var conn = new OleDbConnection(
                "Provider=Search.CollatorDSO;Extended Properties=\"Application=Windows\";");
            conn.Open();
            var q = query.Replace("'", "''");
            var sql =
                "SELECT TOP " + max + " System.ItemPathDisplay, System.FileName, System.ItemType " +
                "FROM SYSTEMINDEX " +
                "WHERE System.FileName LIKE '%" + q + "%' " +
                "ORDER BY System.DateModified DESC";
            using var cmd = new OleDbCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var path = reader.GetString(0);
                var name = reader.GetString(1);
                var kind = reader.IsDBNull(2) ? "" : reader.GetString(2);
                hits.Add(new SearchHit(path, name, kind));
            }
        }
        catch { /* index might be unavailable */ }
        return hits;
    }

    // Strategi B fallback: enkel disk-scan i kända program-mappar (långsam, sista utväg)
    public static List<SearchHit> SearchProgramFolders(string query, int max = 30)
    {
        var hits = new List<SearchHit>();
        var roots = new[] {
            @"C:\Program Files", @"C:\Program Files (x86)",
            @"C:\Users\banan\AppData\Local\Programs",
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs"
        };
        var lc = query.ToLowerInvariant();
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var f in Directory.EnumerateFiles(root, "*.exe", SearchOption.AllDirectories))
                {
                    if (hits.Count >= max) break;
                    var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                    if (name.Contains(lc)) hits.Add(new SearchHit(f, Path.GetFileName(f), "exe"));
                }
                foreach (var f in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
                {
                    if (hits.Count >= max) break;
                    var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                    if (name.Contains(lc)) hits.Add(new SearchHit(f, Path.GetFileName(f), "shortcut"));
                }
            }
            catch { }
        }
        return hits;
    }

    public static string OpenHit(SearchHit hit)
    {
        // Säkerhet: bara öppna .exe, .lnk, eller registrerade fil-typer.
        // BLOCKERA: .bat, .cmd, .ps1, .vbs (kan köra arbiträr kod)
        var ext = Path.GetExtension(hit.Path).ToLowerInvariant();
        var dangerousExts = new HashSet<string> { ".bat", ".cmd", ".ps1", ".vbs", ".js", ".wsf" };
        if (dangerousExts.Contains(ext))
            return "BLOCKERAD: " + ext + "-filer öppnas inte av säkerhetsskäl. Öppna manuellt om du vill.";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = hit.Path,
                UseShellExecute = true,  // låter Windows hantera .lnk, file-associations
                ErrorDialog = false
            };
            var p = Process.Start(psi);
            return p is null ? "Kunde inte öppna." : "Öppnade: " + hit.Path;
        }
        catch (Exception ex) { return "Fel: " + ex.Message; }
    }
}
```

**Routing i `CommandRouterV1.cs`**:
- Nya intents: `SystemSearch`, `SystemOpen`
- `/hitta <query>` → SystemSearch (visa lista i chat med numrerade hits)
- `/öppna hit <N>` → öppna nummer N från senaste sök-listan (state i Program.cs)
- Naturligt: "hitta foo" / "sök efter foo på datorn"

**Säkerhetsregler**:
- ❌ Aldrig öppna `.bat/.cmd/.ps1/.vbs/.js/.wsf` — användaren får göra manuellt
- ❌ Blacklist sökvägar: `C:\Windows\System32`, `regedit.exe`, `cmd.exe`, `powershell.exe`, `taskmgr.exe`
- ✅ Visa **pending approval popup** för icke-whitelistade `.exe` (whitelistade i SafeAppLauncher öppnas direkt)
- ✅ Logg till `data/desktop_actions.log`
- ✅ Max 50 hits returnerade per query (förhindrar DoS-flod)

**Pseudo-flow**:
```
användare: hitta photoshop
Jarvis:    Hittade 3 träffar:
           [1] Adobe Photoshop 2024 → C:\Program Files\Adobe\...\Photoshop.exe
           [2] Photoshop Express → C:\Users\...\PhotoshopExpress.exe
           [3] Photoshop Tutorial.pdf → C:\Users\banan\Desktop\...
användare: öppna hit 1
Jarvis:    [Pending approval] Öppna Adobe Photoshop 2024? [Godkänn] [Avbryt]
användare: [godkänn]
Jarvis:    Öppnade Adobe Photoshop 2024.
```

**Test**: `tests/system-search.test.js` — verifiera blacklist, dangerous-ext-block, max-50-cap.

**Tid: 2-3h**.

### D4. Klick/typ via UI-TARS (~4h, sist)

Bygg på D1 + D3:
1. Användaren skriver "klicka på Send-knappen"
2. ScreenCapture → fil
3. UiTarsBridge.SendInstructionAsync med screenshot + instruktion
4. UI-TARS returnerar `{action: "click", x: 812, y: 445, confidence: 0.91}`
5. Visa pending approval popup med thumbnail + "klick @ x=812, y=445"
6. På godkänn: använd `nutjs` eller motsvarande för faktiska klicket
