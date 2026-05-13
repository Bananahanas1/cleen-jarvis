# SESSION_LOG.md

## 2026-05-13 - Brain Graph Force Sliders

Andringar:

- Lade till tre sliders i Brain Graph: Center, Repel och Link.
- Sliders styr 3D-force-layouten live och startar om settling nar de dras.
- Ny regression: `tests/brain-force-controls.test.js`.

Verifiering:

- TDD red: `tests/brain-force-controls.test.js` failade forst pa saknade sliders.
- Green: riktat test passerade efter implementation.
- Full smoke-scriptet passerade: 41 Node-testfiler, `CommandRouterV1.Tests`,
  `dotnet build` och Markdown-langdkoll.
- Logg: `data/test-runs/20260513-172616/summary.txt`.
- Publish/start klart. Observerad process: `Jarvis.exe` PID 40836.

## 2026-05-13 - Full System Smoke Test

Andringar:

- Skapade `tests/run-full-smoke.ps1`.
- Skapade `docs/FULL_SYSTEM_TEST.md` med manuell testchecklista.
- Skapade `tests/full-smoke-runner.test.js` sa test-runnern bevakas.
- Uppdaterade `tests/README.md` med kort kommando.

Verifiering:

- TDD red: `tests/full-smoke-runner.test.js` failade forst pa saknad runner/guide.
- Green: `tests/full-smoke-runner.test.js` passerade.
- Full smoke-scriptet passerade: 40 Node-testfiler, `CommandRouterV1.Tests`,
  `dotnet build` och Markdown-langdkoll.
- Logg: `data/test-runs/20260513-171921/summary.txt`.
- Docs/test-only: ingen publish/restart behovdes.

## 2026-05-13 - Agent Autopilot Modes V1

Utgangspunkt: femniva-modellen skulle goras konkret. Desktop-kravet andrades
till nastan allt normalt app-arbete, inte liten whitelist.

Andringar:

- Skapade `app/Agents/AgentAutopilotModeV1.cs`.
- Lade till Safe, Approval, Browser Autopilot, Desktop Autopilot och Build Agent.
- Desktop Autopilot ar BroadDesktopControl for nastan alla normala appar, men
  med denylist, scope och Ctrl+Shift+Alt+J kill-switch.
- Browser Autopilot haller OperaGX/Opera synligt och isolerad Chromium internt.
- Lade till `/autopilot status`, `/autopilot approval`,
  `/autopilot browser <uppdrag>`, `/autopilot desktop <uppdrag>`,
  `/autopilot build <uppdrag>` och `/autopilot stop`.
- Oversiktspanelen visar Autopilot-status.

Verifiering:

- TDD red: `tests/autopilot-modes.test.js` failade forst pa 18 saknade delar.
- Green: `tests/autopilot-modes.test.js` passerade.
- Full node-regression passerade: 37 tester.
- `CommandRouterV1.Tests` passerade med autopilot-routerfall.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och kand `MSB3277`.
- Publicerade/startade om. Observerad process: `Jarvis.exe` PID 15628,
  path `F:\Jarvis-clean\dist\Jarvis.exe`.

## 2026-05-13 - Browser Autopilot Runner V1

Andringar:

- Skapade `app/Agents/BrowserAutopilotRunnerV1.cs`.
- `/autopilot browser <uppdrag>` kan nu soka/oppna/lasa URL via Opera-policy.
- Blockar login, password/secrets, betalning, bankid, skicka och publicering.
- V1 klickar/skriver inte i sidor an.

Verifiering:

- TDD red: `tests/browser-autopilot-runner.test.js` failade forst pa 10 delar.
- Green: browser-runner-testet passerade.
- Full node-regression passerade: 38 tester.
- `CommandRouterV1.Tests` passerade.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och kand `MSB3277`.

## 2026-05-13 - Desktop Autopilot Runner V1

Andringar:

- Skapade `app/Agents/DesktopAutopilotRunnerV1.cs`.
- `/autopilot desktop <uppdrag>` foreslar nu ett UI-TARS-steg direkt.
- Varje klick/typ/hotkey blir fortfarande `PendingApprovalV1`.
- Runnern blockerar login, betalning, secrets, admin/system/terminal och delete.
- Max 12 steg per uppdrag och kill-switch finns kvar.

Verifiering:

- TDD red: `tests/desktop-autopilot-runner.test.js` failade forst pa saknad runner.
- Green: riktat desktop-runner-test passerade.
- Full node-regression passerade: 39 tester.
- `CommandRouterV1.Tests` passerade.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors och kand `MSB3277`.
- Publish/start klart. Observerad process: `Jarvis.exe` PID 40716.

