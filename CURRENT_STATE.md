# CURRENT_STATE.md - Jarvis

Senast uppdaterad: 2026-05-17

## 2026-05-17 - Sprint 2 + 3 startade

Sprint 2 (tool-calling) foundation klar:
- `data/agent/tools.json` med 12 tools (search_web, open_file, write_file,
  run_terminal, navigate_panel, speak, create_task, read_project_index,
  open_url, show_widget, close_widget, finish).
- `app/Agent/ToolsRegistryV1.cs`, `AgentLoopV1.cs`, `AgentToolExecutorV1.cs`.
- Hooket in i `ProcessUserChatAsync` bakom `VoiceConfigV1.AgentMode` (default false).
- Tool-loop kor max 5 iterations. Risky tools (write_file, run_terminal) returnerar
  pending-approval, sa LLM ser krav pa godkannande utan att utfora destruktivt.
- Verifierat att Ollama 0.24 + qwen2.5-coder:7b stoder native tool-calling.
- Modellpaket nedladdade: llama3.2:3b, mistral-nemo:12b (extra tool-callable backups).

Sprint 3 (draggable scen-widgets) klar (foundation):
- `dashboard/widgets-v1.{css,js}` — JarvisWidgetsV1 namespace.
- Widget-typer: image, iframe, webcam, video, text, chat-mini, html.
- Drag i header, resize i nedre hogra hornet. Position+storlek persisteras
  per typ i localStorage.
- Widget-tools (show_widget / close_widget) i agent-loop sa LLM kan
  skapa/stanga widgets via "visa kameran", "spela spotify" etc.
- HUD-styling (cyan borders, glow, Jarvis-feeling).

## 2026-05-17 - Agentic Roadmap startad

Stort 7-sprint-spar inlagt i TODO_NEXT.md for att gora Jarvis agentisk.
Sprint 1 (auto-research i scen med bilder) ar i progress.
Resten i ordning: tool-calling (2), widgets (3), browser-autopilot (4),
Browserbase (5), desktop+live-screen (6), self-test (7).

MCP-server-undersokning klar: MCP behovs inte i runtime (Ollama talar inte MCP),
men MCP-klient kan laggas till som Sprint 2b for att fa gratis access till
Anthropic + community MCP-servers (filesystem, fetch, brave-search, github, etc.).

## 2026-05-17 - Sofie Neural TTS + Groq Whisper STT + wake-word + Siri-orb

- TTS: `VoiceTtsEdgeV1` mot Microsoft Edge TTS (Sec-MS-GEC token), default sv-SE-SofieNeural.
- TTS fallbacks: `VoiceTtsElevenLabsV1` (REST) + `VoiceTtsSapiV1` (lokal SAPI via COM).
- STT: `VoiceSttGroqV1` mot Whisper-large-v3-turbo via Groq API (~0.5s, free tier).
- Wake-word: `WakeWordListenerV1` med VAD + svenska Whisper-aliases (hjarvis/joris m.fl.).
- Barge-in: avbryt Sofie genom att borja prata mitt i ett svar.
- Stop-ord: "vanta" / "hejda" terminerar dialog.
- Siri-orb (ovre hogra hornet): conic-sweep + HUD-ringar + organic core + sinus-wave.
- Scen + karta full-screen som brain (`body.scene-mode` + `body.map-mode`).
- Karta-pollers pausar per panel + brain 3D-render pausar nar inte aktiv.
- `Starta-Jarvis.vbs` pekar nu pa `dist-v4` (dist/ last av Session-0 instance).
- Commit `ba35a40` pushad till GitHub.

## 2026-05-16 - Fas 3 V2 migration FORSOKT och ROLLBACKAD

- Migrerade forst `HandleSceneShowAsync` till `jarvisApplySceneV2`.
- Visuell test: typeOn-animation pa summary, progressiv source-card stream
  och skeleton-shimmer mellan faser FORSVANN — `applySceneV2` kor
  `_resetSceneSlots()` vid varje push och bygger om DOM destruktivt.
- Rollback: `HandleSceneShowAsync` aterstalld till V1-flodet (jarvisAddSceneCardV1,
  jarvisUpdateSceneHeroV1, jarvisUpdateSceneSummaryV1). V2-renderern dormant igen.
