# TODO_NEXT.md - nÃ¤sta praktiska steg

Senast uppdaterad: 2026-05-17

## Jarvis Agentic Roadmap 2026-05-17

Stort spar: gora Jarvis till en agentisk assistent som kan utfora uppgifter
sjalv (program, web, desktop) och visa allt i scen-vyn. Ska delas upp i 7 sprintar.
Anvandaren begar att alla sprintar gors i ordning 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7.

### Sprint 1 (i progress) - Auto-research i scen med bilder
- [ ] Forbattra `SceneResearchV1` med DuckDuckGo Image Search-source
- [ ] Lagg till Wikimedia Commons image-API for fria bilder
- [ ] Lagg till HTML content scraping av forsta hit (HtmlAgilityPack eller manuell parse)
- [ ] Visa 3-5 bilder i scen-vyn istallet for bara hero
- [ ] Auto-trigga scen nar user fragar i chatten (om query bedoms research-vard)
- [ ] Test: scen fylls med relevanta bilder + sammanfattning utan att user explicit ber

### Sprint 2 - Tool-calling framework (hjartat i agentic Jarvis)
- [ ] Designa `tools.json` med tool-definitions (open_file, run_cmd, search_web, ui_click, read_screen, etc.)
- [ ] System-prompt template som ger LLM tool-list + svensk instruktion
- [ ] `ToolCallRouterV1` parsar LLM-output for tool-calls (JSON eller XML-format)
- [ ] Execute-pipeline med `PendingApprovalV1` for risky tools (write, click, exec)
- [ ] Tool-result feedback-loop sa LLM kan reagera pa resultat och kalla fler tools
- [ ] Stop-condition: max N tool-calls per query, eller LLM signalerar klar
- [ ] Logg per tool-call i `data/agent/runs/<id>.jsonl`

#### Sprint 2b (optionellt) - MCP-klient
- [ ] Implementera MCP-protokoll (JSON-RPC over stdio) som klient
- [ ] Lat user konfigurera MCP-servers i `config/mcp-servers.json`
- [ ] Auto-discover tools fran connected MCP-servers
- [ ] Routa tool-call till ratt MCP-server eller native handler
- [ ] Gratis MCP-servers att testa: filesystem, fetch, brave-search, github, memory

### Sprint 3 - Draggable scene-widgets
- [ ] Bygg `WidgetV1`-komponent i `index.html` (header med title + close + drag-handle)
- [ ] Mouse-drag positioning, position sparas i localStorage per widget-typ
- [ ] Predefinierade widgets:
  - `chat-mini` (kompakt chat-vy mellan user och Jarvis)
  - `webcam` (visar webcam-stream fran karta-source)
  - `image-viewer` (visar bilder fran scen-research)
  - `info-panel` (visar nyckeldata om aktiv query)
  - `code-snippet` (visar kod-result fran tool-calls)
- [ ] `/widget <typ>` command for att skapa nya widgets
- [ ] Widgets snap till grid for snyggt placering

### Sprint 4 - Browser autopilot (Playwright deepening)
- [ ] Polera existerande `BrowserAutopilotRunnerV1` for langre uppdrag
- [ ] Smartare login-detection (Gmail, GitHub, common SaaS)
- [ ] Approval per click/form/submit med screenshot
- [ ] 2FA-handoff: pause + Jarvis fragar user innan fortsatt
- [ ] Multi-step recipes (t.ex. "boka Apoteket-tid")
- [ ] Page reading: extrahera readable content + screenshots av aktiv sida
- [ ] Kanske integrera Browserbase eller behall lokal Playwright

### Sprint 5 - Browserbase web scraper-integration
- [ ] Skapa `config/browserbase.json.example` template
- [ ] `BrowserbaseClientV1.cs` for REST-API (sessions, navigate, scrape)
- [ ] Stealth/AI-driven scraping for sidor som blockar lokal Playwright
- [ ] Anvandning fran scen-research nar lokal scraping failar
- [ ] Anvandning fran browser-autopilot for hard sidor

### Sprint 6 - Desktop control + live screen feed
- [ ] Forbattra `UI-TARS`-integration (redan delvis byggd)
- [ ] Live screenshot via `PrintWindow` API (alternative: BitBlt eller WGC)
- [ ] Stream screenshots till WebView (canvas eller img.src= dataURL var Nms)
- [ ] Tool: `desktop_click(x, y)`, `desktop_type(text)`, `desktop_screenshot()`
- [ ] Tool: `desktop_open_app(name)` via SafeAppLauncher
- [ ] Action recording sa Jarvis kan "replay" tidigare flows

### Sprint 7 - Self-test runner
- [ ] `/test alla paneler`-kommando
- [ ] Spawnar parallel Jarvis-instans i headless mode (WebView2 utan synligt fonster?)
- [ ] Klickar alla tabs sekventiellt, verifierar att inget kraschar
- [ ] Visual regression: screenshot per panel + diff mot baseline
- [ ] Report till `data/agent/test-runs/<timestamp>.md`
- [ ] CI-like check: kor mellan releases for att fanga regressions

