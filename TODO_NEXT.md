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

## Manuella tester användaren bör göra
- `/brain` — pulserande 3D-graf med projekt-filer + vault, orphans klumpade per mapp
- Klicka projekt-nod → öppnar filen i Files-läget
- `/vault sök <ord>` — topp 10 träffar i chat
- `/vault skapa test-not = hej från Jarvis` — ska visa pending approval; filen ska först skapas efter godkännande
- Skriv normal chat-fråga → Jarvis ska svara med vault-kontext (titta i Översikt: "senaste svar använde N noter")
- `/vault på` / `/vault av` — toggle auto-kontext
- `/modell snabb`, `/modell kod` etc — byter modell
- `/edit docs/test.md = gör texten tydligare` — ska skapa pending edit-preview, inte skriva direkt
- `gå in i docs/test.md och gör texten tydligare` — ska skapa samma pending edit-preview, inte bara öppna filen
- `/bygg en liten todo-app i HTML` — ska ställa frågor, inte skriva filer direkt
- `/bygg svar enkel HTML, localStorage, mörkt UI` — ska spara svaret i aktiv session
- `/bygg plan` — ska skapa pending `FileCreate` för `vault/builds/<slug>/PLAN.md`
- `/desktop status` — ska visa UI-TARS/desktop state
- `/desktop på` — aktiverar desktop-control, men actions kräver fortfarande approval
- `/skärm` — sparar screenshot i `data/screenshots`
- `/desktop klick 100 200` — ska visa pending desktop-action popup; testa först med Avbryt
- Ctrl+Shift+Alt+J — hard-kill desktop-control

## Återstående framtida arbete
- BR3: Project Explorer ↔ Brain sync (klick på fil → highlight nod)
- BR4: avancerat filter (knappen "Bygg om" finns redan)
- Auto-promotion: efter `kom ihåg` → även spara i `vault/auto/`
- Bind FileExplorer-write till PendingApprovalV1-popup
- Skrivverktyg i OllamaAgentHarness via PendingApproval
- Verifiera neurolinked/server.py respekterar JARVIS_BIND_HOST
- BuilderMode nästa fas: skapa filer från godkänd plan stegvis via PendingApproval
- D1/D3/D4 nästa polish: thumbnail i approval-popup och bättre multi-monitor-stöd

## Current next steps

Prioritera stabilisering och små fokuserade förbättringar. Fortsätt inte om från redan klara slash-/pending-/smart-open-steg.

- [ ] Manually verify terminal routing/cancel/terminal panel in Jarvis UI.
- [ ] Manually verify pending file write/append/delete/undo in Jarvis UI.
- [x] Improve Project Explorer tree polish (active-file/active-folder highlight + persistent active path across re-render).
- [ ] Manually verify active-file/active-folder highlight in Project Explorer after opening a file.
- [x] Add active folder + latest file change cells to Jarvis Översikt.
- [ ] Manually verify Översikt shows active folder + latest file change live.
- [ ] Add latest build status + latest memory change cells to Jarvis Översikt later.
- [x] Add `=` as preferred separator in file commands (`/fil skapa path = text`, `skriv fil: path = text`, `föreslå rubrik: path = h`, etc.). `|` keeps working as fallback. Helper `CommandRouterV1.SplitFileCommandArguments` chooses whichever separator appears first.
- [x] Publish/restart efter `=`-separator slutförd. Användaren stängde gamla Jarvis manuellt; ny Jarvis igång som PID 74224 (SessionId 11). Kommandona `/fil skapa docs/test.md = text`, `skriv fil: docs/test.md = text` etc. är aktiva i UI:t.
- [x] TAB cyclar mappar för `/fil skapa` och `skapa fil:`; SPACE låser valt förslag så användaren kan skriva filnamn + extension + `=` + content fritt.
- [x] Färgkodade autocomplete-rader: kommandoprefix vit, mappar gula, filer gröna.
- [ ] Manually verify: TAB→cycle folders, SPACE→lock, type rest, Enter; verify yellow/green/white colors.
- [x] Build file panel edit mode with pending save in source.
- [x] Improve terminal transcript formatting: chat response now stays compact and full output stays in Terminal-panel.
- [x] Add explicit `/fil skapa` with pending approval in source.
- [ ] Add named checkpoint/history beyond one-step undo.
- [ ] Build `.jarvis/tasks` task workspace later.
- [ ] Build worker delegation later; workers read/summarize/propose only.
- [ ] Add local Ollama/Claude Code setup docs/scripts later.
- [x] Replace confusing `Visual Lab` wording with a practical `Jarvis Översikt` panel.
- [x] Start safe Obsidian/minne direction with local `/obsidian status`, `/minne status` and `/översikt`.
- [x] Make dashboard scrollbars dark so Project Explorer, editor, chat, terminal, diff and popup fit the theme.
- [x] Review experimental `JarvisCLI`/`PocketBridge` additions and keep them out of main compile scope.
- [ ] Keep real 3D later and off by default.
- [x] Research `F:\Free Jarvis` as read-only inspiration and document findings.
- [ ] Create `docs/VOICE_MODE_PLAN.md` later if the user wants voice mode.
- [ ] Keep Free Jarvis reference-only unless license/permission is clarified.

