# TODO_NEXT PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


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
