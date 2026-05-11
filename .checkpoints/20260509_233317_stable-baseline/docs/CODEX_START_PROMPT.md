# CODEX_START_PROMPT.md

Kopiera denna prompt till Codex när Codex ska ta över projektet.

## Prompt

You are taking over my local Windows/C# project called Jarvis-clean.

Project root:

```text
F:\Jarvis-clean
```

Do not touch:

```text
F:\New project
```

`F:\New project` is read-only reference only. Do not write, delete, move, rename, format, or auto-modify anything there.

Before changing code, read current state first:

- `CURRENT_STATE.md`
- `TODO_NEXT.md`
- `BUILD_PLAN.md`
- `docs\JARVIS_LONG_TERM_VISION.md`
- `docs\CODEX_HANDOFF.md`
- `docs\SESSION_LOG.md`
- `docs\PROJECT_INDEX.md`
- `docs\COMMAND_ROUTER_RESEARCH.md`
- `docs\REFERENCE_PROJECTS.md`
- `README.md`
- `RELEASE_STATUS.md`
- `app\CommandRouterV1.cs`
- `app\CommandValidatorV1.cs`
- `app\ToolRegistryV1.cs`
- `app\PendingApprovalV1.cs`
- `app\Program.cs`
- `dashboard\index.html`
- `tests` folder

Current true status:

- CommandRouter V1 exists.
- Slash commands have already been implemented for help/status/memory/file/terminal areas.
- Do not redo slash step 1.
- PendingApproval V1 handles file write, append, delete, undo and terminal preview/approval.
- Dashboard smart-open has guardrails for risky/router-only commands.
- Old smart-open V3/V4/V5/V6/V7 duplication was cleaned into one canonical smart-open path.
- Terminal panel V1 exists.
- `visa terminal` / `vad stod i terminalen` routing was fixed.
- Generic `avbryt` is context-aware.
- Known build warning remains WindowsBase/WebView2, but build has 0 errors.

Very important rules:

1. Do not touch `F:\New project` except as read-only reference.
2. Do not give Jarvis unrestricted write access to the F drive.
3. Do not add new V8/V9 smart-open patches.
4. All local commands must be handled before Ollama.
5. Only normal chat and reasoning should go to Ollama.
6. File write, append, delete, undo, terminal run, external tools and future UI automation must require safety checks and pending approval.
7. Workers/agents must never write directly. They may read, summarize and propose only.
8. Keep changes small and build/test after important steps.
9. Update docs after each successful runtime change.
10. Do not publish/restart for docs-only work.
11. After runtime code/dashboard changes pass relevant tests and `dotnet build`, publish/restart Jarvis-clean so the user can test immediately.

Current next work:

1. Manually verify terminal routing/cancel/terminal panel in Jarvis UI.
2. Manually verify pending file write/append/delete/undo in Jarvis UI.
3. Improve Project Explorer tree polish.
4. Build file panel edit mode with pending save.
5. Improve terminal transcript formatting.
6. Add explicit `/fil skapa` with pending approval.
7. Add named checkpoint/history beyond one-step undo.
8. Build `.jarvis/tasks` task workspace later.
9. Build worker delegation later; workers read/summarize/propose only.
10. Add local Ollama/Claude Code setup docs/scripts later.
11. Keep 3D/Visual Lab later and off by default.

Expected safety behavior:

- `/hjälp`, `/status`, `/minne`, `/fil`, `/terminal` and other local slash commands must never go to Ollama.
- `visa terminal` and `vad stod i terminalen` must not open terminal test files.
- `avbryt` with no pending action should say there is no pending action.
- `avbryt` with pending terminal should cancel terminal.
- File write/append/delete/undo must not touch disk before approval.
- Terminal run must not execute before approval.

Build/test commands:

```powershell
cd F:\Jarvis-clean\app
dotnet build
```

Useful tests when runtime code changes:

```powershell
node F:\Jarvis-clean\tests\dashboard-routing.test.js
node F:\Jarvis-clean\tests\terminal-approval-safety.test.js
node F:\Jarvis-clean\tests\approval-popup.test.js
node F:\Jarvis-clean\tests\help-text.test.js
node F:\Jarvis-clean\tests\file-write-safety.test.js
node F:\Jarvis-clean\tests\file-delete-safety.test.js
node F:\Jarvis-clean\tests\undo-safety.test.js
node F:\Jarvis-clean\tests\smart-open-cleanup.test.js
dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj
```

Publish/restart after runtime changes pass build/tests:

```powershell
Stop-Process -Name JarvisClean -Force -ErrorAction SilentlyContinue
Stop-Process -Name Jarvis -Force -ErrorAction SilentlyContinue
dotnet publish F:\Jarvis-clean\app\JarvisClean.csproj -c Release -o F:\Jarvis-clean\dist --no-self-contained
wscript F:\Jarvis-clean\Starta-Jarvis.vbs
```

For docs-only work:

- run `dotnet build`
- do not publish
- do not restart Jarvis
- update `docs\SESSION_LOG.md`