## Recent source work completed 2026-05-06

- [x] File panel `Edit-läge` now unlocks the textarea for opened files.
- [x] File panel `Spara med godkännande` creates a pending `FileWrite` approval instead of writing directly.
- [x] Truncated/too-long file previews are blocked from edit/save to avoid overwriting files with partial content.
- [x] `/fil skapa docs/test.md | text` creates a pending `FileCreate` approval.
- [x] `skapa fil: docs/test.md | text` routes locally and is not treated as smart file-open.
- [x] File-create approval creates undo/review metadata after approval.
- [x] Jarvis Översikt added as a separate panel from Workspace Panel.
- [x] Jarvis Översikt shows active file, pending approval, latest terminal, memory state, Obsidian state and the safe Jarvis loop.
- [x] `/översikt`, `/minne status` and `/obsidian status` route locally and do not go to Ollama.
- [x] Latest runtime publish/restart completed; Jarvis.exe PID 66812 observed.
- [x] Added scrollbar style regression test for the dashboard.
- [x] Added app project scope regression test so nested experimental C# source does not break main build.
- [x] Pending approval hint added near chat input.
- [x] Change review labels now distinguish create/write/append/delete/undo.
- [x] `docs\VISUAL_PANEL_PLAN.md` documents that visual layers are panels, not the whole app.

Manual verification after publish/restart:

- [ ] `/fil skapa docs/test-create.md | HEJ FRÅN CREATE`
- [ ] Approve popup; expected file is created and review bar appears.
- [ ] Click `Ångra`; approve undo; expected created file is removed.
- [ ] Open an existing safe file, click `Edit-läge`, edit text, click `Spara med godkännande`.
- [ ] Cancel editor-save popup; expected file remains unchanged.
- [ ] Repeat editor-save and approve; expected review bar appears.

## Recent UI polish completed 2026-05-05

- [x] `vad stod i terminalen` now returns command, working directory, exit code, timeout and summary only, then points to Terminal-panel for full output.
- [x] Delete review bar can display `1 fil raderad +0 -N` when C# sends delete change kind.
- [x] Autocomplete hides `avbryt kör` when there is no pending terminal run.
- [x] Autocomplete shows `avbryt kör` when a terminal run approval popup is active.

## Reference research completed 2026-05-06

- [x] `F:\Free Jarvis` inspected with static/read-only methods.
- [x] `ProjectPixel.exe` was not run.
- [x] `.env` values were not copied; only variable names were documented.
- [x] Findings saved in `docs\FREE_JARVIS_RESEARCH.md`.
- [x] Useful ideas captured for later Voice Mode, TTS/STT cache and provider-safety design.

Remaining polish:

- [x] Latest runtime publish/restart completed so UI can test Visual Lab and pending hint.
- [x] Make review/undo bar support richer labels for create/write/append/delete/undo states.
- [x] Add better visual state for pending approval type near the input.

## Current manual UI tests

Kör i Jarvis UI:

