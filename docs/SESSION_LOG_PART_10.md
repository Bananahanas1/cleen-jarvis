# SESSION_LOG_PART_10.md

Flyttat från `docs/SESSION_LOG.md` 2026-05-15 för att hålla huvudloggen under 14 000 tecken.

## 2026-05-13 - Panel-first monitor och TaskStoreV1

UtgÃ¥ngspunkt: anvÃ¤ndaren vill inte behÃ¶va minnas mÃ¥nga kommandon. Jarvis ska
visa vad som hÃ¤nder live nÃ¤r den jobbar, kodar, skapar saker eller kÃ¶r
bakgrundsjobb.

Ã„ndringar:

- Ny `app/Tasks/TaskStoreV1.cs` med lokala tasks i `data/tasks/tasks.json`.
- Nya task-intents: lista, status, add, done och sÃ¶k.
- Task-skrivningar gÃ¥r via `PendingApprovalTypeV1.TaskChange`.
- Ã–versiktspanelen visar nu livearbete, bakgrundsjobb, tasks, pending,
  terminal/build och en mini-agent som animeras nÃ¤r arbete pÃ¥gÃ¥r.
- Panelen har snabbknappar fÃ¶r index, audit, jobbstatus, tasks och terminal.
- Panelen har Ã¤ven snabb task-input med rÃ¶d/orange/blÃ¥ prioritet; den skickar
  `/task add ...` och gÃ¥r fortfarande via pending approval.
- `Program.cs` registrerar livearbete nÃ¤r Jarvis tÃ¤nker, skapar pending filer,
  gÃ¶r kodfÃ¶rslag, kÃ¶r terminal eller Ã¤ndrar tasks.
- Ny regression: `tests/tasks-monitor-panel.test.js`.

Verifiering:

- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerade.
- Full node-regression passerade: 35 tester.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och kÃ¤nd `MSB3277`.

## 2026-05-13 - Background status och token/context-estimat

UtgÃ¥ngspunkt: anvÃ¤ndaren vill att Jarvis ska sÃ¤ga mer medan den tÃ¤nker eller
jobbar i bakgrunden, inklusive token/context nÃ¤r det gÃ¥r.

Ã„ndringar:

- Ny `app/Brain/ContextBudgetEstimatorV1.cs`.
- Vanliga Ollama-svar visar nu ungefÃ¤rligt `ctxâ‰ˆ...` och `svarâ‰ˆ...`.
- Background jobs sparar `CurrentStep`, `LastAction`, `NextAction` och
  `ContextEstimateTokens`.
- `/jobb status` visar steg, token/context-estimat och nÃ¤sta handling.
- Startmeddelanden fÃ¶r project index/audit sÃ¤ger att token/context rapporteras
  nÃ¤r jobbet bÃ¶rjar.
- Ny regression: `tests/background-status-token.test.js`.

Verifiering:

- `node F:\Jarvis-clean\tests\background-status-token.test.js` passerade.
- `node F:\Jarvis-clean\tests\background-jobs-architecture.test.js` passerade.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors
  och kÃ¤nd `MSB3277`.

## 2026-05-13 - BrowserPolicyV1 runtime

UtgÃ¥ngspunkt: anvÃ¤ndaren vill att Jarvis inte ska anvÃ¤nda andra synliga
browsers Ã¤n OperaGX/Opera, men godkÃ¤nde rekommendationen att intern
agent-automation fÃ¥r anvÃ¤nda isolerad Playwright Chromium nÃ¤r det Ã¤r tekniskt
bÃ¤ttre.

Ã„ndringar:

- Ny `app/Desktop/BrowserPolicyV1.cs`.
- `SafeAppLauncher` har inte lÃ¤ngre Chrome, Edge eller Firefox som synliga
  launch-mÃ¥l.
- `open browser`, `webblÃ¤sare`, `opera gx` och `operagx` routas till Opera.
- Explicit Chrome, Edge, Firefox och Chromium blockeras som synliga launch-mÃ¥l.
- WebbsÃ¶k/helptext sÃ¤ger Google i OperaGX/Opera.
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

