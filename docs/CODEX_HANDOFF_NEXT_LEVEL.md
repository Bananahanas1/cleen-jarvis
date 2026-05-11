# CODEX_HANDOFF_NEXT_LEVEL.md

Skapad: 2026-05-10
Mål: Codex tar över bygget av Spår B, C, D från `JARVIS_NEXT_LEVEL_SUPERPLAN.md`. Claude Code har slut på tokens.

## Vad är klart innan handoff

Spår A — alla 4 fixar implementerade:
- ✅ A1: Klick på projektfil-nod öppnar direkt i editor (ingen extra inspector-popup)
- ✅ A2: Recently-modified-ringarna borttagna (mtimeMin finns kvar i inspector)
- ✅ A3: "Bygg om"-knapp scramblar positioner + räknar ned + disabled under bygget
- ✅ A4: `kom ihåg X` → sparas i `memory.md` OCH `vault/auto/<datum>-<topic>.md` (auto-promote)

`F:\Jarvis-clean\` build status: 0 errors, 27 node-tester gröna, 63 C#-tester gröna.

## Statusuppdatering efter Codex 2026-05-10

Följande delar är nu redan implementerade i aktuell kod:
- B1 ModelRouter
- B2 ConversationHistory
- C1/C2 WebSearcher enligt användarens Google+Opera-val
- D2 SafeAppLauncher
- B3 NaturalEditTool första pass
- B4 BuilderMode första pass
- D1/D3/D4 UI-TARS desktop-control safe pass

B3-status:
- Ny fil: `app\Brain\NaturalEditTool.cs`
- Slash: `/edit <fil> = <beskrivning>`
- Naturligt språk: `gå in i docs/test.md och gör texten tydligare`
- Resultat: pending `FileWrite` preview via `PendingApprovalV1`; ingen direkt skrivning.
- Tester: `tests\natural-edit-tool.test.js` + C# router/validator-tester.

B4-status:
- Ny fil: `app\Brain\BuilderMode.cs`
- Slash: `/bygg <idé>`, `/bygg svar <svar>`, `/bygg plan`, `/bygg status`, `/bygg avbryt`
- Resultat: `/bygg plan` skapar pending `FileCreate` för `vault/builds/<slug>/PLAN.md`; ingen direkt skrivning.
- Tester: `tests\builder-mode.test.js` + C# router/validator-tester.
- Kvar i senare fas: skapa filer från godkänd plan stegvis via `PendingApprovalV1`.

D1/D3/D4-status:
- Ny bridge: `app\Bridges\UiTarsBridge.cs`
- Nya desktop-filer: `app\Desktop\ScreenCapture.cs`, `DesktopActionRequestV1.cs`, `DesktopActionGate.cs`, `DesktopActionExecutor.cs`
- Slash: `/desktop status`, `/desktop på`, `/desktop av`, `/desktop tars start`, `/desktop tars stop`, `/skärm`, `/desktop klick`, `/desktop skriv`, `/desktop fråga`
- Resultat: click/type/scroll/drag/hotkey går via pending `DesktopAction`; UI-TARS/VLM får bara föreslå action.
- Hard kill: Ctrl+Shift+Alt+J.
- Tester: `tests\desktop-control.test.js` + C# router/parser-tester.
- Kvar: thumbnail i approval-popup, multi-monitor och verklig `/desktop fråga` kräver UI-TARS-kompatibel API-konfig.

Kvar efter detta dokument:
- BuilderMode nästa fas: skapa filer från plan via pending approval.
- UI-TARS polish: thumbnail i approval-popup, multi-monitor, bättre post-action verifiering.

## Filer Codex bör läsa först

Läs i denna ordning:
1. `docs\JARVIS_NEXT_LEVEL_SUPERPLAN.md` — översikt + alla beslut + säkerhet
2. `docs\BRAIN_3D_SUPERPLAN.md` — 3D-grafens designprinciper
3. `docs\UNIFICATION_PLAN.md` — bakgrund
4. `vault\Decisions\DECISIONS_LOG.md` — alla beslut hittills
5. `vault\Memory\Azu_preferences.md` — användarens regler
6. `app\Program.cs` — JarvisForm (5400+ rader, alla tools)
7. `app\CommandRouterV1.cs` — slash-routing-mönster
8. `app\Brain\VaultSearcher.cs` — pattern för Brain/-tools
9. `app\Brain\FileGraphBuilder.cs` — fil-graf
10. `app\Bridges\NeuroLinkedBridge.cs` — pattern för subprocess-bridge

## Spår B — Conversational Jarvis

### B1. Multi-model orchestration (~2h)

**Ny fil**: `app\Brain\ModelRouter.cs`

```csharp
using System.Text.RegularExpressions;
namespace JarvisClean;