- [ ] `visa terminal`
- [ ] `vad stod i terminalen`
- [ ] `avbryt`
- [ ] `terminal preview: dotnet build`
- [ ] Approve terminal popup and confirm Terminal-panel receives full output.
- [ ] `terminal preview: dotnet build`, then `avbryt`
- [ ] `skriv fil: docs/test-agent.md | TESTAR PENDING APPROVAL`
- [ ] Cancel file write popup and confirm file is unchanged.
- [ ] Approve file write popup and confirm review bar appears.
- [ ] Click `Granska ändringar`.
- [ ] Click `Ångra` and approve undo.
- [ ] `radera fil: docs/test-agent.md` should create pending delete popup, not delete directly.
- [ ] `öppna tests/terminal-approval-safety.test.js`
- [ ] `/fil öppna tests/terminal-approval-safety.test.js`
- [ ] Click `Översikt`; expected: panel shows memory/Obsidian/Jarvis-loop state.
- [ ] Type `/översikt`; expected: middle panel opens Jarvis Översikt.
- [ ] Type `/minne status`; expected: local memory counts, no Ollama.
- [ ] Type `/obsidian status`; expected: safe read-only status, no vault write.

## Current architecture priorities

- Keep local commands before Ollama.
- Keep risky actions behind `PendingApprovalV1`.
- Keep smart-open centralized; do not add V8/V9 patch layers.
- Keep `F:\New project` read-only.
- Keep other F-drive roots read-only by default.
- Keep docs updated after successful runtime changes.
- Publish/restart is allowed after successful runtime changes and tests.
- Do not publish/restart for docs-only work.

## Completed / old history

These items were once TODOs but are now implemented or partially implemented according to `docs\SESSION_LOG.md` and `CURRENT_STATE.md`.

### Safe dashboard and local basics

- [x] Safe dashboard starts without freezing.
- [x] C# WinForms/WebView2 app starts Jarvis dashboard.
- [x] JavaScript to C# bridge works.
- [x] Local calculator/tooling exists.
- [x] Local Ollama chat exists.
- [x] Local markdown memory exists.
- [x] Smart Memory commands exist.
- [x] Diskvakt commands exist.
- [x] Model management exists.

### Offline Codex / file safety

- [x] Safe file read exists.
- [x] Safe file write/append requests exist.
- [x] File writes/appends moved behind PendingApproval.
- [x] Missing file write does not create new files silently.
- [x] File delete requests moved behind PendingApproval.
- [x] File type/path safety exists for local file tools.
- [x] Change review/diff UI V1 exists.
- [x] Close button for review bar exists.
- [x] One-step undo V1 exists for latest approved file write/append/delete.

### CommandRouter / slash commands

- [x] `CommandRouterV1` exists.
- [x] `CommandValidatorV1` exists.
- [x] `ToolRegistryV1` exists.
- [x] `PendingApprovalV1` exists.
- [x] `/hjälp` and `/status` route locally.
- [x] `/minne visa`, `/minne viktiga`, `/minne projekt`, `/minne sök`, `/minne arkiv sök` route locally.
- [x] `/fil öppna` and `/fil läs` route locally.
- [x] `/terminal preview`, `/terminal godkänn`, `/terminal avbryt`, `/terminal visa` route locally.
- [x] Dashboard slash autocomplete exists.
- [x] Help text cleaned of old test prompts.
- [x] Dashboard blocks risky/router-only commands from smart-open interception.

### Smart-open cleanup

- [x] Old duplicated smart-open V3/V4/V5/V6/V7 methods removed.
- [x] WebView smart-open message compatibility routes through one canonical smart-open path.
- [x] `tests\smart-open-cleanup.test.js` guards against duplicate smart-open returning.

### Terminal safety

- [x] Terminal preview/confirm/cancel uses `PendingApprovalV1`.
- [x] Approval popup reused for terminal preview.
- [x] Approval popup focuses `Avbryt` first and briefly locks `Godkänn`.
- [x] Approved terminal timeout increased to 120 seconds.
- [x] Terminal output streams stdout/stderr asynchronously.
- [x] Terminal-panel V1 exists.
- [x] Chat receives compact terminal summaries.
- [x] Latest terminal transcript stays in runtime memory.
- [x] `visa terminal` and related phrases no longer open terminal test files.
- [x] Generic `avbryt` is context-aware.

## Later roadmap

Read `docs\JARVIS_LONG_TERM_VISION.md` before larger work.

Later phases:

- Smart natural-language routing to validated intents.
- `.jarvis/tasks` task workspace.
- Worker agents for read/summarize/propose only.
- Multi-root Project Explorer with read-only defaults.
- Local model/provider setup docs/scripts.
- Optional Visual Lab / 3D after safety and workspace are stable.
- Voice Jarvis on top of the same router/validator/approval path.
