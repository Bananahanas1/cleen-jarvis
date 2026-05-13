# TODO_NEXT.md - nästa praktiska steg

Senast uppdaterad: 2026-05-13

## Huvudprioritet

Nästa riktiga build:

**Jarvis Project Index + Background Jobs MVP**

Detta går före Kartan, liveflyg, livebåtar, avancerad 3D Earth,
weather-animationer och andra stora future-features.

## Aktiv nästa-lista

- [x] Skapa job data model för background jobs.
- [x] Skapa enkel job queue och background worker.
- [x] Lägg till status/progress/log/result för jobs.
- [x] Lägg till `/jobb`, `/jobb status`, `/jobb start`, `/jobb avbryt`.
- [x] Starta read-only project scan när användaren ber Jarvis läsa/analysera allt.
- [x] Svara direkt: "Jag börjar läsa och indexera projektet i bakgrunden. Du kan fortsätta skriva under tiden."
- [x] Skapa `data/project-index` med filmetadata, modified time och filhash.
- [x] Gör scan incremental: återanvänd hash/summaries för oförändrade filer.
- [x] Lägg till summaries per fil och mapp.
- [x] Lägg till enkel sökning och RAG/smart context från projektindex.
- [x] Lägg till deep audit som background job och sparad rapport.
- [x] Dokumentera Project Index incremental/search/audit-slicen i `docs/SESSION_LOG.md`.
- [x] Kör relevanta tester och `dotnet build` efter denna runtime-ändring.
- [x] Publish/restart efter grön runtime-ändring.
- [x] Commit och push efter gröna build/test för denna slice.

## Nästa efter denna slice

- [ ] Lägg till riktig pause/resume för background jobs.
- [x] Lägg till background status-events: steg, progress, senaste handling och token/context-estimat där det går.
- [ ] Lägg till mer avancerad chunk/map-reduce summary vid stora filer.
- [x] Lägg till `BrowserPolicyV1`: OperaGX/Opera som enda synliga browsermål och isolerad Playwright Chromium som intern automation engine.
- [ ] Använd `BrowserPolicyV1` när framtida Browser Autopilot byggs.
- [ ] Fortsätt Program.cs-refaktor: flytta terminal-, memory- och file-tool-logik till små services.

## Amy Windows Autopilot beslut 2026-05-13

- [x] Dokumenterade att Amy-idén ska porteras Windows-native, inte kopieras från macOS.
- [x] Dokumenterade OperaGX/Opera-only browser policy.
- [x] Dokumenterade scoped Autopilot i stället för permanent fri datormakt.
- [x] Dokumenterade att bakgrundsarbete ska visa kort arbetsstatus och token/context-estimat när möjligt.
- [x] Skapade `docs/AMY_WINDOWS_AUTOPILOT_PLAN.md`.

## Amy parity runtime 2026-05-13

- [x] Synlig browser-policy: OperaGX/Opera only.
- [x] Intern browser-agent-motor: isolerad Playwright Chromium.
- [x] Token/context-estimat i vanliga Ollama-svar.
- [x] Steg, nästa handling och token/context-estimat i `/jobb status`.
- [x] Lokal TaskStore V1 med röd/orange/blå prioritet och pending approval för task-skrivning.
- [x] Panel-first monitor i Översikt: livearbete, bakgrundsjobb, tasks, pending, terminal och mini-agent.
- [x] Snabb task-input i Översikt så tasks kan skapas visuellt utan att minnas `/task add`.

## Agent Autopilot Modes V1 2026-05-13