## 2026-05-13 - HybridModelRouterV1 + ContextPackV1

Utgangspunkt: LLM ska hjalpa Jarvis forsta och planera, men inte fa direkt
skriv-, terminal- eller desktopmakt.

Andringar:

- Skapade `app/Brain/ContextPackV1.cs`.
- Skapade `app/Brain/HybridModelRouterV1.cs`.
- `Program.cs` har nu hybrid chat fallback efter lokal router och safe tools.
- Online providers via env vars: `GROQ_API_KEY`, `GEMINI_API_KEY`, `GITHUB_TOKEN`.
- `/modell provider` visar backendstatus.
- `/modell lage lokal` och `/modell lage auto` styr lokal/auto-free.
- Oversiktspanelen visar Modellmotor.

Sakerhet: LLM ar radgivare/tolk; Jarvis ager routing, approval och tools.

Verifiering:

- TDD red/green: `tests/hybrid-model-router-context.test.js`.
- Full node-regression passerade: 36 tester.
- `CommandRouterV1.Tests` passerade.
- Markdown-langdkontroll passerade: alla `.md` under 14 000 tecken.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och kand `MSB3277`.
- Publicerade till `F:\Jarvis-clean\dist` och startade Jarvis igen.
- Observerad process: `Jarvis.exe` PID 28220, path `F:\Jarvis-clean\dist\Jarvis.exe`.
- Notering: kombinerat publish/start-kommando returnerade exit code 1 efter
  startdelen, men publish-output var lyckad och processen verifierades separat.

## 2026-05-13 - Panel-first monitor och TaskStoreV1

Utgångspunkt: användaren vill inte behöva minnas många kommandon. Jarvis ska
visa vad som händer live när den jobbar, kodar, skapar saker eller kör
bakgrundsjobb.

Ändringar:

- Ny `app/Tasks/TaskStoreV1.cs` med lokala tasks i `data/tasks/tasks.json`.
- Nya task-intents: lista, status, add, done och sök.
- Task-skrivningar går via `PendingApprovalTypeV1.TaskChange`.
- Översiktspanelen visar nu livearbete, bakgrundsjobb, tasks, pending,
  terminal/build och en mini-agent som animeras när arbete pågår.
- Panelen har snabbknappar för index, audit, jobbstatus, tasks och terminal.
- Panelen har även snabb task-input med röd/orange/blå prioritet; den skickar
  `/task add ...` och går fortfarande via pending approval.
- `Program.cs` registrerar livearbete när Jarvis tänker, skapar pending filer,
  gör kodförslag, kör terminal eller ändrar tasks.
- Ny regression: `tests/tasks-monitor-panel.test.js`.

Verifiering:

- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerade.
- Full node-regression passerade: 35 tester.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och känd `MSB3277`.

## 2026-05-13 - Background status och token/context-estimat

Utgångspunkt: användaren vill att Jarvis ska säga mer medan den tänker eller
jobbar i bakgrunden, inklusive token/context när det går.

Ändringar:

- Ny `app/Brain/ContextBudgetEstimatorV1.cs`.
- Vanliga Ollama-svar visar nu ungefärligt `ctx≈...` och `svar≈...`.
- Background jobs sparar `CurrentStep`, `LastAction`, `NextAction` och
  `ContextEstimateTokens`.
- `/jobb status` visar steg, token/context-estimat och nästa handling.
- Startmeddelanden för project index/audit säger att token/context rapporteras
  när jobbet börjar.
- Ny regression: `tests/background-status-token.test.js`.

Verifiering:

- `node F:\Jarvis-clean\tests\background-status-token.test.js` passerade.
- `node F:\Jarvis-clean\tests\background-jobs-architecture.test.js` passerade.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och känd `MSB3277`.

## 2026-05-13 - BrowserPolicyV1 runtime

Utgångspunkt: användaren vill att Jarvis inte ska använda andra synliga
browsers än OperaGX/Opera, men godkände rekommendationen att intern
agent-automation får använda isolerad Playwright Chromium när det är tekniskt
bättre.

Ändringar:

- Ny `app/Desktop/BrowserPolicyV1.cs`.
- `SafeAppLauncher` har inte längre Chrome, Edge eller Firefox som synliga
  launch-mål.
- `open browser`, `webbläsare`, `opera gx` och `operagx` routas till Opera.
- Explicit Chrome, Edge, Firefox och Chromium blockeras som synliga launch-mål.
- Webbsök/helptext säger Google i OperaGX/Opera.
- `python/jarvis_web_agent.py` dokumenterar isolerad Playwright Chromium som
  intern automation engine.
