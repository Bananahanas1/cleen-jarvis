# PROJECT_INDEX.md - Jarvis project index

Senast uppdaterad: 2026-05-12

## Syfte

Detta index hjälper Jarvis, Codex och nästa AI-agent att snabbt förstå
`cleen-jarvis` / `F:\Jarvis-clean`.

## Produktroll

- `cleen-jarvis` är huvudprodukten.
- Lokal arbetsmapp är `F:\Jarvis-clean`.
- `f-drive-projects` är referens, backup och inspiration.
- `F:\New project` är read-only reference och får aldrig ändras.
- GitHub-repot är källan externa AI-agenter kan läsa från.

## Aktuell prioritet

Nästa riktiga build är **Project Index + Background Jobs MVP**.
Kartan och stora live/3D-features väntar.

## Root-filer

- `AGENTS.md` - regler för AI-agenter.
- `README.md` - kort projektöversikt.
- `JARVIS_MEGA_MASTER_PROMPT.md` - kort mega-index.
- `PLANNING_INDEX.md` - plan-index.
- `JARVIS_MASTER_PLAN.md` - aktiv masterplan.
- `JARVIS_CORE_INDEX.md` - core-arkitektur.
- `CURRENT_PROJECT_AUDIT.md` - nulägesaudit.
- `JARVIS_BACKGROUND_JOBS_PLAN.md` - nästa MVP.
- `KARTAN_INDEX.md` - framtida Kartan-plan.
- `NEXT_AI_AGENT_TODO.md` - konkret nästa-agent lista.
- `CURRENT_STATE.md` - aktuell status.
- `TODO_NEXT.md` - aktiv TODO.
- `BUILD_PLAN.md` - äldre fasplan.
- `RELEASE_STATUS.md` - release/status.
- `Starta-Jarvis.vbs` - startscript.

## Viktiga mappar

- `app` - C# WinForms/WebView2 runtime.
- `dashboard` - HTML/CSS/JS dashboard.
- `docs` - handoff, planer, research och session-logg.
- `tests` - Node- och C#-tester.
- `config` - lokal runtime-konfiguration.
- `data` - lokalt minne, framtida jobs och projektindex.
- `vault` - vault/AI-kontext.
- `neurolinked` och `python` - porterade stödkomponenter.
- `graphify-out` - genererad grafdata/rapport.

## Core-filer

- `app/Program.cs`
- `app/CommandRouterV1.cs`
- `app/CommandValidatorV1.cs`
- `app/ToolRegistryV1.cs`
- `app/PendingApprovalV1.cs`
- `app/Brain/ModelRouter.cs`
- `app/Brain/ConversationHistory.cs`
- `app/Brain/VaultSearcher.cs`
- `app/Brain/BuilderMode.cs`
- `app/Brain/NaturalEditTool.cs`
- `app/Desktop/*`
- `dashboard/index.html`

## Testfiler att känna till

- `tests/dashboard-routing.test.js`
- `tests/terminal-approval-safety.test.js`
- `tests/approval-popup.test.js`
- `tests/help-text.test.js`
- `tests/file-write-safety.test.js`
- `tests/file-delete-safety.test.js`
- `tests/undo-safety.test.js`
- `tests/smart-open-cleanup.test.js`
- `tests/desktop-control.test.js`
- `tests/builder-mode.test.js`
- `tests/natural-edit-tool.test.js`
- `tests/CommandRouterV1.Tests/Program.cs`

## Markdown/PART-policy

Alla `.md`-filer ska vara under 14 000 tecken. Långa historiska dokument har
delats i `*_PART_01.md`, `*_PART_02.md` osv. Läs indexfilen först och sedan
delarna i ordning.

## GitHub-sync

Efter större lyckad ändring i huvudprodukten:

1. `git status`
2. relevant build/test
3. stage:a avsedda filer
4. commit
5. push

Stage:a inte secrets, `.env`, runtime-cache eller orelaterade användarändringar.