- `scene-renderer-v1.js` + `SceneComposerV1.cs` kvar i kodbasen — V2-migration
  kraver forst att rendereren blir animations-medveten (smart diff istallet
  for reset+rebuild).

## 2026-05-16 - Cinematic Workspace Pro Fas 3 foundation

- `SceneComposerV1.cs` (V2-schema: 7 layouttyper, FromV1-adapter).
- `dashboard/scene-renderer-v1.js` (read-only dispatcher per layout).
- V1 ScenePayload-flodet orort — bakatkompatibelt.

## 2026-05-16 - Docs-konsolidering

- 14 duplicate plan/master/index-filer arkiverade till `archive/`.
- `MASTER_PLAN.md` ar nu enda kallan (runtime-refererad).
- Root .md-filer: 24 → 12.


## 2026-05-13 - BrowserPolicyV1

Jarvis har nu en tydlig browser-policy:

- Synlig browser för användaren är OperaGX/Opera.
- Chrome, Edge, Firefox och Chromium är blockerade som synliga launch-mål.
- Intern agent-automation får använda isolerad Playwright Chromium när det är
  tekniskt säkrare eller stabilare.
- `SafeAppLauncher` och WebSearcher följer policyn.
- Regression finns i `tests/browser-policy.test.js`.

## 2026-05-13 - Background status/token

Jarvis visar nu mer Amy-lik arbetsstatus:

- Vanliga Ollama-svar får ungefärligt `ctx≈...` och `svar≈...`.
- Background jobs sparar och visar steg, senaste handling, nästa handling och
  token/context-estimat.
- `/jobb status` är mer informativt medan Jarvis jobbar.
- Regression finns i `tests/background-status-token.test.js`.

## 2026-05-13 - Panel-first monitor och tasks

Jarvis har nu första panel-first-slicen:

- Översiktspanelen visar livearbete, bakgrundsjobb, tasks, pending, terminal och mini-agent.
- Snabbknappar i panelen startar index, audit, jobbstatus, tasks och terminalstatus.
- Snabb task-input i panelen skapar pending tasks med röd/orange/blå prioritet.
- `TaskStoreV1` sparar lokala tasks i `data/tasks/tasks.json`.
- Task-skrivningar går via `PendingApprovalV1` med `TaskChange`.
- Prioritet följer Amy-idén: röd, orange och blå.
- Regression finns i `tests/tasks-monitor-panel.test.js` och C# routertest.

## 2026-05-13 - HybridModelRouterV1 for gratis/online-iden

Forsta sakra hybrid-slicen ar pa plats:

- `ContextPackV1` bygger avgransad kontext fran minne, project index, tasks,
  aktiv fil och senaste terminalsummary.
- `HybridModelRouterV1` har lagena lokal Ollama och auto gratis/online.
- Online providers lases bara fran env vars: `GROQ_API_KEY`, `GEMINI_API_KEY`
  och `GITHUB_TOKEN`. Inga nycklar sparas i repo eller status.
- Sprakmodellen ar radgivare/tolk: den far inte kora tools, skriva filer,
  klicka eller kora terminal. Jarvis utfor via lokala tools och pending approval.
- Oversiktspanelen visar nu Modellmotor.

## 2026-05-13 - Agent Autopilot Modes V1

Jarvis har nu en central autopilot-niva for Amy-kraften:

- Niva 1 Safe: lasa, soka, analysera och oppna sakert.
- Niva 2 Approval: riskmoment blir pending approval.
- Niva 3 Browser Autopilot: OperaGX/Opera synligt, isolerad Chromium internt.
- Niva 4 Desktop Autopilot: BroadDesktopControl for nastan alla normala appar,
  men denylist, scope och kill-switch galler.
- Niva 5 Build Agent: jobbar i `F:\Jarvis-clean`, aldrig i `F:\New project`.
- Slash: `/autopilot status`, `/autopilot approval`,
  `/autopilot browser <uppdrag>`, `/autopilot desktop <uppdrag>`,
  `/autopilot build <uppdrag>` och `/autopilot stop`.
- Oversiktspanelen visar Autopilot-status.
- Browser Autopilot Runner V1 kan oppna/soka/lasa URL via Opera-policy.
- Desktop Autopilot Runner V1 foreslar ett UI-TARS-steg i taget och varje
  click/type/hotkey gar fortfarande via pending approval.