// Auto-routing: enkla → fast, komplexa → smart, kod → coder, planering → reason.
// Används av AskOllamaAsync när användaren INTE har bytt modell manuellt med /modell byt.
public static class ModelRouter
{
    private static readonly Regex CodeRx = new(@"\b(klass|funktion|metod|fil|kod|fix|bug|implementera|refaktorera|debug|exception|stack|method|class|function)\b", RegexOptions.IgnoreCase);
    private static readonly Regex PlanRx = new(@"\b(planera|design|arkitektur|tänk|borde|hur ska|strategi|approach|trade-off)\b", RegexOptions.IgnoreCase);
    private static readonly Regex ReasonRx = new(@"\b(varför|analysera|jämför|utvärdera|bevisa|härled)\b", RegexOptions.IgnoreCase);

    public static (string model, string reason) PickModelForQuery(string query, int turnDepth, string? activeModelOverride)
    {
        // Användaren har valt modell manuellt — respektera det
        if (!string.IsNullOrEmpty(activeModelOverride) && activeModelOverride != ModelCatalog.Fast.Name)
            return (activeModelOverride, "manuellt val");

        if (CodeRx.IsMatch(query)) return (ModelCatalog.Coder.Name, "kod-task");
        if (ReasonRx.IsMatch(query)) return (ModelCatalog.Reason.Name, "djupanalys");
        if (PlanRx.IsMatch(query) || query.Length > 200) return (ModelCatalog.Smart.Name, "komplext");
        if (turnDepth > 5) return (ModelCatalog.Smart.Name, "djup dialog");
        return (ModelCatalog.Fast.Name, "snabb");
    }
}
```

**Ändra i `Program.cs`**:
- I `AskOllamaAsync`: byt `model = GetActiveOllamaModel()` mot `model = ModelRouter.PickModelForQuery(text, _conversationHistory.Count, _activeModel == OllamaModel ? null : _activeModel).model`
- Visa badge i chat-svar: prepend `[fast]` / `[smart]` / `[code]` etc.
- Test i `tests/CommandRouterV1.Tests/Program.cs`: 5 tester för olika query-mönster

### B2. Multi-turn context (~1h)

**Ny fil**: `app\Brain\ConversationHistory.cs`

```csharp
namespace JarvisClean;

public static class ConversationHistory
{
    private const int MaxTurns = 20;
    private const int MaxTotalChars = 8000;
    private static readonly object _lock = new();
    private static readonly List<(string role, string content)> _turns = new();

    public static void AddUser(string text) { lock (_lock) { _turns.Add(("user", text)); Trim(); } }
    public static void AddAssistant(string text) { lock (_lock) { _turns.Add(("assistant", text)); Trim(); } }

    public static List<object> AsOllamaMessages()
    {
        lock (_lock) {
            return _turns.Select(t => (object)new { role = t.role, content = t.content }).ToList();
        }
    }

    public static int Count { get { lock (_lock) return _turns.Count; } }

    public static void Clear() { lock (_lock) _turns.Clear(); }

