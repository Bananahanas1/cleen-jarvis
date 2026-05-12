# CURRENT_STATE PART 03

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


Decision:
- Do not integrate either project directly into Jarvis-clean right now.
- Keep them as reference/inspiration only.
- Jarvis-clean must first finish CommandRouter V1, CommandValidator, and safe file write approval.

Reason:
Jarvis-clean currently has routing and command-safety technical debt. Adding external agent/workspace systems directly now would make the architecture harder to control and could create unsafe file/terminal behavior.

Ideas to reuse later:

From Octogent:
- task/workspace folders
- CONTEXT.md per task
- TODO.md per task
- NOTES.md per task
- agent workspace UI
- terminal/task status panels
- handoff notes between AI agents

From claude-coworker-model:
- worker model delegation
- cheap/local worker for large file reading
- worker summaries before main Jarvis reasoning
- separate "main brain" and "worker/helper" responsibilities

Current rule:
External systems can be cloned/read as references, but they should not be connected to Jarvis runtime until local command routing and write safety are stable.

## Future access plan: F drive roots

Jarvis should later browse approved folders on F:\ through Project Explorer. This must start as read-only. F:\New project is reference only and must never be changed. Writing outside F:\Jarvis-clean stays blocked until a permission system and pending approval flow exists.

## 2026-05-05 — Codex handoff and slash-command decision

Created docs/CODEX_HANDOFF.md and docs/CODEX_START_PROMPT.md. Important decision: Jarvis should support exact slash-commands plus natural language routing. Slash commands should go directly to CommandRouter and never to Ollama. Natural language may use LLM reasoning but must convert actions into safe validated intents before execution.

## 2026-05-05 — Current command-safety state

Dashboard smart-open now refuses router-only commands before it can treat them as file-open. This covers slash commands, pending approval commands, file write/append/propose/delete commands and terminal preview/confirm/cancel.

`skriv fil` and `lägg till fil` now create pending approval only for existing safe text files inside `F:\Jarvis-clean`. They do not create new files automatically. A future `/fil skapa` command is still needed for safe new-file creation.

Approved file write/append/delete creates a latest-file undo snapshot. The dashboard shows an `Ångra` button beside `Granska ändringar`; clicking it creates a pending undo preview and does not touch disk until approved. Undo V1 is one-level and file-only, not a global app undo system.

Latest verification:
- CommandRouterV1 tests passed.
- Dashboard routing, help text, file write safety, file delete safety, approval popup, change review, smart-open cleanup and undo safety tests passed.
- `dotnet build` passed.
- `dotnet publish F:\Jarvis-clean\app\JarvisClean.csproj -c Release -o F:\Jarvis-clean\dist --no-self-contained` passed.
- Known warning remains: WindowsBase/WebView2 version conflict warning.
- Jarvis was restarted from `Starta-Jarvis.vbs`.

Manual tests for the user:
- `skriv fil: docs/test-agent.md | TESTAR PENDING APPROVAL`
- approve/cancel with popup buttons
- `skriv fil: docs/test-safe-write.md | text`
- `radera fil: docs/test-agent.md`
- `Granska ändringar`, close `×`, and `Ångra`
- `/hjälp`, `/status`, `/minne viktiga`, `/fil öppna app/Program.cs`, `/fil läs docs/PROJECT_INDEX.md`

## 2026-05-05 — Terminal approval state

Terminal preview/confirm/cancel now uses the shared `PendingApprovalV1` popup flow.

Current behavior:
- `terminal preview: dotnet build` creates a pending terminal run and shows the popup.
- The popup starts with `Avbryt` focused, while `Godkänn` is disabled for about 1.2 seconds to prevent accidental approval.
- `bekräfta kör` approves only a pending terminal run.
- `avbryt kör` cancels only a pending terminal run.
- `/terminal preview dotnet build`, `/terminal godkänn`, and `/terminal avbryt` route locally and do not go to Ollama.
- `bygg projektet men fråga först` maps to terminal preview.
- Approved terminal runs stream stdout/stderr asynchronously and have a 120 second timeout.
- Full terminal output is shown in the middle workspace Terminal panel instead of flooding chat.
- Chat gets a compact terminal summary.
- Jarvis keeps the latest terminal transcript in runtime memory and can answer `visa terminal`, `vad stod i terminalen`, `senaste terminal`, `terminal output` and `/terminal visa`.
- Dashboard smart-open blocks terminal UI phrases so they do not open files such as `tests/terminal-approval-safety.test.js`.
- Generic `avbryt`, `avbryt allt`, `cancel` and `stoppa` cancel the active pending approval item by type. With no pending action, Jarvis answers `Det finns inget pending att avbryta.`

Latest verification:
- CommandRouterV1 tests passed.
- Dashboard routing, help text, file write/delete safety, approval popup, change review, smart-open cleanup, undo safety and terminal approval safety tests passed.
- `dotnet build` passed.
- `dotnet publish F:\Jarvis-clean\app\JarvisClean.csproj -c Release -o F:\Jarvis-clean\dist --no-self-contained` passed.
- Known warning remains: WindowsBase/WebView2 version conflict warning.
- Jarvis was restarted from `Starta-Jarvis.vbs`.

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