## 2026-05-12 - Background Jobs / Project Index MVP första kodslice

Första strukturerade runtime-slicen för Project Index + Background Jobs är på plats:

- Ny modul `app/Jobs/` i stället för att lägga mer logik i `Program.cs`.
- `BackgroundJobQueueV1` startar, listar, visar status och avbryter background jobs.
- `ProjectIndexServiceV1` gör read-only scan av projektfiler, exkluderar generated mappar och skriver metadata till `data/project-index/index.json`.
- Slash: `/jobb`, `/jobb status`, `/jobb start`, `/jobb avbryt`, `/projekt index`.
- Naturligt språk: `analysera projektet`, `läs hela repo`, `gå igenom allt`, `förstå projektet`, `skapa audit`.
- Jarvis svarar direkt och indexerar i bakgrunden.

Verifiering:

- Nytt test: `tests/background-jobs-architecture.test.js`.
- C# routertest täcker `/jobb`, `/projekt index` och faktisk temp-indexering.
- Full node-regression passerade.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` passerade med 0 errors och känd `MSB3277`.

## 2026-05-12 - Project Index incremental/search/audit slice

Andra runtime-slicen byggde vidare på samma Jobs-arkitektur:

- `ProjectIndexServiceV1` gör incremental scan och återanvänder hash/summaries för oförändrade filer.
- Indexet skriver fil- och mappsammanfattningar till `data/project-index/files` och `data/project-index/folders`.
- `data/project-index/search.jsonl` skapas för enkel lokal text/symbol/TODO-sökning.
- Ny `ProjectIndexSearchServiceV1` ger `/projekt sök <query>` och smal RAG-kontext innan vanlig Ollama-chat.
- Ny `ProjectAuditServiceV1` skapar en läsbar `result.md` från projektindex.
- `/projekt audit` och `skapa audit` startar read-only audit som background job.
- Background jobs skriver nu `log.md` och `result.md` under `data/jobs/<job-id>`.

## Produktroll

- `cleen-jarvis` är huvudprodukten.
- Lokal arbetsmapp är `F:\Jarvis-clean`.
- `f-drive-projects` är referens, backup och inspiration.
- `F:\New project` är read-only reference och får inte ändras.
- GitHub är källan externa AI-agenter kan läsa från.

## Nuvarande status

Jarvis är redan en fungerande lokal Windows/C# produkt med:

- C# WinForms/WebView2 dashboard
- Project Explorer
- filpanel/kodvisare
- terminalpanel
- Jarvis-chat med lokal Ollama
- lokalt markdown-minne
- CommandRouter V1
- CommandValidator V1
- ToolRegistry V1
- PendingApproval V1
- safe file write/delete/undo-loop
- approval popup
- review/diff UI
- ModelRouter
- ConversationHistory
- WebSearcher via browser
- SafeAppLauncher
- BuilderMode
- NaturalEditTool
- desktop-control via pending approval
- vault/AI-kontext
- brain-vy
- tester och säkerhetsregler

## Huvudprioritet

Nästa riktiga build ska vara:

**Jarvis Project Index + Background Jobs MVP**

Detta prioriteras före Kartan, liveflyg, livebåtar, avancerad 3D Earth,
weather-animationer och andra stora future-features.

Se [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md).

## Vad som saknas mest

Det saknas inte fler stora idéer. Det saknas bättre:

- produktordning
- background jobs
- projektindex
- Program.cs-refaktor
- bättre job pause/resume
- build/test/push-loop

## GitHub-sync

Efter större lyckad ändring i `cleen-jarvis`:

1. Kontrollera `git status`.
2. Kör relevant build/test.
3. Stage:a bara avsedda filer.
4. Commit och push.

Om ändringar bara finns lokalt men inte är pushade kan externa AI-agenter inte
läsa dem från GitHub.

## Historik

Den tidigare långa `CURRENT_STATE.md` är bevarad i delar för att hålla alla
Markdown-filer under 14 000 tecken:

- [PART 01](CURRENT_STATE_PART_01.md)
- [PART 02](CURRENT_STATE_PART_02.md)
- [PART 03](CURRENT_STATE_PART_03.md)
- [PART 04](CURRENT_STATE_PART_04.md)