    private static void Trim()
    {
        // Behåll de första 2 (system-context) + senaste tills MaxTurns/MaxTotalChars
        while (_turns.Count > MaxTurns) _turns.RemoveAt(0);
        var totalChars = _turns.Sum(t => t.content.Length);
        while (totalChars > MaxTotalChars && _turns.Count > 4)
        {
            totalChars -= _turns[0].content.Length;
            _turns.RemoveAt(0);
        }
    }
}
```

**Ändra `AskOllamaAsync`**:
- Innan request: `ConversationHistory.AddUser(text)`
- Bygg messages-array: `system` + `ConversationHistory.AsOllamaMessages()`
- Efter svar: `ConversationHistory.AddAssistant(reply)`

**Slash-kommandon**:
- `/historik` → returnerar `ConversationHistory.AsOllamaMessages()` formaterad
- `/glöm samtal` → `ConversationHistory.Clear()`

Dessa går via CommandRouterV1 — nytt enum-värde + ParseSlashCommand-handling.

### B3. Naturligt språk → kod-edit (~2h)

**Ny fil**: `app\Brain\NaturalEditTool.cs`

Pipeline:
1. **Detektera intent** — regex i `HandleMessageAsync` innan Ollama-fallback:
   ```csharp
   var nlEditRx = new Regex(@"^(gå in i|öppna|ändra|fixa|uppdatera|refaktorera)\s+(?:filen?\s+)?([\w/\\.-]+\.\w+)\s*(?:och\s+)?(.+)$", RegexOptions.IgnoreCase);
   ```
2. **Läs fil** via `OllamaAgentHarness.ReadFile` (som redan finns)
3. **Generera ändring** med Coder-modellen, system-prompt:
   ```
   Du får en fil och en ändringsbeskrivning. Returnera ENDAST det fullständiga nya innehållet. Inga förklaringar.
   ```
4. **Diff** — använd `FileChangeReviewV1` som redan finns
5. **Pending approval** — `PendingApprovalV1.FileWrite` med diffen
6. **På godkänn** — write via existerande approval-flow

**Slash också**: `/edit <fil> = <beskrivning av ändring>`.

### B4. Builder-läge (~1h)

**Ny fil**: `app\Brain\BuilderMode.cs`

State machine:
- `Idle` → `Probing` (Jarvis ställer frågor) → `PlanReady` → `Building` (skapar filer) → `Done`

Slash `/bygg <beskrivning>`:
1. Initial fråga: kalla Smart-modellen med prompt `Användaren vill bygga: "{beskrivning}". Ställ 3-5 specifika klargörande frågor. Inga svar än, bara frågor.`
2. Användarens svar → spara i `_builderState`
3. Efter ~3 svar → generera `vault/builds/<slug>/PLAN.md` med fil-lista + arkitektur
4. Användaren skriver `/bygg fortsätt` → Jarvis skapar filerna en åt gången via PendingApproval

**Förenklat första pass**: bara generera PLAN.md, låt användaren sedan trigga skapande manuellt.

### B verifiering
- Skriv "fixa OllamaModel-konstanten i Program.cs så den heter qwen3:1.7b" → diff-popup visas
- Skriv "vad gör PendingApprovalV1?" 5 turns i rad → Jarvis kommer ihåg tidigare frågor
- Bygga: skriv "/bygg en TODO-app i HTML" → Jarvis ställer frågor

## Spår C — Internet-sökning

### C1 + C2 — Web-search + fetch (~2h)

**Ny fil**: `app\Brain\WebSearcher.cs`

```csharp
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
namespace JarvisClean;

// DuckDuckGo HTML scraping (gratis, ingen API-nyckel).
// Cache 24h i data/web_cache/<sha256>.json.
public static class WebSearcher
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task<List<(string title, string url, string snippet)>> SearchAsync(string query, int max = 5)
    {
        if (!await Program.JarvisForm.IsInternetOnlineCachedAsync()) return new();
        // duckduckgo.com/html?q=...
        var url = "https://duckduckgo.com/html/?q=" + Uri.EscapeDataString(query);
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Jarvis-clean)");
        try
        {
            var html = await Http.GetStringAsync(url);
            // Parse <a class="result__a" href="...">Title</a> + <a class="result__snippet">snippet</a>
            var results = new List<(string, string, string)>();
            var rx = new Regex(@"<a[^>]+class=""result__a""[^>]+href=""([^""]+)""[^>]*>([^<]+)</a>.*?<a[^>]+class=""result__snippet""[^>]*>(.*?)</a>", RegexOptions.Singleline);
            foreach (Match m in rx.Matches(html))
            {
                if (results.Count >= max) break;
                var u = Uri.UnescapeDataString(m.Groups[1].Value);
                if (u.StartsWith("//duckduckgo.com/l/?uddg=")) u = Uri.UnescapeDataString(u.Substring(25).Split('&')[0]);
                results.Add((StripHtml(m.Groups[2].Value), u, StripHtml(m.Groups[3].Value)));
            }
            return results;
        }
        catch { return new(); }
    }

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

**Säkerhetsregler från SUPERPLAN**:
- Hard kill: Ctrl+Shift+Alt+J avbryter ALL desktop-control
- Audit: alla actions till `data/desktop_actions.log` med thumbnail
- Rate limit: 30 actions/min, 200ms mellan
- Blacklist windows: Task Manager, Registry, cmd, PowerShell

## Test-mönster för Codex att följa

Varje ny tool ska ha:
1. **C#-router-test** i `tests/CommandRouterV1.Tests/Program.cs` — slash routing
2. **Node-test** i `tests/<feature>.test.js` — säkerhetschecker (path-traversal, whitelist, etc.)
3. **Build-verifiering**: `dotnet build app/JarvisClean.csproj -c Release` → 0 errors

