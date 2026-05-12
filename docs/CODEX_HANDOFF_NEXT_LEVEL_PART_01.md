# CODEX_HANDOFF_NEXT_LEVEL PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

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
