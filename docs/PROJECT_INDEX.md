# PROJECT_INDEX.md — Jarvis project index

Senast uppdaterad: 2026-05-10 (kväll, post-BR-refaktor)

## Syfte

Detta index hjälper Jarvis och Codex att förstå projektstrukturen i `F:\Jarvis-clean`.

## Regler

- Indexet gäller bara `F:\Jarvis-clean`.
- `F:\New project` är read-only reference och ska inte ändras.
- Tunga/generated mappar som `bin`, `obj`, `dist`, `.git` och `node_modules` ska normalt exkluderas från läs-/indexarbete.
- Runtime actions ska följa CommandRouter, CommandValidator och PendingApproval.

## Nuvarande status i korthet

- Safe dashboard och C# WebView2-app fungerar.
- Project Explorer och filpanel finns.
- Terminal-panel V1 finns.
- Lokal Ollama-chat och lokal markdown-memory finns.
- CommandRouter V1 och slash commands finns.
- PendingApproval V1 hanterar file write, append, delete, undo och terminal preview/approval.
- Source-läge har även file panel pending save och `/fil skapa` via PendingApproval.
- Dashboard smart-open har guardrails.
- Gamla smart-open V3/V4/V5/V6/V7-implementationer har rensats till en canonical smart-open path.
- Known warning: WindowsBase/WebView2 conflict; build har 0 errors.

## Viktiga mappar

- `app` — C# WinForms/WebView2 runtime.
- `dashboard` — HTML/CSS/JS dashboard.
- `docs` — handoff, planer, projektindex, research och session-logg.
- `tests` — Node- och C#-testharness för routing/safety/UI-regression.
- `config` — lokal runtime-konfiguration.
- `data` — lokal markdown-memory.
- `dist` — publicerad release-output.

## Viktiga root-filer

- `AGENTS.md` — regler för AI-agenter.
- `README.md` — kort projektöversikt.
- `CURRENT_STATE.md` — senaste stabila status.
- `TODO_NEXT.md` — aktuell next-step lista (med Fas 0-8 unifieringscheckliste).
- `BUILD_PLAN.md` — fasplan.
- `MASTER_PLAN.md` — långsiktig masterplan.
- `RELEASE_STATUS.md` — release/status för startbar app.
- `Starta-Jarvis.vbs` — startscript.

## Aktiva planer

- `docs\UNIFICATION_PLAN.md` — porten av `F:\New project` till `F:\Jarvis-clean` (KLAR Fas 0–8)
- `docs\BRAIN_3D_SUPERPLAN.md` — 3D Brain View med vault-integration (BR1, BR2, BR6 KLARA; BR3, BR4 delvis)

## Faktiska filer på plats (2026-05-10 kväll)

C#-källor:
- `app\Program.cs` (5400+ rader) — JarvisForm, alla tools, vault-AI-injection
- `app\CommandRouterV1.cs` — slash-routing (45+ intents)
- `app\CommandValidatorV1.cs` — validering
- `app\PendingApprovalV1.cs` — approval-store
- `app\ToolRegistryV1.cs` — verktygskatalog
- `app\Brain\FileGraphBuilder.cs` — bygger 3D-grafens nod/edge-data (cache, vault-pre-check)
- `app\Brain\VaultSearcher.cs` — vault-sök för AI-kontext (BR2/BR6)
- `app\Bridges\NeuroLinkedBridge.cs` — Python brain-server livscykel
- `app\Agents\OllamaAgentHarness.cs` — read-only agent med 5 tools
- `app\Core\ModelCatalog.cs` — 5 modellprofiler