## Git hygiene 2026-05-15

## Git hygiene 2026-05-15

- [x] Stoppa `git-lfs filter-process`-stormen genom att ignorera secrets,
  checkpoints, build-output, runtime-data, venv/node_modules och stora assets.
- [x] Lagg regression `tests/gitignore-safety.test.js`.
- [ ] Senare: se over `.gitattributes`; `*.json filter=lfs` ar brett och kan
  vara onodigt tungt for vanliga config/test-json.

## Desktop launcher 2026-05-15

- [x] Skapa skrivbordsgenvag `Jarvis Clean - senaste.lnk` som pekar pa
  `F:\Jarvis-clean\Starta-Jarvis.vbs`.

## Karta-utvidgning 2026-05-15

- [x] Lägg `TRAFIKLAB_API_KEY` i `EnvVaultV1`-whitelist.
- [x] Lägg `karta_transit_departures`-handler i Program.cs med
  ResRobot nearbystops + departureBoard.
- [x] Visa "Nästa avgångar"-sektion i POI-sidopanelen vid klick på
  busshållplats/spårvagn/tåg-station.
- [x] Höj alla OSM-symbol-lager (text + ikoner) ovanpå 3D-byggnader
  så de aldrig döljs vid tilt; återapplicera vid styledata-event.
- [x] Regression `tests/karta-transit-and-label-z-order.test.js`.
- [ ] Användare: registrera gratis Trafiklab-nyckel på trafiklab.se
  ("ResRobot - Stops and Departures") och lägg in via Inställningar.
- [ ] Manuell visuell verifiering: tilt > 30° över Lund/Malmö, klicka
  busshållplats, kontrollera att text syns ovanpå byggnader och
  tidtabell laddas.
- [ ] Senare: auto-refresh av avgångar var 30:e sek när panelen är öppen.

## Huvudprioritet

NÃ¤sta riktiga build:

**Jarvis Project Index + Background Jobs MVP**

Detta gÃ¥r fÃ¶re Kartan, liveflyg, livebÃ¥tar, avancerad 3D Earth,
weather-animationer och andra stora future-features.

## Aktiv nÃ¤sta-lista

- [x] Skapa job data model fÃ¶r background jobs.
- [x] Skapa enkel job queue och background worker.
- [x] LÃ¤gg till status/progress/log/result fÃ¶r jobs.
- [x] LÃ¤gg till `/jobb`, `/jobb status`, `/jobb start`, `/jobb avbryt`.
- [x] Starta read-only project scan nÃ¤r anvÃ¤ndaren ber Jarvis lÃ¤sa/analysera allt.
- [x] Svara direkt: "Jag bÃ¶rjar lÃ¤sa och indexera projektet i bakgrunden. Du kan fortsÃ¤tta skriva under tiden."
- [x] Skapa `data/project-index` med filmetadata, modified time och filhash.
- [x] GÃ¶r scan incremental: Ã¥teranvÃ¤nd hash/summaries fÃ¶r ofÃ¶rÃ¤ndrade filer.
- [x] LÃ¤gg till summaries per fil och mapp.
- [x] LÃ¤gg till enkel sÃ¶kning och RAG/smart context frÃ¥n projektindex.
- [x] LÃ¤gg till deep audit som background job och sparad rapport.
- [x] Dokumentera Project Index incremental/search/audit-slicen i `docs/SESSION_LOG.md`.
- [x] KÃ¶r relevanta tester och `dotnet build` efter denna runtime-Ã¤ndring.
- [x] Publish/restart efter grÃ¶n runtime-Ã¤ndring.
- [x] Commit och push efter grÃ¶na build/test fÃ¶r denna slice.

## NÃ¤sta efter denna slice

- [x] Skapa planfiler for Cinematic Workspace Pro och relaterade delsystem.
- [x] Fas 1: lagg till `dashboard/theme.css` och `dashboard/scene-pro.css`.
- [x] Fas 1: bygg pro idle screen med central orb, grid, command feed,
  system widgets och tool dock.
- [x] Fas 1: lagg regression `tests/scene-pro-phase1.test.js`.
- [x] Hotfix: pruned scan for Project Explorer och Brain Graph sa `.venv`,
  `logs` och `runtimes` inte gor folder/graph-laddning seg.
- [x] Hotfix: lagg regression `tests/project-scan-pruning-performance.test.js`.
- [ ] Fas 2: lagg `SystemHealthPanelV1` och samla repetitiva tekniska fel.
- [x] Fas 3 foundation: `SceneComposerV1.cs` (V2-schema + 7 layouter + FromV1-adapter)
  och `dashboard/scene-renderer-v1.js` (read-only, dispatchar per layouttyp).
  Test: `tests/scene-composer-phase3.test.js` 55/55 PASS.
- [ ] Fas 3 nasta slice: migrera `HandleSceneShowAsync` att skicka V2-payload
  via `jarvisApplySceneV2` istallet for V1 ScenePayload.
  Forsta forsok 2026-05-16 rolled back: V2-renderern `_resetSceneSlots()`
  raderar typeOn/source-stream/skeleton-shimmer-animationer. Behover
  animations-aware diff i `applySceneV2` innan migration kan upprepas.