- [x] Skapa central `AgentAutopilotModeV1` med Safe, Approval, Browser Autopilot, Desktop Autopilot och Build Agent.
- [x] Gora Desktop Autopilot till BroadDesktopControl for nastan alla normala appar, med denylist och kill-switch i stallet for liten whitelist.
- [x] Lagg till `/autopilot status`, `/autopilot approval`, `/autopilot browser <uppdrag>`, `/autopilot desktop <uppdrag>`, `/autopilot build <uppdrag>` och `/autopilot stop`.
- [x] Visa Autopilot i Oversiktspanelen.
- [x] Browser Autopilot Runner V1: oppna/sok/lasa URL via Opera-policy och blocka login/betalning/secrets/skicka/publicera.
- [ ] Lagg till kontrollerad click/type for Browser Autopilot efter starkare sida/form-riskklassning.
- [x] Desktop Autopilot Runner V1: foreslar ett UI-steg i taget via pending approval, med kill-switch, denylist och maxsteg.
- [x] Desktop Autopilot auto-continue UI: Oversikt visar Fortsatt Autopilot nar desktop-uppdrag kan fortsatta efter godkand action.
- [x] Desktop Autopilot local app fallback: enkla uppdrag som `oppna notepad` gar via SafeAppLauncher innan UI-TARS kravs.
- [x] Oversikt command panels: snabbknapparna ar grupperade i visuella paneler med kort forklaring.

## Agent VM Sandbox riktning 2026-05-13

- [ ] Utred Agent VM Sandbox for niva 4/5: egen Windows VM med snapshot/rollback, kill-switch och kontrollerad delad workspace.
- [ ] Lat host-Jarvis behalla chat, paneler, minne och approval; lat fri desktop-agent koeras i VM nar uppdraget kraver hog riskfrihet.
- [ ] Designa sync-regel: read-only import till VM och explicit export tillbaka till `F:\Jarvis-clean`.

## Hybrid AI router 2026-05-13

- [x] Skapa `ContextPackV1` sa Jarvis sjalv ager arbetskontexten.
- [x] Skapa `HybridModelRouterV1` med lokal Ollama och auto gratis/online-lage.
- [x] Stod env-konfig for Groq, Gemini och GitHub Models utan att spara secrets.
- [x] Visa Modellmotor i Oversiktspanelen.
- [ ] Lagg till UI-falt for att valja lokal/auto utan kommando.
- [ ] Lagg till strict JSON-intent-tolk for nar Jarvis inte forstar naturligt sprak.
- [ ] Utvardera gratis providers praktiskt med sma testprompts nar nycklar finns.

## Full system test 2026-05-13

- [x] Skapa `tests/run-full-smoke.ps1` for stort lokalt regressionstest.
- [x] Skapa `docs/FULL_SYSTEM_TEST.md` med manuell Jarvis-checklista.
- [x] Bevaka smoke-runnern med `tests/full-smoke-runner.test.js`.

## Brain graph controls 2026-05-13

- [x] Lagg till sliders i Brain Graph for Center force, Repel force och Link force.
- [x] Koppla sliders direkt till 3D force-layouten sa grafen kan spridas live.

## Produktroll

- `cleen-jarvis` är huvudprodukt.
- `F:\Jarvis-clean` är lokal arbetsmapp.
- `f-drive-projects` är referens/backup/inspiration.
- `F:\New project` är read-only reference.

## Docs-pass 2026-05-12

- [x] Läste `JARVIS_UPDATE_TASK.md`.
- [x] Delade långa Markdown-filer i PART-filer.
- [x] Skapade `JARVIS_MEGA_MASTER_PROMPT.md`.
- [x] Skapade `PLANNING_INDEX.md`.
- [x] Skapade `JARVIS_CORE_INDEX.md`.
- [x] Skapade `KARTAN_INDEX.md`.
- [x] Skapade `JARVIS_BACKGROUND_JOBS_PLAN.md`.
- [x] Skapade `NEXT_AI_AGENT_TODO.md`.
- [x] Skapade `JARVIS_MASTER_PLAN.md`.
- [x] Skapade `CURRENT_PROJECT_AUDIT.md`.
- [x] Verifierade att alla Markdown-filer är under 14 000 tecken.
- [x] Kör `dotnet build` för docs-only sanity check: 0 errors, känd `MSB3277`.

## Historik

Den tidigare långa TODO-listan är bevarad i delar:

- [PART 01](TODO_NEXT_PART_01.md)
- [PART 02](TODO_NEXT_PART_02.md)
- [PART 03](TODO_NEXT_PART_03.md)
