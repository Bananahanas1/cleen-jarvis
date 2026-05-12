# CURRENT_STATE PART 04

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


Manual tests:
- `terminal preview: dotnet build` and immediately try `Godkänn`; expected: disabled button does nothing.
- `terminal preview: dotnet build` then popup `Avbryt`; expected: no terminal command runs.
- `terminal preview: dotnet build` then popup `Godkänn`; expected: compact chat summary and full output in Terminal panel.
- `visa terminal`; expected: latest terminal transcript summary is answered locally.
- `vad stod i terminalen`; expected: latest terminal transcript summary is answered locally.
- `avbryt`; expected without pending: `Det finns inget pending att avbryta.`
- `terminal preview: dotnet build` then `avbryt`; expected: pending terminal run is cancelled, no command runs.
- `öppna tests/terminal-approval-safety.test.js`; expected: file opens normally.
- `/fil öppna tests/terminal-approval-safety.test.js`; expected: file opens via C# router, not dashboard smart-open.
- Click Terminal panel `Kopiera`, `Rensa`, and `×`.
- `/terminal preview dotnet build`
- `/terminal avbryt`
- `bygg projektet men fråga först`

UI/3D status:
- UI/3D is ready for a design pass, but should start as optional lightweight visual mode.
- Do not add heavy default WebGL/Three.js simulation yet.
- Proposed next UI slice: a `Visual Lab` area/state that makes approvals, project state and future 3D/Obsidian/NeuroLink status visible without changing the safe 3-panel workflow.

## 2026-05-05 — Terminal/review/autocomplete polish

Latest source changes:
- `vad stod i terminalen` / `visa terminal` chat response is now compact and points to Terminal-panel for full output.
- File change review payload now includes change kind.
- Delete review bar can show `1 fil raderad +0 -N` instead of generic `1 fil har ändrats`.
- Dashboard autocomplete now hides `avbryt kör` when there is no pending terminal run, and shows it when terminal pending approval is active.

Verification:
- terminal approval safety test passed.
- change review UI test passed.
- dashboard routing/autocomplete test passed.
- approval popup, help, file write/delete, undo, smart-open cleanup and CommandRouter tests passed.
- `dotnet build` passed with 0 errors.
- Known warning remains: WindowsBase/WebView2 version conflict warning.

Publish/restart:
- Not run in this source pass because starting Jarvis requires explicit permission in `AGENTS.md`.

## 2026-05-06 — File panel pending save and `/fil skapa` source pass

Source implementation completed for the next Developer Workspace slice:

- File panel now has active `Edit-läge` and `Spara med godkännande` controls.
- Editing an opened file does not write directly.
- Saving from the file panel posts a `jarvis_editor_save_pending_v1` message to C#.
- C# creates a `PendingApprovalV1` file-write preview for editor saves.
- Long/truncated file previews are blocked from edit/save so Jarvis cannot overwrite a file with partial displayed content.
- `/fil skapa docs/test.md | text` now routes through `CommandRouterV1` as a pending file-create request.
- `skapa fil: docs/test.md | text` also stays local and creates pending file creation.
- File creation uses `PendingApprovalTypeV1.FileCreate`.
- Approval creates the file only after user approval and records undo/review metadata.
- Dashboard smart-open blocks `skapa fil:` so it is not misread as file-open.

Verification:
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passed.
- `node F:\Jarvis-clean\tests\dashboard-routing.test.js` passed.
- `node F:\Jarvis-clean\tests\file-write-safety.test.js` passed.
- `node F:\Jarvis-clean\tests\editor-save-safety.test.js` passed.
- Existing approval, terminal, delete, undo, change review, help and smart-open cleanup tests passed.
- `dotnet build` passed with 0 errors.
- Known warning remains: WindowsBase/WebView2 version conflict warning.

Publish/restart:
- At the time of that source pass, publish/restart was not run because the old `AGENTS.md` required explicit permission.
- `AGENTS.md` has since been updated: after successful runtime changes, tests and `dotnet build`, Codex may publish/restart Jarvis-clean so the user can test immediately. Docs-only work still must not publish/restart.

Manual tests after publish/restart:
- `/fil skapa docs/test-create.md | HEJ FRÅN CREATE`
- approve popup; expected: file created and review bar appears
- click `Ångra`, approve undo; expected: created file removed
- open an existing safe file, click `Edit-läge`, edit text, click `Spara med godkännande`
- cancel popup; expected: file unchanged
- repeat editor-save and approve; expected: file written only after approval and review bar appears

## 2026-05-06 — AGENTS publish/restart rule updated

`AGENTS.md` no longer blocks Jarvis-clean restart after every runtime pass.

Current rule:
- after runtime code/dashboard changes pass relevant tests and `dotnet build` with 0 errors, Codex may stop, publish and restart Jarvis-clean so the user can test immediately
- docs-only/research-only work still must not publish/restart
- NeuroLinked, heavy simulation and unsafe reference apps still require explicit permission

Reason:
- the old rule was a broad safety brake from earlier phases
- the safer current workflow is faster UI verification after tested runtime changes

Latest publish/restart:
- publish passed
- Jarvis restarted through `Starta-Jarvis.vbs`
- observed process: `Jarvis.exe` PID 57932

## 2026-05-06 — Visual Lab V1 panel source pass

Jarvis dashboard now treats the practical middle workspace as `Workspace Panel`.

New source behavior:
- Added `Visual Lab` button beside `Filer` and `Terminal`.
- Visual Lab V1 is a separate optional panel, not the whole app.
- Visual Lab V1 shows active file, pending approval state, latest terminal state and future visual architecture note.
- Visual Lab V1 stays lightweight: no heavy 3D, no render loop and no separate action system.
- Pending approval hint now appears near chat input while an approval is active.
- Change review labels now distinguish:
  - `1 fil skapad`
  - `1 fil skriven`
  - `1 fil utökad`
  - `1 fil raderad`
  - `1 fil återställd`
- `docs\VISUAL_PANEL_PLAN.md` documents that future visual work should be panel-based.

Manual tests after publish/restart:
- Click `Visual Lab`; expected: middle panel switches to Visual Lab state view.
- Click `Filer`; expected: normal Workspace Panel/file editor returns.
- Run `/fil skapa docs/test-create.md | HEJ FRÅN CREATE`; expected: pending hint appears near input and popup appears.
- Approve; expected: review bar says `1 fil skapad`.
- Open/edit/save existing file; expected: review bar says `1 fil skriven`.

Verification/publish:
- Visual panel, change review UI, approval popup, dashboard routing, editor-save safety, terminal approval, help, file write/delete, undo, smart-open cleanup and CommandRouter tests passed.
- `dotnet build` passed with 0 errors.
- Known warning remains: WindowsBase/WebView2 `MSB3277`.
- `dotnet publish` passed.
- Jarvis restarted through `Starta-Jarvis.vbs`.
- Observed process: `Jarvis.exe` PID 52660.
