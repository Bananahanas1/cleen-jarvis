---
type: project-doc
source_file: "docs/MULTI_WINDOW_DESIGN.md"
created: 2026-05-10
tags: [project, mirrored]
---

# MULTI_WINDOW_DESIGN.md â€” Tre-fÃ¶nster-arkitekturen

Skapad: 2026-05-09
Driver: `docs\UNIFICATION_PLAN.md` Fas 3 (Brain) och Fas 4 (Explorer).

## Ã–versikt

Jarvis-clean blir en multi-window WinForms-app med **tre samtidiga fÃ¶nster** i samma C#-process. Main Ã¤r primÃ¤rt; Brain och File Explorer Ã¤r sekundÃ¤ra huvudskÃ¤rmar som kan Ã¶ppnas och stÃ¤ngas oberoende.

```
â”Œâ”€â”€â”€â”€ MAIN (primÃ¤r) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Project Explorer | Editor | Chat          â”‚
â”‚  Knappar: [Brain]  [File Explorer]         â”‚
â”‚           [Ã–versikt] [Terminal]            â”‚
â”‚  Status: â— Brain redo  â— Ollama  â— Online  â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
   â”‚                                  â”‚
   â”‚ klick "Brain"                    â”‚ klick "File Explorer"
   â–¼                                  â–¼
â”Œâ”€ BRAIN (sekundÃ¤r) â”€â”€â”€â”€â”€â”   â”Œâ”€ FILE EXPLORER (sekundÃ¤r) â”€â”€â”
â”‚  3D NeuroLinked        â”‚   â”‚  Tree | Multi-tab editor    â”‚
â”‚  HjÃ¤rnregioner         â”‚   â”‚  Multi-root: Jarvis-clean   â”‚
â”‚  Knowledge panel       â”‚   â”‚  + F:\New project read-only â”‚
â”‚  Live packets          â”‚   â”‚  SÃ¶k, filter                â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

## FÃ¶nster-livscykel

### Main
- Skapas av `Application.Run(new JarvisForm())` i `Program.cs`
- StÃ¤ng main â†’ hela appen stÃ¤ngs (alla sekundÃ¤ra fÃ¶nster ocksÃ¥, samt Python-server)
- Ã„r Ã¤gare av `NeuroLinkedBridge` (Python-server-livscykeln)

### Brain
- Skapas fÃ¶rsta gÃ¥ngen anvÃ¤ndaren klickar `Brain`-knappen eller skriver `/brain`
- HÃ¥lls vid liv mellan open/close (samma instans Ã¥teranvÃ¤nds) â€” snabb Ã¥terÃ¶ppning
- StÃ¤ng Brain â†’ fÃ¶nstret dÃ¶ljs (`Hide()`), Python-servern fortsÃ¤tter kÃ¶ra (always-on)
- Vid main-shutdown: `BrainWindow.Dispose()` anropas

### File Explorer
- Samma livscykel-mÃ¶nster som Brain
- Skapas vid fÃ¶rsta `/explorer`-klick
- Multi-root state hÃ¥lls i fÃ¶nstret, persistas till `config\explorer.json`

## Inter-window-kommunikation

Alla fÃ¶nster delar samma `JarvisForm`-instans som hub. SekundÃ¤ra fÃ¶nster hÃ¥ller en referens till main:

```csharp
public sealed class BrainWindow : Form
{
    private readonly JarvisForm _main;
    public BrainWindow(JarvisForm main) { _main = main; ... }
}
```

**Pattern**:
- SekundÃ¤r â†’ Main: direkt metodanrop pÃ¥ `_main` (samma trÃ¥d, samma process)
- Main â†’ SekundÃ¤r: event eller `_brainWindow?.UpdateState(...)`
- Dashboard JS i sekundÃ¤rt fÃ¶nster â†’ C# via `WebView2.WebMessageReceived` (samma som main)
- SekundÃ¤r WebView â†’ Main UI: lyfts via C# (sekundÃ¤r postar JSON, main fÃ¥r meddelande, main uppdaterar sin egen UI)

## SÃ¤kerhetsregler fÃ¶r sekundÃ¤ra fÃ¶nster

Alla regler frÃ¥n Main gÃ¤ller Ã¤ven Brain och Explorer:

1. **PendingApprovalV1** fÃ¶r all filskrivning (Ã¤ven frÃ¥n Explorer-fÃ¶nstret).
2. **CommandRouterV1** fÃ¥ngar alla kommandon (Ã¤ven frÃ¥n Brain knowledge-panel).
3. **F:\New project Ã¤r read-only** â€” Explorer kan visa, inte skriva.
4. **Inga lÃ¶senord/API-nycklar** i nÃ¥gon UI-yta.
5. **PowerShell/terminal-kÃ¶rning** krÃ¤ver fortfarande pending approval.

## Tekniska detaljer

### WebView2 i flera fÃ¶nster

Varje WebView2-instans behÃ¶ver eget eller delat user-data-folder. Vi anvÃ¤nder **delad** user-data-folder sÃ¥ cookies, cache och localStorage delas mellan fÃ¶nstren:

```csharp
const string SharedUserDataFolder = @"F:\DevCache\WebView2\Jarvis";
```

Detta ger:
- Snabbare start av sekundÃ¤ra fÃ¶nster (cache redan varm)
- Konsekvent state (om en sida sÃ¤tter localStorage, ser alla)
- Samma `CoreWebView2Environment` Ã¥teranvÃ¤nds via `await CoreWebView2Environment.CreateAsync(SharedUserDataFolder)`

### Z-order och alltid-pÃ¥-toppen

- Default: alla fÃ¶nster Ã¤r vanliga (kan placeras under andra).
- AnvÃ¤ndarinstÃ¤llning per fÃ¶nster: `[ ] Alltid pÃ¥ toppen` (standard âœ—).
- Brain-fÃ¶nstret kan vara nyttigt pÃ¥ en separat skÃ¤rm â€” `RestoreBounds` persistas per fÃ¶nster i `config\windows.json`.

### StÃ¤ng-beteende

| FÃ¶nster | StÃ¤ng-knapp | Beteende |
|---------|-------------|----------|
| Main | Ã— | App stÃ¤ngs, alla sekundÃ¤ra stÃ¤ngs, Python-server stoppas |
| Brain | Ã— | FÃ¶nstret dÃ¶ljs (`e.Cancel = true; this.Hide();`) |
| File Explorer | Ã— | FÃ¶nstret dÃ¶ljs pÃ¥ samma sÃ¤tt |

AnvÃ¤ndaren kan tvinga full stÃ¤ngning via `File â†’ Avsluta` i sekundÃ¤rt fÃ¶nster (via `_actuallyClose = true; Close();`).

## Planering och fas-mappning

| Fas i UNIFICATION_PLAN | Vad bygger vi |
|------------------------|---------------|
| Fas 3 | `BrainWindow.cs` + `dashboard\brain.html` + Three.js statisk |
| Fas 4 | `FileExplorerWindow.cs` + `dashboard\explorer.html` |
| Fas 5 | `NeuroLinkedBridge.cs` + always-on Python â†’ Brain blir levande |

## Test-tÃ¤ckning

Nya tester (skapas under Fas 3-5):
- `tests\brain-window.test.js` â€” laddningskontrakt, fallback om Three.js saknas
- `tests\file-explorer-window.test.js` â€” multi-root, read-only-blockering
- `tests\multi-window-lifecycle.test.js` â€” open/close/Ã¥terÃ¶ppning
- `tests\neurolinked-bridge.test.js` â€” start/stop/timeout/offline-fallback

## Risker och mitigeringar

| Risk | Mitigering |
|------|------------|
| SekundÃ¤rt fÃ¶nster fryser, blockerar main | Egen trÃ¥d via `Application.Run` per fÃ¶nster Ã¤r fel; vi kÃ¶r samma UI-trÃ¥d men WebView2 Ã¤r async sÃ¥ den blockerar inte. Bridges anvÃ¤nder `await` Ã¶verallt. |
| Brain WebView blir tung | safe-mode `?safe=1` URL-flagga som hoppar Ã¶ver WebGL |
| State-konflikt mellan fÃ¶nster | Single source of truth: main hÃ¥ller state, sekundÃ¤ra frÃ¥gar via metodanrop |
| AnvÃ¤ndaren stÃ¤nger main nÃ¤r Brain Ã¤r Ã¶ppet | Main `OnClosing` stÃ¤nger alla sekundÃ¤ra fÃ¶rst, sedan Python, sedan sig sjÃ¤lv |