Mönster:
```javascript
const fs = require("fs"); const path = require("path");
const program = fs.readFileSync(path.join(__dirname, "..", "app", "Program.cs"), "utf8");
let failures = 0;
const markers = ["MyToolName", "MyToolMethod"];
for (const m of markers) { if (!program.includes(m)) { failures++; console.log("FAIL: " + m); } else console.log("PASS: " + m); }
if (failures > 0) process.exit(1);
```

## MD-filer att uppdatera efter varje fas

1. `TODO_NEXT.md` — markera fas KLAR
2. `CURRENT_STATE.md` — lägg till sektion med datum
3. `vault/Decisions/DECISIONS_LOG.md` — logga viktiga beslut
4. `RELEASE_STATUS.md` — uppdaterad funktionsslista
5. `docs/SESSION_LOG.md` — sessionssammanfattning

## Verifierings-checklista per spår

### Efter B
- `dotnet build` → 0 errors
- Alla node + C# tester gröna
- Manuell: chatta 5 turns, se att Jarvis kommer ihåg tidigare frågor
- Manuell: skriv "fixa X i Program.cs" → diff-popup
- Modell-badge syns i chat-svar

### Efter C
- `/sök hello world` → 5 träffar i chat
- `/läs https://example.com` → sammanfattning
- Med wifi av: tydligt fel-meddelande

### Efter D
- `/öppna program notepad` → notepad öppnas
- Försök öppna ej-whitelistat program → blockerat
- `/skärm` → screenshot sparas
- `/desktop på` → bridge startar
- Ctrl+Shift+Alt+J → bridge dödas
- Försök klicka i blacklistad fönster → blockerat

## Beslut Codex behöver fatta (eller fråga användaren)

1. **WebSearcher: DuckDuckGo HTML scraping eller annan metod?** Rekommendation = DuckDuckGo (gratis, ingen API-nyckel).
2. **Vart sparas builds från Builder-mode?** Rekommendation = `vault/builds/<slug>/`
3. **UI-TARS: bygga från source eller använda npm-paket?** Rekommendation = subprocess via `pnpm start` om byggt, annars instruktion till användaren att bygga först.
4. **Multi-turn context: ska VaultSearcher kallas varje turn eller bara första?** Rekommendation = varje turn (minst overhead, mest context).
5. **Modell-routing: visa badge per svar eller dölja?** Rekommendation = visa.

## Kontakt-punkter med befintlig kod

- `Program.cs` `AskOllamaAsync` (rad ~4380) — multi-model + multi-turn + vault-context (vault klart, multi-model + multi-turn behövs)
- `Program.cs` `HandleMessageAsync` — naturligt-språk-edit hookas in före Ollama
- `Program.cs` `_activeModel` field — Override för ModelRouter
- `app/CommandRouterV1.cs` — alla nya slash-kommandon (B/C/D)
- `app/Brain/VaultSearcher.cs` — pattern för cached lookup
- `app/Bridges/NeuroLinkedBridge.cs` — pattern för subprocess-bridge

## Säkerhetsregler från användarens preferenser (`vault/Memory/Azu_preferences.md`)

- Inga lösenord i logs/chat/markdown
- F:\New project är read-only-referens
- All filskrivning via PendingApprovalV1
- Ingen fri F-disk-write-access
- Allt på svenska (default)

## Tidsestimat för Codex

- Spår B: 6 timmar (fördelat på 2-3 sessions)
- Spår C: 2 timmar
- Spår D: 10 timmar (fördelat på 3-4 sessions, sist eftersom mest komplext)

**Totalt**: ~18 timmar för Codex att slutföra Spår B+C+D.

## Vad som INTE ska göras

- Ändra `F:\New project\*` (utom ARCHIVED.md som finns)
- Auto-aktivera desktop-control (kräver explicit `/desktop på`)
- Skapa nya separata Windows-fönster (ETT program-regeln)
- Ta bort PendingApproval-skydd för någon write-path
- Bypass UI-TARS säkerhetsblacklists
- Lägga in API-nycklar i kod

## Slut

Användaren får läsa BÄDA dokumenten:
- `JARVIS_NEXT_LEVEL_SUPERPLAN.md` — översikt + säkerhet + beslut
- `CODEX_HANDOFF_NEXT_LEVEL.md` (denna) — exakt kod-skiss + nästa steg

Codex kan börja med Spår B1 (ModelRouter — minst risk, mest värde direkt).
