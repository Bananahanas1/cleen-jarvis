# TODO_NEXT PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

# TODO_NEXT.md — Nästa praktiska steg

Senast uppdaterad: 2026-05-10 (D1/D3/D4 UI-TARS desktop-control)

## Aktiv plan: Unifiering F:\Jarvis-clean + F:\New project

Detaljer: `docs\UNIFICATION_PLAN.md`. Nuvarande fas: **Fas 1 — Slutför baseline**.

### Fas 0 — MD-uppdatering (KLAR 2026-05-09)
- [x] MASTER_PLAN.md, AGENTS.md, BUILD_PLAN.md, CURRENT_STATE.md, TODO_NEXT.md uppdaterade
- [x] docs\UNIFICATION_PLAN.md skapad

### Fas 1 — Slutför baseline (KLAR 2026-05-09)
- [x] **1.1** Lägg till `senaste build-status` + `senaste minnesförändring` i Jarvis Översikt
- [x] **1.2** Bygg ut named checkpoints: `/checkpoint skapa <namn>`, `/checkpoint lista`, `/checkpoint återställ <namn>`
- [x] **1.3** InternetProbe i C# (cachad TCP-koll mot 1.1.1.1:443, 800ms timeout, 30s cache)
- [x] **1.4** Initial test harness: unit tests för CommandValidatorV1 + integration tests för PendingApprovalV1