- [ ] Fas 3 polish: flytta kort-rendering ur `index.html` till renderer-modulen.
- [ ] Fas 4: forbattra news brief med timeline, confirmed/uncertain, impact
  och next questions.
- [ ] LÃ¤gg till riktig pause/resume fÃ¶r background jobs.
- [x] LÃ¤gg till background status-events: steg, progress, senaste handling och token/context-estimat dÃ¤r det gÃ¥r.
- [ ] LÃ¤gg till mer avancerad chunk/map-reduce summary vid stora filer.
- [x] LÃ¤gg till `BrowserPolicyV1`: OperaGX/Opera som enda synliga browsermÃ¥l och isolerad Playwright Chromium som intern automation engine.
- [ ] AnvÃ¤nd `BrowserPolicyV1` nÃ¤r framtida Browser Autopilot byggs.
- [ ] FortsÃ¤tt Program.cs-refaktor: flytta terminal-, memory- och file-tool-logik till smÃ¥ services.

## Amy Windows Autopilot beslut 2026-05-13

- [x] Dokumenterade att Amy-idÃ©n ska porteras Windows-native, inte kopieras frÃ¥n macOS.
- [x] Dokumenterade OperaGX/Opera-only browser policy.
- [x] Dokumenterade scoped Autopilot i stÃ¤llet fÃ¶r permanent fri datormakt.
- [x] Dokumenterade att bakgrundsarbete ska visa kort arbetsstatus och token/context-estimat nÃ¤r mÃ¶jligt.
- [x] Skapade `docs/AMY_WINDOWS_AUTOPILOT_PLAN.md`.

## Amy parity runtime 2026-05-13

- [x] Synlig browser-policy: OperaGX/Opera only.
- [x] Intern browser-agent-motor: isolerad Playwright Chromium.
- [x] Token/context-estimat i vanliga Ollama-svar.
- [x] Steg, nÃ¤sta handling och token/context-estimat i `/jobb status`.
- [x] Lokal TaskStore V1 med rÃ¶d/orange/blÃ¥ prioritet och pending approval fÃ¶r task-skrivning.
- [x] Panel-first monitor i Ã–versikt: livearbete, bakgrundsjobb, tasks, pending, terminal och mini-agent.
- [x] Snabb task-input i Ã–versikt sÃ¥ tasks kan skapas visuellt utan att minnas `/task add`.

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
- [x] UI-TARS package manager fallback: `/desktop tars start` letar efter pnpm och faller tillbaka till corepack pnpm innan den ger installationsrad.

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
- [x] Brain Relations-First: dolj runtime/generated-brus, bygg stabil graf-meta
  och prioritera riktiga kod/vault/markdown-relationer.

## Amy Windows Standalone 2026-05-14

- [x] Skapa separat `amy-windows/` med egen `.env`, FastAPI backend, SQLite och Vite/Three.js dashboard.
- [x] Installera Amy dependencies i `.venv` via Python 3.13 och `frontend/node_modules`.
- [x] Lagg till setup-status for Claude, Groq Whisper, ElevenLabs och fal.ai utan secret-lackage.
- [x] Lagg till approval-first Playwright browser-plan som startar i dry-run utan nycklar.
- [ ] Koppla riktiga Claude/Groq/ElevenLabs/fal-floden nar API-nycklar laggs i `amy-windows/.env`.

## Produktroll

- `cleen-jarvis` Ã¤r huvudprodukt.
- `F:\Jarvis-clean` Ã¤r lokal arbetsmapp.
- `f-drive-projects` Ã¤r referens/backup/inspiration.
- `F:\New project` Ã¤r read-only reference.

## Docs-pass 2026-05-12

- [x] LÃ¤ste `JARVIS_UPDATE_TASK.md`.
- [x] Delade lÃ¥nga Markdown-filer i PART-filer.
- [x] Skapade `JARVIS_MEGA_MASTER_PROMPT.md`.
- [x] Skapade `PLANNING_INDEX.md`.
- [x] Skapade `JARVIS_CORE_INDEX.md`.
- [x] Skapade `KARTAN_INDEX.md`.
- [x] Skapade `JARVIS_BACKGROUND_JOBS_PLAN.md`.
- [x] Skapade `NEXT_AI_AGENT_TODO.md`.
- [x] Skapade `JARVIS_MASTER_PLAN.md`.
- [x] Skapade `CURRENT_PROJECT_AUDIT.md`.
- [x] Verifierade att alla Markdown-filer Ã¤r under 14 000 tecken.
- [x] KÃ¶r `dotnet build` fÃ¶r docs-only sanity check: 0 errors, kÃ¤nd `MSB3277`.

## Historik

Den tidigare lÃ¥nga TODO-listan Ã¤r bevarad i delar:

- [PART 01](TODO_NEXT_PART_01.md)
- [PART 02](TODO_NEXT_PART_02.md)
- [PART 03](TODO_NEXT_PART_03.md)

