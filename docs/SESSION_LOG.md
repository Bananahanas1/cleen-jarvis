# SESSION_LOG.md

## 2026-05-13 - HybridModelRouterV1 + ContextPackV1

Utgangspunkt: anvandaren vill att sprakmodeller ska vara de man bollar ideer
med och som hjalper Jarvis forsta naturligt sprak, men utan att modellerna far
fri skriv-, terminal- eller desktopmakt.

Andringar:

- Skapade `app/Brain/ContextPackV1.cs`.
- Skapade `app/Brain/HybridModelRouterV1.cs`.
- `Program.cs` har nu hybrid chat fallback efter lokal router och safe tools.
- Online providers kan anvandas via env vars: `GROQ_API_KEY`, `GEMINI_API_KEY`
  och `GITHUB_TOKEN`.
- `/modell provider` visar backendstatus.
- `/modell lage lokal` och `/modell lage auto` styr lokal/auto-free.
- Oversiktspanelen visar Modellmotor.

Sakerhetslinje:

- LLM ar radgivare/tolk, inte aktor.
- Jarvis ager kontext, routing, validation, pending approval och tools.
- Secrets sparas inte i repo eller status.

Verifiering:

- TDD red: `tests/hybrid-model-router-context.test.js` failade forst pa saknade
  filer och kopplingar.
- Green: `node F:\Jarvis-clean\tests\hybrid-model-router-context.test.js`
  passerade.
- Full node-regression passerade: 36 tester.
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj`
  passerade.
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

## 2026-05-12 - Background Jobs / Project Index första runtime-slice

Utgångspunkt: användaren ville få ordning på kodarkitekturen och arbeta enligt
MD-filerna. Eftersom planerna säger Project Index + Background Jobs före Kartan
blev första kodslicen en ny Jobs-modul i stället för mer logik i `Program.cs`.

Ändringar:

- Ny mapp `app/Jobs/`.
- Ny `BackgroundJobQueueV1` med job data model, in-memory queue, status/list/cancel och statuspersistens under `data/jobs`.
- Ny `ProjectIndexServiceV1` som gör read-only scan, exkluderar `.git`, `.checkpoints`, `bin`, `obj`, `dist`, `node_modules`, `graphify-out` och `EBWebView`.
- Indexet skriver metadata och SHA256 till `data/project-index/index.json`.
- `CommandRouterV1` har nu `ProjectIndex`, `JobList`, `JobStatus`, `JobCancel`.
- Slash-kommandon: `/jobb`, `/jobb status`, `/jobb start`, `/jobb avbryt`, `/projekt index`.
- Naturliga fraser som `analysera projektet` och `läs hela repo` startar background project index lokalt före Ollama.
- `Program.cs` delegerar till `BackgroundJobQueueV1` i stället för att bära jobblogiken själv.
- Dashboard autocomplete och hjälptext nämner `/jobb`.

Verifiering:

- TDD red: `tests/background-jobs-architecture.test.js` failade först på saknad `app/Jobs` och saknade intents.
- TDD red: `CommandRouterV1.Tests` failade först eftersom `JobList`, `JobStatus`, `JobCancel` saknades.
- Ny faktisk C#-verifiering bygger ett index i tempmapp och bekräftar att `bin` exkluderas.
- Full node-regression passerade.
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerade.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors och känd `MSB3277`.
- `dotnet publish F:\Jarvis-clean\app\JarvisClean.csproj -c Release -o F:\Jarvis-clean\dist --no-self-contained` passerade med känd `MSB3277`.
- Startade via `wscript F:\Jarvis-clean\Starta-Jarvis.vbs`.
- Observerad process: `Jarvis.exe` PID 30432, path `F:\Jarvis-clean\dist\Jarvis.exe`.
- Notering: kombinerat PowerShell-kommando returnerade exit code 1 efter `wscript`, men publish-output var lyckad och processen verifierades som körande.
- Slutkontroll hittade `docs/JARVIS_NEXT_LEVEL_SUPERPLAN.md` strax över 14 000 tecken; risksektionen flyttades till `docs/JARVIS_NEXT_LEVEL_SUPERPLAN_PART-02.md`.

Kvar:

- Incremental scan med återanvända hashes.
- Sökning/RAG mot projektindex.
- Fortsatt lugn refaktor ur `Program.cs` till services.

## 2026-05-12 - Documentation split + Project Index/Background Jobs priority

Utgångspunkt: användaren bad att läsa `JARVIS_UPDATE_TASK.md` från början till
slut och faktiskt uppdatera filerna. Detta var ett docs-only pass.

Ändringar:

- Skapade `JARVIS_MEGA_MASTER_PROMPT.md` som kort index, inte jätteprompt.
- Skapade `PLANNING_INDEX.md`.
- Skapade `JARVIS_CORE_INDEX.md`.
- Skapade `KARTAN_INDEX.md`.
- Skapade `JARVIS_BACKGROUND_JOBS_PLAN.md`.
- Skapade `NEXT_AI_AGENT_TODO.md`.
- Skapade `JARVIS_MASTER_PLAN.md`.
- Skapade `CURRENT_PROJECT_AUDIT.md`.
- Uppdaterade `README.md`, `CURRENT_STATE.md`, `TODO_NEXT.md`, `MASTER_PLAN.md`
  och `docs/PROJECT_INDEX.md`.
- Dokumenterade att `cleen-jarvis` är huvudprodukten, `f-drive-projects` är
  referens/backup/inspiration och `F:\New project` är read-only reference.
- Dokumenterade GitHub-sync-regeln.
- Dokumenterade att Project Index + Background Jobs MVP går före Kartan.
- Delade alla Markdown-filer över 14 000 tecken i PART-filer.

Verifiering:

- Markdown-längdkontroll körd: alla `.md` är under 14 000 tecken.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` lyckades med 0 errors
  och känd `MSB3277` WindowsBase/WebView2-warning.
- Docs-only arbete: ingen publish/restart av Jarvis.

Kvar:

- Commit/push om build och git-läge tillåter.

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