- Ny regression: `tests/browser-policy.test.js`.

Verifiering:

- `node F:\Jarvis-clean\tests\browser-policy.test.js` passerade.
- `node F:\Jarvis-clean\tests\b1-b2-c1-d2.test.js` passerade.
- `node F:\Jarvis-clean\tests\help-text.test.js` passerade.
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerade.

## 2026-05-13 - Amy Windows Autopilot designbeslut

Utgangspunkt: anvandaren vill att Jarvis pa sikt ska kunna allt Amy kan, men
Windows-native och med agentkraft som kan ta beslut inom godkant scope.

Andringar:

- Skapade `docs/AMY_WINDOWS_AUTOPILOT_PLAN.md`.
- Dokumenterade att framtida browserfloden ska vara OperaGX/Opera-only.
- Dokumenterade scoped Autopilot: Browser Autopilot, Desktop Autopilot och
  Build Agent utan permanent fri makt over hela datorn.
- Dokumenterade att background jobs/langre svar ska visa kort status, progress
  och token/context-estimat nar det finns.
- Uppdaterade `BUILD_PLAN.md` och `TODO_NEXT.md` med beslutet.

Verifiering:

- Docs-only andring. Ingen runtime, build eller publish kordes.

## 2026-05-12 - Project Index incremental search audit slice

Utgångspunkt: fortsätt hela Jarvis-planen, men prioritera Project Index +
Background Jobs före Kartan.

Ändringar:

- `ProjectIndexServiceV1` gör nu incremental scan och återanvänder oförändrade
  hash/summaries.
- Indexet skriver `data/project-index/files`, `data/project-index/folders` och
  `data/project-index/search.jsonl`.
- Ny `ProjectIndexSearchServiceV1` ger `/projekt sök <query>` och smal
  Project Index-kontext innan vanlig Ollama-chat.
- Ny `ProjectAuditServiceV1` skapar läsbar auditrapport.
- `/projekt audit` och `skapa audit` startar audit som background job.
- Background jobs skriver nu `log.md` och `result.md`.

Verifiering:

- TDD red: Node-arkitekturtest och C# routertest failade först på saknade
  incremental/search/audit-delar.
- Green: riktade Node- och C#-tester passerade efter implementation.
- Full node-regression passerade.
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerade.
- Markdown-längdkontroll passerade: alla `.md` under 14 000 tecken.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors och känd `MSB3277`.
- `dotnet publish F:\Jarvis-clean\app\JarvisClean.csproj -c Release -o F:\Jarvis-clean\dist --no-self-contained` producerade releasefiler med känd `MSB3277`.
- Startade via `wscript F:\Jarvis-clean\Starta-Jarvis.vbs`.
- Observerad process: `Jarvis.exe` PID 9544, path `F:\Jarvis-clean\dist\Jarvis.exe`.
- Notering: kombinerat PowerShell-kommando returnerade exit code 1 efter `wscript`, men publish-output var lyckad och processen verifierades som körande.
- Efter UI-test visade `/projekt audit` att `.nuget` indexerades och `/jobb status`
  visade absolut resultatsökväg. Lade regressioner och fixade så `.nuget`/`.vs`
  exkluderas och job status visar relativ `data/jobs/.../result.md`.
- Audit-rapporten begränsades så framtida `result.md` håller sig under 14 000 tecken.
- Efter bugfixen kördes full node-regression, C# routertest, Markdown-längdkontroll
  och `dotnet build` igen: allt passerade med 0 errors och känd `MSB3277`.
- Publicerade/startade om igen. Observerad process: `Jarvis.exe` PID 18532, path
  `F:\Jarvis-clean\dist\Jarvis.exe`.

Kvar:

- Riktig pause/resume för background jobs.
- Mer avancerad chunk/map-reduce summary för stora filer.
- Fortsatt `Program.cs`-refaktor till mindre services.

## 2026-05-12 - Tidigare background/docs-slices

Detaljer finns i PART-loggarna. Kort: Project Index, Background Jobs, incremental search/audit och docs-split byggdes fore Kartan.

## Historisk session-logg

Den tidigare långa `docs/SESSION_LOG.md` är bevarad i delar:

- [PART 01](SESSION_LOG_PART_01.md)
- [PART 02](SESSION_LOG_PART_02.md)
- [PART 03](SESSION_LOG_PART_03.md)
- [PART 04](SESSION_LOG_PART_04.md)
- [PART 05](SESSION_LOG_PART_05.md)
- [PART 06](SESSION_LOG_PART_06.md)
- [PART 07](SESSION_LOG_PART_07.md)
- [PART 08](SESSION_LOG_PART_08.md)
