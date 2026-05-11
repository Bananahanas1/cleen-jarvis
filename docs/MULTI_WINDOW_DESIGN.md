# MULTI_WINDOW_DESIGN.md — Tre-fönster-arkitekturen

Skapad: 2026-05-09
Driver: `docs\UNIFICATION_PLAN.md` Fas 3 (Brain) och Fas 4 (Explorer).

## Översikt

Jarvis-clean blir en multi-window WinForms-app med **tre samtidiga fönster** i samma C#-process. Main är primärt; Brain och File Explorer är sekundära huvudskärmar som kan öppnas och stängas oberoende.

```
┌──── MAIN (primär) ─────────────────────────┐
│  Project Explorer | Editor | Chat          │
│  Knappar: [Brain]  [File Explorer]         │
│           [Översikt] [Terminal]            │
│  Status: ● Brain redo  ● Ollama  ● Online  │
└────────────────────────────────────────────┘
   │                                  │
   │ klick "Brain"                    │ klick "File Explorer"
   ▼                                  ▼
┌─ BRAIN (sekundär) ─────┐   ┌─ FILE EXPLORER (sekundär) ──┐
│  3D NeuroLinked        │   │  Tree | Multi-tab editor    │
│  Hjärnregioner         │   │  Multi-root: Jarvis-clean   │
│  Knowledge panel       │   │  + F:\New project read-only │
│  Live packets          │   │  Sök, filter                │
└────────────────────────┘   └─────────────────────────────┘
```

## Fönster-livscykel

### Main
- Skapas av `Application.Run(new JarvisForm())` i `Program.cs`
- Stäng main → hela appen stängs (alla sekundära fönster också, samt Python-server)
- Är ägare av `NeuroLinkedBridge` (Python-server-livscykeln)

### Brain
- Skapas första gången användaren klickar `Brain`-knappen eller skriver `/brain`
- Hålls vid liv mellan open/close (samma instans återanvänds) — snabb återöppning
- Stäng Brain → fönstret döljs (`Hide()`), Python-servern fortsätter köra (always-on)
- Vid main-shutdown: `BrainWindow.Dispose()` anropas

### File Explorer
- Samma livscykel-mönster som Brain
- Skapas vid första `/explorer`-klick
- Multi-root state hålls i fönstret, persistas till `config\explorer.json`

## Inter-window-kommunikation

Alla fönster delar samma `JarvisForm`-instans som hub. Sekundära fönster håller en referens till main:

```csharp
public sealed class BrainWindow : Form
{
    private readonly JarvisForm _main;
    public BrainWindow(JarvisForm main) { _main = main; ... }
}
```

**Pattern**:
- Sekundär → Main: direkt metodanrop på `_main` (samma tråd, samma process)
- Main → Sekundär: event eller `_brainWindow?.UpdateState(...)`
- Dashboard JS i sekundärt fönster → C# via `WebView2.WebMessageReceived` (samma som main)
- Sekundär WebView → Main UI: lyfts via C# (sekundär postar JSON, main får meddelande, main uppdaterar sin egen UI)

## Säkerhetsregler för sekundära fönster

Alla regler från Main gäller även Brain och Explorer:

1. **PendingApprovalV1** för all filskrivning (även från Explorer-fönstret).
2. **CommandRouterV1** fångar alla kommandon (även från Brain knowledge-panel).
3. **F:\New project är read-only** — Explorer kan visa, inte skriva.
4. **Inga lösenord/API-nycklar** i någon UI-yta.
5. **PowerShell/terminal-körning** kräver fortfarande pending approval.

## Tekniska detaljer

### WebView2 i flera fönster

Varje WebView2-instans behöver eget eller delat user-data-folder. Vi använder **delad** user-data-folder så cookies, cache och localStorage delas mellan fönstren:

```csharp
const string SharedUserDataFolder = @"F:\DevCache\WebView2\Jarvis";
```

Detta ger:
- Snabbare start av sekundära fönster (cache redan varm)
- Konsekvent state (om en sida sätter localStorage, ser alla)
- Samma `CoreWebView2Environment` återanvänds via `await CoreWebView2Environment.CreateAsync(SharedUserDataFolder)`

### Z-order och alltid-på-toppen

- Default: alla fönster är vanliga (kan placeras under andra).
- Användarinställning per fönster: `[ ] Alltid på toppen` (standard ✗).
- Brain-fönstret kan vara nyttigt på en separat skärm — `RestoreBounds` persistas per fönster i `config\windows.json`.

### Stäng-beteende

| Fönster | Stäng-knapp | Beteende |
|---------|-------------|----------|
| Main | × | App stängs, alla sekundära stängs, Python-server stoppas |
| Brain | × | Fönstret döljs (`e.Cancel = true; this.Hide();`) |
| File Explorer | × | Fönstret döljs på samma sätt |

Användaren kan tvinga full stängning via `File → Avsluta` i sekundärt fönster (via `_actuallyClose = true; Close();`).

## Planering och fas-mappning

| Fas i UNIFICATION_PLAN | Vad bygger vi |
|------------------------|---------------|
| Fas 3 | `BrainWindow.cs` + `dashboard\brain.html` + Three.js statisk |
| Fas 4 | `FileExplorerWindow.cs` + `dashboard\explorer.html` |
| Fas 5 | `NeuroLinkedBridge.cs` + always-on Python → Brain blir levande |

## Test-täckning

Nya tester (skapas under Fas 3-5):
- `tests\brain-window.test.js` — laddningskontrakt, fallback om Three.js saknas
- `tests\file-explorer-window.test.js` — multi-root, read-only-blockering
- `tests\multi-window-lifecycle.test.js` — open/close/återöppning
- `tests\neurolinked-bridge.test.js` — start/stop/timeout/offline-fallback

## Risker och mitigeringar

| Risk | Mitigering |
|------|------------|
| Sekundärt fönster fryser, blockerar main | Egen tråd via `Application.Run` per fönster är fel; vi kör samma UI-tråd men WebView2 är async så den blockerar inte. Bridges använder `await` överallt. |
| Brain WebView blir tung | safe-mode `?safe=1` URL-flagga som hoppar över WebGL |
| State-konflikt mellan fönster | Single source of truth: main håller state, sekundära frågar via metodanrop |
| Användaren stänger main när Brain är öppet | Main `OnClosing` stänger alla sekundära först, sedan Python, sedan sig själv |