Dashboard:
- `dashboard\index.html` — main UI med inbäddad Brain-vy + Översikt + chat
- `dashboard\vendor\` — Three.js + OrbitControls + EffectComposer + UnrealBloomPass (1.4 MB, offline)
- INGA brain.html eller explorer.html separata filer (refaktor 2026-05-10)

Vault (`vault/`):
- `Index.md` — startpunkt med [[wikilinks]]
- `Project/` — UNIFICATION_PLAN, MULTI_WINDOW_DESIGN, MIGRATION, BRAIN_3D_SUPERPLAN, CURRENT_STATE
- `Memory/Azu_preferences.md`
- `Decisions/DECISIONS_LOG.md`
- `Issues/Brain_visual_polish.md`, `Vault_AI_context.md`
- `Backlog/FasA-MVP.md`, `FasA-MVP-Tasks.md`

Externa:
- `neurolinked\` — Python FastAPI-server (porterad från gamla)
- `python\` — weather/news/TTS/STT/web (porterad)
- `graphify-out\graph.json` — kunskapsgraf (114 MB, 63052 noder)

Borttagna efter refaktor (finns i `.checkpoints/20260510_after_phase8/`):
- `app\BrainWindow.cs`, `app\FileExplorerWindow.cs`
- `dashboard\brain.html`, `dashboard\explorer.html`

## Viktiga app-filer

- `app\Program.cs` — huvudruntime och WebView2 bridge.
- `app\CommandRouterV1.cs` — central command routing.
- `app\CommandValidatorV1.cs` — validering av command results.
- `app\ToolRegistryV1.cs` — tool metadata.
- `app\PendingApprovalV1.cs` — pending approval-modell.
- `app\JarvisClean.csproj` — C# projektfil.
- `app\README.md` — app-specifik info.

## Viktiga dashboard-filer

- `dashboard\index.html` — huvuddashboard med Project Explorer, filpanel, terminalpanel och chat.
- `dashboard\README.md` — dashboard notes.

## Viktiga docs

- `docs\JARVIS_LONG_TERM_VISION.md` — större Jarvis-vision och roadmap.
- `docs\CODEX_HANDOFF.md` — aktuell handoff till Codex.
- `docs\CODEX_START_PROMPT.md` — prompt för nya Codex-sessioner.
- `docs\COMMAND_ROUTER_RESEARCH.md` — CommandRouter-designresearch.
- `docs\REFERENCE_PROJECTS.md` — externa referensprojekt.
- `docs\OFFLINE_CODEX_PLAN.md` — lokal Codex-liknande agentplan.
- `docs\VISUAL_PANEL_PLAN.md` — panel-arkitektur för Jarvis Översikt, 3D och framtida visuella lager.
- `docs\SESSION_LOG.md` — historik över genomförda pass.
- `docs\PROJECT_INDEX.md` — detta index.
- `docs\FUTURE_IDEAS.md` — senare idéer.
- `docs\SMART_MEMORY_PLAN.md` — Smart Memory-plan.

## Viktiga tests

- `tests\dashboard-routing.test.js` — dashboard smart-open/routing guardrails.
- `tests\terminal-approval-safety.test.js` — terminal preview/approval/transcript/cancel-safety.
- `tests\approval-popup.test.js` — approval popup UI behavior.
- `tests\approval-popup-csharp.test.js` — C# popup payload hooks.
- `tests\help-text.test.js` — help text regression.
- `tests\file-write-safety.test.js` — pending file write/append safety.
- `tests\editor-save-safety.test.js` — file panel edit/save must create pending approval and block truncated previews.
- `tests\visual-panel.test.js` — Jarvis Översikt must remain a separate lightweight panel.
- `tests\dashboard-scrollbar-style.test.js` — dashboard scrollbars must use dark styling.
- `tests\app-project-scope.test.js` — main app project must exclude experimental nested C# source folders.
- `tests\project-explorer-polish.test.js` — Project Explorer aktiv-fil/aktiv-mapp-highlight och persistent active path across rerender.
- `tests\overview-livestate.test.js` — Jarvis Översikt visar aktiv mapp och senaste filändring; computeActiveFolderLabelV1 path-cases.
- `tests\file-delete-safety.test.js` — pending delete safety.
- `tests\change-review-ui.test.js` — diff/review UI.
- `tests\change-review-csharp.test.js` — C# change review hooks.
- `tests\smart-open-cleanup.test.js` — old smart-open duplicate guard.
- `tests\undo-safety.test.js` — one-step undo approval safety.
- `tests\CommandRouterV1.Tests\Program.cs` — C# CommandRouter V1 tests.
- `tests\README.md` — test notes.

## Nästa steg

Aktuell next-step lista ska matcha `TODO_NEXT.md` och `docs\CODEX_HANDOFF.md`:

1. Manually verify terminal routing/cancel/terminal panel in Jarvis UI.
2. Manually verify pending file write/append/delete/undo in Jarvis UI.
3. Manually verify Jarvis Översikt, `/översikt`, `/minne status` and `/obsidian status`.
4. Manually verify Project Explorer active-file/active-folder highlight after opening files.
5. Improve terminal transcript formatting.
6. Add named checkpoint/history beyond one-step undo.
7. Build `.jarvis/tasks` task workspace later.
8. Build worker delegation later; workers read/summarize/propose only.
9. Keep real 3D later and off by default.

## Manual smoke tests

Testa i Jarvis UI:

- `visa terminal`
- `vad stod i terminalen`
- `avbryt`
- `terminal preview: dotnet build`
- `/fil skapa docs/test-create.md | HEJ FRÅN CREATE`
- `/översikt`
- `/minne status`
- `/obsidian status`
- `skriv fil: docs/test-agent.md | TESTAR PENDING APPROVAL`
- `öppna tests/terminal-approval-safety.test.js`
- `/fil öppna tests/terminal-approval-safety.test.js`