### Fas 2 — Vendor (KLAR 2026-05-10)
- [x] Three.js-vendor kopierad till `dashboard\vendor\` (1.4 MB)
- [x] `graphify-out\graph.json` kopierad (114 MB, 63052 noder, 189022 edges)
- [x] Test: `tests\vendor-assets.test.js`

### Fas 3 — Brain-fönster (KLAR 2026-05-10)
- [x] `dashboard\brain.html` med 11 hjärnregioner + Three.js
- [x] `app\BrainWindow.cs` med SetVirtualHostNameToFolderMapping (jarvis.local)
- [x] Slash `/brain`, naturligt språk "öppna hjärnan"
- [x] Test: `tests\brain-window.test.js`

### Fas 4 — File Explorer-fönster (KLAR 2026-05-10)
- [x] `dashboard\explorer.html` med multi-root tree + multi-tab editor + filter
- [x] `app\FileExplorerWindow.cs` med tree-listing, file-open, ResolveSafePath
- [x] Multi-root: clean (rw via approval) + newproject (read-only)
- [x] Slash `/explorer`
- [x] Test: `tests\file-explorer-window.test.js`

### Fas 5 — Python NeuroLinked always-on (KLAR 2026-05-10)
- [x] `neurolinked\` (74 filer) + `python\` (10 filer) kopierade
- [x] `app\Bridges\NeuroLinkedBridge.cs` med ResolvePython, StartAsync, StopAsync, IsAliveAsync
- [x] Auto-start vid main OnLoad, auto-stop vid OnMainFormClosing
- [x] Status-chip "Brain: redo/startar/ej tillgänglig" i Översikt
- [x] Offline-graceful (Python saknas → main fortsätter)
- [x] Test: `tests\neurolinked-bridge.test.js`

### Fas 6 — OllamaAgentHarness read-only (KLAR 2026-05-10)
- [x] `app\Agents\OllamaAgentHarness.cs` med 5 read-tools (read_file, list_files, glob_files, grep_text, get_project_context)
- [x] Skrivverktyg (write_file, replace_in_file, run_command, etc.) **explicit blockerade** med pekare till /fil skapa
- [x] Slash `/agent <task>`
- [x] Path-traversal-guard
- [x] Test: `tests\ollama-agent-safety.test.js`

### Fas 7 — ModelCatalog (KLAR 2026-05-10)
- [x] `app\Core\ModelCatalog.cs` med 5 profiler (qwen3:1.7b, qwen3:8b, qwen2.5-coder:7b, deepseek-r1:7b, llama3.1:8b)
- [x] `_activeModel` i JarvisForm + persistens till `config\model.txt`
- [x] Slash `/modell` (lista) och `/modell byt <name|role>` (byte)
- [x] Auto-upgrade fast→coder för agent-tasks
- [x] 7 nya C#-tester

### Fas 8 — Cleanup + slutverifiering (KLAR 2026-05-10)
- [x] Uppdatera TODO_NEXT.md med slutgiltiga statuskryss
- [x] Uppdatera CURRENT_STATE.md, RELEASE_STATUS.md
- [x] Skapa `docs\MIGRATION_FROM_NEW_PROJECT.md`
- [x] `F:\New project\ARCHIVED.md` skapad (markerad som arkiverad referens)
- [x] Full regressionstest

---

## BR-faser — efter användarfeedback 2026-05-10

Ny riktning: ETT program (inga lösa fönster), Brain som inbyggd vy, Project Explorer = the explorer, vault som AI-kontext. Detaljer: `docs\BRAIN_3D_SUPERPLAN.md`.

### Refaktor 1 — Embedded Brain + Explorer borttagen (KLAR)
- [x] BrainWindow.cs och FileExplorerWindow.cs borttagna
- [x] Brain är nu en inbäddad panel i mittpanelen (knapp "Brain")
- [x] Explorer-vyn helt borta — Project Explorer (vänster) är "the" explorer
- [x] Brain-mode CSS döljer Project Explorer + edit/save-knappar
- [x] Main-fönstret använder virtual host (jarvis.local) → Three.js från lokal vendor

### BR0 — FileGraphBuilder bugfix + vault-skanning (KLAR)
- [x] Fix `__init__.py` duplicate-key crash (ToLookup istället för ToDictionary)
- [x] Vault-pre-check: hoppa MD utan `[[` eller `source_file:` (29s → 1.4s)
- [x] Cache till `.checkpoints/.brain-graph-cache.json` (5 min TTL)
- [x] Frontend-timeout 8s → 60s
- [x] FileGraphBuilder skannar både projekt-filer och vault-noter

### BR1 — Visual NeuroLinked-stil (KLAR)
- [x] UnrealBloomPass post-processing för glow
- [x] Stjärn-bakgrund (800 punkter)
- [x] ACES Filmic tone-mapping
- [x] 4 sci-fi glas-paneler (Stats, Filter, Sök, Inspector)
- [x] Pulse via SCALE-animation per nod (syns även utzoomat)
- [x] Filter-checkboxar per filtyp
- [x] Live-sök i path/label
- [x] "Bygg om" knapp som rensar cache

### BR-feedback — pulser + zoom + bg + orphans (KLAR 2026-05-10)
- [x] Helt svart bg (0x000000) — ren rymd
- [x] Fog borttagen så grafen syns utzoomat
- [x] `controls.zoomToCursor = true` — zoom följer mus
- [x] Pulse via scale (±18%) med slumpmässig fas per nod — syns alltid
- [x] Implicita folder-edges för orphans → klumpas per mapp i fysiken
- [x] Tooltip på "Bygg om"-knappen

### Vault-struktur (KLAR 2026-05-10)
- [x] `vault/Index.md` med [[wikilinks]] till allt
- [x] `vault/Project/` (UNIFICATION_PLAN, MULTI_WINDOW_DESIGN, MIGRATION, BRAIN_3D_SUPERPLAN, CURRENT_STATE)
- [x] `vault/Memory/Azu_preferences.md`
- [x] `vault/Decisions/DECISIONS_LOG.md`
- [x] `vault/Issues/Brain_visual_polish.md`, `Vault_AI_context.md`
- [x] Vault-scope reducerat till bara `F:\Jarvis-clean\vault\`

### BR2 + BR6 — Vault som AI-kontext (KLAR 2026-05-10)
- [x] `app/Brain/VaultSearcher.cs` med ord-frekvens-scoring + titel-boost
- [x] Cache med invalidation om någon `.md` ändrats
- [x] Excerpts runt första query-träffen (max 600 tecken/not, 4000 totalt)
- [x] Auto-läsning ON default — `BuildContextPrefix` injiceras i AskOllamaAsync
- [x] Slash: `/vault status`, `/vault sök <q>`, `/vault skapa <namn> = <text>`, `/vault på`, `/vault av`
- [x] Översikt-cell "Vault (AI-kontext)" med live-status
- [x] Säkerhetsfix: `/vault skapa` går nu via `PendingApprovalV1`/`FileCreate` preview i stället för direkt `File.WriteAllText`; godkända vault-skrivningar invalidaterar vault-index.

### B1 + B2 + C1 + D2 — Conversational + Web + Programs (KLAR 2026-05-10 sent)
- [x] **B1 ModelRouter** (`app/Brain/ModelRouter.cs`): auto-routing kod/reason/smart/fast + badge i svar
- [x] **B2 ConversationHistory** (`app/Brain/ConversationHistory.cs`): sliding window 20 turns / 8000 tecken
- [x] **C1 WebSearcher** (`app/Brain/WebSearcher.cs`): Google + Opera (per användarval), `/sök` öppnar SERP, `/läs` hämtar+sammanfattar
- [x] **D2 SafeAppLauncher** (`app/Desktop/SafeAppLauncher.cs`): whitelist (notepad/vscode/chrome/opera/...), audit-log, inga argument
- [x] Slash-kommandon: `/sök`, `/läs`, `/öppna program`, `/lista program`, `/historik`, `/glöm samtal`
- [x] Autocomplete + hjälptext uppdaterad
- [x] Test: `tests/b1-b2-c1-d2.test.js` (50+ checks gröna)
- [x] Verifiering: 28 node + 63 C# tester gröna

### B3 — NaturalEditTool (FÖRSTA PASS KLART 2026-05-10)
- [x] `app\Brain\NaturalEditTool.cs` med naturlig fras-parser, prompt-builder och code-fence cleanup.
- [x] Slash `/edit <fil> = <beskrivning>` routear lokalt via `CommandRouterV1`.
- [x] Naturliga fraser som `gå in i docs/test.md och gör texten tydligare` fångas före smart-open.
- [x] Ändringen genereras med `qwen2.5-coder:7b` som komplett nytt filinnehåll.
- [x] Resultatet läggs som `PendingApprovalV1.FileWrite`; ingen direkt filskrivning.
- [x] Test: `tests\natural-edit-tool.test.js` + 5 C#-tester i `CommandRouterV1.Tests`.
- [ ] Manual UI-test efter publish/restart: `/edit docs/test.md = gör texten tydligare` ska visa pending popup.

### B4 — BuilderMode (FÖRSTA PASS KLART 2026-05-10)
- [x] `app\Brain\BuilderMode.cs` med runtime-session, safe slug, frågor/svar och plan-markdown.
- [x] Slash `/bygg <idé>` startar lokalt BuilderMode och ställer 3-5 klargörande frågor via Smart-modellen.
- [x] Slash `/bygg svar <svar>` sparar användarens svar i aktiv builder-session.
- [x] Slash `/bygg plan` genererar `vault/builds/<slug>/PLAN.md` som pending `FileCreate` via `CreateProjectFileRequestTool`.
- [x] Slash `/bygg status` och `/bygg avbryt` hanterar sessionen lokalt.
- [x] Inga builder-filer skrivs direkt; plan sparas först efter användarens approval-popup.
- [x] Test: `tests\builder-mode.test.js` + C#-router/validator-tester i `CommandRouterV1.Tests`.
- [ ] Manual UI-test efter publish/restart: `/bygg en liten todo-app i HTML` → frågor.
- [ ] Manual UI-test: `/bygg svar enkel HTML, localStorage, mörkt UI` → svar räknas i `/bygg status`.
- [ ] Manual UI-test: `/bygg plan` → pending popup för `vault/builds/<slug>/PLAN.md`.

### Autocomplete-uppdatering (KLAR 2026-05-10)
- [x] `slashCommandSuggestionsV14` har ALLA nya kommandon: /brain, /agent, /modell (+5 shortcuts), /vault (+5 shortcuts), /checkpoint (+3 shortcuts)
- [x] `commandBaseSuggestions` (V10) har nya naturligt-språk-grupper för brain/agent/vault
- [x] Skriv `/` → första 12 listas; TAB cyklar
- [x] Substring-matchning för fuzzy: `/brn` matchar `/brain`

### D1/D3/D4 — UI-TARS desktop-control (SAFE PASS KLART 2026-05-10)
- [x] `app\Bridges\UiTarsBridge.cs` med local-source detection, UI-TARS Desktop subprocess start/stop och OpenAI-kompatibel vision endpoint.
- [x] `app\Desktop\ScreenCapture.cs` med `/skärm`.
- [x] `app\Desktop\DesktopActionRequestV1.cs` med UI-TARS action parser: click, double_click, right_click, hover, drag, type, hotkey, scroll, finished.
- [x] `app\Desktop\DesktopActionGate.cs` med default OFF, foreground blacklist, rate limit och audit log.
- [x] `app\Desktop\DesktopActionExecutor.cs` kör click/type/scroll/drag/hotkey först efter approval.
- [x] `PendingApprovalTypeV1.DesktopAction` och popup-stöd.
- [x] Ctrl+Shift+Alt+J hard-kill.
- [x] Slash: `/desktop status`, `/desktop på`, `/desktop av`, `/desktop tars start`, `/desktop tars stop`, `/skärm`, `/desktop klick`, `/desktop skriv`, `/desktop fråga`.
- [x] Test: `tests\desktop-control.test.js` + C#-router/parser-tester.
- [ ] Manual UI-test efter publish/restart: `/desktop status`.
- [ ] Manual UI-test: `/desktop på`, `/skärm`, `/desktop klick 100 200` och avbryt popup.
- [ ] För vision: lägg egen UI-TARS-kompatibel API-konfig i env eller `config\uitars.json` innan `/desktop fråga ...`.
