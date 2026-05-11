---
type: project-doc
source_file: "docs/UNIFICATION_PLAN.md"
created: 2026-05-10
tags: [project, mirrored]
---

# UNIFICATION_PLAN.md â€” Jarvis-clean + F:\New project â†’ Ett projekt

Skapad: 2026-05-09
Ã„gare: Claude Code (planerare), Codex/Claude (utfÃ¶rare)
Status: **AWAITING APPROVAL**

## MÃ¥l (frÃ¥n anvÃ¤ndaren)

1. Aktivera 3D/NeuroLinked i Jarvis-clean (idag avstÃ¤ngt enligt MASTER_PLAN/AGENTS.md).
2. LÃ¤gg till **File Explorer som sekundÃ¤rt huvudfÃ¶nster** (egen window/sida).
3. Bygg klart Jarvis-clean fÃ¶rst sÃ¥ den fungerar med dessa tillÃ¤gg.
4. Integrera sedan med `F:\New project` â†’ ETT projekt.
5. NÃ¤r konflikter: ta den bÃ¤sta lÃ¶sningen frÃ¥n respektive (gamla vs nya).
6. Uppdatera alla MD-filer sÃ¥ planen Ã¤r dokumenterad.

## SlutmÃ¥l â€” arkitektur

```
F:\Jarvis-clean\               â† unifierat projekt (slutligt hem)
â”œâ”€â”€ app\                       â† C# WinForms, multi-window
â”‚   â”œâ”€â”€ Program.cs             â† JarvisForm (huvud, 3-panel: Explorer | Editor | Chat)
â”‚   â”œâ”€â”€ BrainWindow.cs         â† NY: separat 3D-fÃ¶nster (WebView2)
â”‚   â”œâ”€â”€ FileExplorerWindow.cs  â† NY: separat fullscreen file explorer
â”‚   â”œâ”€â”€ CommandRouterV1.cs     â† bevaras
â”‚   â”œâ”€â”€ CommandValidatorV1.cs  â† bevaras
â”‚   â”œâ”€â”€ ToolRegistryV1.cs      â† bevaras
â”‚   â”œâ”€â”€ PendingApprovalV1.cs   â† bevaras
â”‚   â”œâ”€â”€ Agents\                â† NY: OllamaAgentHarness (frÃ¥n gamla, 17 verktyg)
â”‚   â”œâ”€â”€ Core\
â”‚   â”‚   â””â”€â”€ ModelCatalog.cs    â† NY: 5 modellprofiler (frÃ¥n gamla)
â”‚   â””â”€â”€ Bridges\
â”‚       â””â”€â”€ NeuroLinkedBridge.cs â† NY: opt-in start av Python-server
â”œâ”€â”€ dashboard\
â”‚   â”œâ”€â”€ index.html             â† huvud, 3-panel (befintlig)
â”‚   â”œâ”€â”€ brain.html             â† NY: 3D-visualisering
â”‚   â”œâ”€â”€ explorer.html          â† NY: file explorer fullscreen
â”‚   â”œâ”€â”€ css\style.css          â† gemensam (porteras frÃ¥n gamla)
â”‚   â”œâ”€â”€ js\
â”‚   â”‚   â”œâ”€â”€ brain3d.js         â† porterad frÃ¥n gamla (1949 rader)
â”‚   â”‚   â”œâ”€â”€ knowledge_panel.js â† porterad
â”‚   â”‚   â”œâ”€â”€ jarvis_bridge.js   â† porterad
â”‚   â”‚   â””â”€â”€ ...
â”‚   â””â”€â”€ vendor\                â† NY: Three.js + addons (offline)
â”œâ”€â”€ neurolinked\               â† NY: Python brain-service (opt-in)
â”‚   â”œâ”€â”€ server.py              â† FastAPI :8000
â”‚   â”œâ”€â”€ run.py
â”‚   â””â”€â”€ ...                    â† porterad frÃ¥n gamla
â”œâ”€â”€ python\                    â† NY: weather/news/TTS/STT/web (porterad)
â”œâ”€â”€ graphify-out\              â† NY: graph.json (porterad)
â”œâ”€â”€ tests\                     â† befintliga + nya
â”œâ”€â”€ docs\                      â† MD-filer uppdaterade
â””â”€â”€ data\, vault\, config\     â† lokal state, ofÃ¶rÃ¤ndrad
```

**Tre fÃ¶nster:**

```
â”Œâ”€ MAIN (Jarvis-fÃ¶nstret) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Explorer | Editor | Chat                                  â”‚
â”‚  Knappar: [Brain] [File Explorer] [Ã–versikt]              â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
        â†“ (klick pÃ¥ knapp eller /kommando)
â”Œâ”€ BRAIN (eget fÃ¶nster) â”€â”€â”€â”€â”€â”  â”Œâ”€ FILE EXPLORER (eget) â”€â”€â”€â”€â”
â”‚  3D NeuroLinked-visualizationâ”‚  â”‚  Fullscreen file tree +  â”‚
â”‚  HjÃ¤rnregioner, knowledge    â”‚  â”‚  multi-tab editor         â”‚
â”‚  panel, packets              â”‚  â”‚  SÃ¶k, filter, multi-root  â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Alla fÃ¶nster delar samma C#-process och bridge â€” main-fÃ¶nstret Ã¤r fortfarande primÃ¤rt och hanterar Ollama, command routing, pending approval. Brain/Explorer Ã¤r sekundÃ¤ra visualiseringar.

## SÃ¤kerhetslinje (Ã¤ndrad frÃ¥n strikt avstÃ¤ngd â†’ always-on med offline-fallback)

Idag sÃ¤ger MASTER_PLAN och AGENTS.md att 3D/NeuroLinked **inte ska startas**. Den nya regeln (per anvÃ¤ndarens beslut 2026-05-09):

- **Jarvis Ã¤r always-online** â€” brain (NeuroLinked Python-server) startas automatiskt med main-appen sÃ¥ Jarvis alltid har tillgÃ¥ng till brain.
- **Offline graceful degradation** â€” nÃ¤r internet saknas: brain fortsÃ¤tter funka lokalt (Ollama, lokala verktyg), web-tools svarar "Internet saknas just nu, hoppar Ã¶ver".
- **Brain-fÃ¶nstret kan Ã¶ppnas/stÃ¤ngas separat** â€” Python-servern Ã¤r dock alltid igÃ¥ng i bakgrunden sÃ¥ data Ã¤r redo direkt.
- **F:\New project Ã¤r fortfarande read-only-referens** under hela porten â€” ingen kod Ã¤ndras dÃ¤r, bara lÃ¤ses och kopieras till F:\Jarvis-clean.
- **PendingApproval gÃ¤ller fortfarande fÃ¶r all filskrivning** â€” Ã¤ven om Brain/Explorer visar fler filer.
- **CommandRouter fÃ¥ngar alla kommandon fÃ¶re Ollama** â€” ofÃ¶rÃ¤ndrat.
- **Om Python eller Three.js saknas:** main fortsÃ¤tter fungera utan brain; Brain-fÃ¶nstret visar "Brain-lÃ¤ge krÃ¤ver Python/Three.js" istÃ¤llet fÃ¶r att hÃ¤nga.
- **Multi-window**: Main + Brain + Explorer som tre separata `Form`-instanser i samma C#-process. StÃ¤nga ett sekundÃ¤rt fÃ¶nster pÃ¥verkar inte main.

## BÃ¤sta-av-bÃ¤gge â€” beslutslista

| Komponent | Gamla (`F:\New project`) | Clean (`F:\Jarvis-clean`) | **Vinnare** |
|-----------|--------------------------|---------------------------|-------------|
| 3D-dashboard | âœ“ Three.js, brain3d.js | âœ— avstÃ¤ngt | **Gamla** (porteras) |
| Knowledge panel | âœ“ Graphify+Obsidian | âœ— saknas | **Gamla** (porteras) |
| 17-tool agent | âœ“ OllamaAgentHarness | âœ— saknas | **Gamla** (porteras) |
| Multi-model switch | âœ“ ModelCatalog (5 profiler) | âœ— saknas (en hÃ¥rdkodad modell) | **Gamla** (porteras) |
| CommandRouter | âœ— saknas | âœ“ V1 stabilt | **Clean** (bevaras) |
| PendingApproval | partiellt | âœ“ V1 (file write/delete/undo/terminal) | **Clean** (bevaras) |
| Project Explorer | i dashboard, mindre | âœ“ aktiv-fil-highlight, tree-polish | **Clean** (bevaras) |
| File panel edit | enklare | âœ“ Edit-lÃ¤ge + Spara med godkÃ¤nnande | **Clean** (bevaras) |
| Terminal-panel | saknades | âœ“ V1 med pending approval | **Clean** (bevaras) |
| Smart memory | partiell, ej central | âœ“ data\memory.md med kommandon | **Clean** (bevaras) |
| Ã–versikt-panel | "Visual Lab" rÃ¶rigt | âœ“ Jarvis Ã–versikt rent | **Clean** (bevaras) |
| Slash-commands | fÃ¤rre | âœ“ CommandRouterV1 | **Clean** (bevaras) |
| Tester | mÃ¥nga, men splittrade | âœ“ 19 node + C# router | **Clean** (utÃ¶kas) |
| Build/release | build.ps1 (har bugg) | dotnet publish | **Clean** (enklare) |
| Python-verktyg | weather/news/TTS/STT/web | saknas | **Gamla** (porteras opt-in) |

## Faser (i ordning, varje mÃ¥ste verifieras fÃ¶re nÃ¤sta)

### Fas 0 â€” MD-uppdatering & beslutsbekrÃ¤ftelse (~30 min)

Uppdatera dessa filer sÃ¥ den nya riktningen Ã¤r dokumenterad:

- [ ] `MASTER_PLAN.md` â€” 3D/NeuroLinked Ã¤ndras frÃ¥n "avstÃ¤ngt" till "opt-in via knapp"
- [ ] `BUILD_PLAN.md` â€” Fas 7 (NeuroLinked) flyttas tidigare, ny Fas: Multi-window
- [ ] `AGENTS.md` â€” 3D-regeln Ã¤ndras frÃ¥n "starta inte" till "starta bara pÃ¥ explicit anvÃ¤ndarbegÃ¤ran"
- [ ] `CURRENT_STATE.md` â€” lÃ¤gg till sektion "2026-05-09 Unifieringsplan startad"
- [ ] `TODO_NEXT.md` â€” lÃ¤gg in Fas 1â€“8-checklista frÃ¥n denna plan
- [ ] Skapa `docs/MULTI_WINDOW_DESIGN.md` med fÃ¶nster-arkitekturen
- [ ] `docs/PROJECT_INDEX.md` â€” uppdatera med nya filer

**Verifiering**: `dotnet build` passerar (docs-only Ã¤ndringar).

### Fas 1 â€” SlutfÃ¶r Jarvis-clean baseline (~3-4h)

StÃ¤ng kvarstÃ¥ende TODO_NEXT-poster sÃ¥ clean fungerar fullt ut innan vi slÃ¥r pÃ¥ nya saker.

- [ ] **1.1** LÃ¤gg till `senaste build-status` + `senaste minnesfÃ¶rÃ¤ndring` i Jarvis Ã–versikt
- [ ] **1.2** Bygg ut named checkpoints/history bortom one-step undo (`/checkpoint skapa <namn>`, `/checkpoint lista`, `/checkpoint Ã¥terstÃ¤ll <namn>`)
- [ ] **1.3** InternetProbe i C# (cachad TCP-koll mot 1.1.1.1:443, 800ms timeout, 30s cache) â€” krav frÃ¥n OFFLINE_PLAN
- [ ] **1.4** Initial test harness Fas A MVP: unit tests fÃ¶r CommandRouterV1 + CommandValidatorV1, integration test fÃ¶r PendingApprovalV1 mot mockad disk

**Verifiering**:
- Alla nya tester grÃ¶na
- `dotnet run --project tests\CommandRouterV1.Tests` grÃ¶n
- `node tests\*.test.js` (alla 19+nya) grÃ¶na
- `dotnet build` 0 errors
- Manuell UI-verifiering av nya kommandon

**Publish/restart**: ja efter grÃ¶nt

### Fas 2 â€” Vendor 3D-assets till Jarvis-clean (~30 min)

Bara kopiera in statiska beroenden, ingen logik Ã¤nnu.

- [ ] **2.1** Skapa `F:\Jarvis-clean\dashboard\vendor\` och kopiera:
  - `three.module.js`
  - `controls\OrbitControls.js` (+ andra som anvÃ¤nds)
  - `postprocessing\*` (om anvÃ¤nds)
  - `shaders\*` (om anvÃ¤nds)
- [ ] **2.2** Skapa `F:\Jarvis-clean\graphify-out\` och kopiera `graph.json` frÃ¥n gamla
- [ ] **2.3** Test: `node --check dashboard\vendor\three.module.js` (eller equivalent syntax-check)

**Verifiering**: filer pÃ¥ plats, syntax-check OK, inga runtime-Ã¤ndringar Ã¤n.

### Fas 3 â€” Brain-fÃ¶nster (3D NeuroLinked) (~4-6h)

Skapa det nya separata 3D-fÃ¶nstret. Statisk version fÃ¶rst, ingen Python-server.

- [ ] **3.1** Skapa `F:\Jarvis-clean\dashboard\brain.html` med:
  - Importmap till lokal `/vendor/three.module.js`
  - Canvas + glass-paneler (porterat frÃ¥n gamla `index.html`)
  - Inline CSS (eller separat `dashboard\css\brain.css`)
- [ ] **3.2** Portera `brain3d.js` frÃ¥n gamla:
  - Ta bort kopplingar till `/api/knowledge/map` (Python-server) â€” anvÃ¤nd `graph.json` direkt via fetch i Fas 5
  - FÃ¶r Fas 3: rendera bara hjÃ¤rnregioner + dummy-data, inga knowledge-noder Ã¤nnu
- [ ] **3.3** Skapa `F:\Jarvis-clean\app\BrainWindow.cs`:
  - Egen `Form` med `WebView2`
  - Laddar `dashboard\brain.html` via `NavigateToString` (samma pattern som main)
  - `OnClosing` â†’ bara dÃ¶lj, inte stÃ¤ng processen
- [ ] **3.4** LÃ¤gg till knapp `Brain` i main-window och slash-kommando `/brain`
- [ ] **3.5** Skapa test `tests\brain-window.test.js` (laddningskontrakt) + UI-flagga via WebMessage sÃ¥ main vet att Brain Ã¤r Ã¶ppet
- [ ] **3.6** Felfallback: om `vendor\three.module.js` saknas â†’ visa "Brain-lÃ¤ge krÃ¤ver Three.js. KÃ¶r fas 2."

**Verifiering**:
- `node --check brain3d.js`
- Brain-fÃ¶nster Ã¶ppnas utan att frysa main
- StÃ¤nga Brain â†’ main fortsÃ¤tter fungera
- Build 0 errors, alla tester grÃ¶na

**Publish/restart**: ja

### Fas 4 â€” File Explorer-fÃ¶nster (~3-4h)

SekundÃ¤r huvudskÃ¤rm fÃ¶r fullscreen filhantering.

- [ ] **4.1** Skapa `F:\Jarvis-clean\dashboard\explorer.html`:
  - TvÃ¥-panel: tree (vÃ¤nster, expanderbar) + multi-tab editor (hÃ¶ger)
  - SÃ¶k-fÃ¤lt, filter, multi-root-stÃ¶d (F:\Jarvis-clean default; F:\New project read-only)
  - Ã…teranvÃ¤nd Project Explorer-koden frÃ¥n huvuddashboarden
- [ ] **4.2** Skapa `F:\Jarvis-clean\app\FileExplorerWindow.cs`:
  - Egen `Form` med `WebView2`
  - Delar samma WebMessage-protokoll som main fÃ¶r fil-lÃ¤sning/skrivning
  - All skrivning gÃ¥r genom samma `PendingApprovalV1`
- [ ] **4.3** LÃ¤gg till knapp `File Explorer` + slash-kommando `/explorer`
- [ ] **4.4** Multi-root: `F:\Jarvis-clean` (read-write via approval), `F:\New project` (read-only)
- [ ] **4.5** Skapa `tests\file-explorer-window.test.js`

**Verifiering**:
- File Explorer Ã¶ppnas separat
- Pending approval triggas fÃ¶r skrivning Ã¤ven frÃ¥n Explorer-fÃ¶nstret
- Read-only F:\New project blockerar skrivfÃ¶rsÃ¶k
- Build 0 errors

**Publish/restart**: ja

### Fas 5 â€” Python NeuroLinked-server (always-on) (~6-8h)

Nu kopplar vi in den riktiga Python-servern sÃ¥ brain alltid Ã¤r tillgÃ¤nglig.

- [ ] **5.1** Kopiera `F:\New project\neurolinked\` â†’ `F:\Jarvis-clean\neurolinked\` (inte F:\New project)
- [ ] **5.2** Kopiera `F:\New project\python\` â†’ `F:\Jarvis-clean\python\`
- [ ] **5.3** Skapa `F:\Jarvis-clean\app\Bridges\NeuroLinkedBridge.cs`:
  - Probar Python (samma logik som gamla, sÃ¤ker discovery: JARVIS_PYTHON env, py -3, lokala installs, PATH)
  - `StartAsync()` â†’ kÃ¶r `neurolinked\run.py` som child-process vid app-start
  - `StopAsync()` â†’ killar child vid app-shutdown
  - `IsAlive()` â†’ HTTP-GET mot localhost:8000/api/state med 800ms timeout
  - **Auto-start** vid main app OnLoad (efter dashboard ready)
- [ ] **5.4** BrainWindow.cs uppdateras:
  - Brain-fÃ¶nstret kan Ã¶ppnas nÃ¤r som helst â€” Python Ã¤r redan redo
  - Navigera WebView till `http://127.0.0.1:8000`
  - Watchdog: om server inte ready inom 8s â†’ visa fallback statisk brain.html (Fas 3)
- [ ] **5.5** **Offline-graceful**: NeuroLinkedBridge respekterar InternetProbe (Fas 1.3)
  - Lokal brain (Ollama + memory) fortsÃ¤tter funka utan internet
  - Web-tools (vÃ¤der, news, sÃ¶kning) svarar "Internet saknas, hoppar Ã¶ver"
- [ ] **5.6** Sluten port-policy: NeuroLinked binder bara till 127.0.0.1, ingen extern access
- [ ] **5.7** Status-indikator i main: chip som visar "Brain: redo" / "Brain: startar..." / "Brain: ej tillgÃ¤nglig"
- [ ] **5.8** Test: `tests\neurolinked-bridge.test.js` (start/stop/timeout/fallback/offline)

**Verifiering**:
- Main startar â†’ Python auto-startar i bakgrunden, status-chip "Brain: redo" inom 10s
- Brain-fÃ¶nster Ã¶ppnas â†’ dashboard visas direkt (Python redan redo)
- StÃ¤ng main â†’ Python stoppas inom 5s, inga rest-processer
- StÃ¤ng av WiFi â†’ Ollama + memory fortsÃ¤tter fungera, web-tools faller tillbaka
- Om Python saknas â†’ main fortsÃ¤tter, Brain-fÃ¶nster visar "krÃ¤ver Python"
- `Get-Process python` efter app-stop visar inga rest

**Publish/restart**: ja, med uppdatering i CURRENT_STATE.md om always-on policy

### Fas 6 â€” OllamaAgentHarness (17 verktyg) (~4-5h)

Bring in tool-calling-agenten frÃ¥n gamla sÃ¥ Ollama kan lÃ¤sa/Ã¤ndra filer.

- [ ] **6.1** Kopiera `F:\New project\app\Agents\OllamaAgentHarness.cs` â†’ `F:\Jarvis-clean\app\Agents\OllamaAgentHarness.cs`
- [ ] **6.2** Anpassa till clean's safety-regler:
  - `write_file`, `replace_in_file`, `run_command` MÃ…STE gÃ¥ genom `PendingApprovalV1`
  - `read_file` fÃ¥r lÃ¤sa F:\Jarvis-clean och F:\New project (read-only)
  - INGEN fri F-disk-skrivning
- [ ] **6.3** `run_command` blockas helt utanfÃ¶r whitelistade kommandon (`dotnet build`, `dotnet test`, `dotnet publish`)
- [ ] **6.4** LÃ¤gg till slash-kommando `/agent <task>` som triggar agentlÃ¤ge
- [ ] **6.5** Test: `tests\ollama-agent-safety.test.js` â€” verifiera att alla 17 verktyg respekterar PendingApproval

**Verifiering**:
- Agent kan lÃ¤sa filer fritt
- Agent fÃ¶rsÃ¶ker skriva â†’ pending approval popup
- Agent fÃ¶rsÃ¶ker `rm -rf` â†’ blockas
- Build 0 errors

### Fas 7 â€” Multi-model (ModelCatalog) (~2h)

Multi-model switching frÃ¥n gamla.

- [ ] **7.1** Kopiera `F:\New project\app\Core\ModelCatalog.cs`
- [ ] **7.2** LÃ¤gg till `_activeModel` field i JarvisForm
- [ ] **7.3** Slash-commands: `/modell visa`, `/modell byt <namn>`, `/modell snabb`, `/modell kod`
- [ ] **7.4** Auto-upgrade fastâ†’coder vid agent-lÃ¤ge
- [ ] **7.5** Pull-script som verifierar att alla 5 modeller finns lokalt

**Verifiering**: byter modell mid-conversation â†’ kontext bevaras

### Fas 8 â€” Cleanup, dokumentation, slutverifiering (~2-3h)

- [ ] **8.1** Uppdatera alla MD-filer i F:\Jarvis-clean med slutgiltig arkitektur
- [ ] **8.2** Skriv `MIGRATION_FROM_NEW_PROJECT.md` med exakt vad som porterats
- [ ] **8.3** Skriv ny `RELEASE_STATUS.md` med v1.0 unified
- [ ] **8.4** Markera `F:\New project` som **arkiverad referens** (skapa `F:\New project\ARCHIVED.md`)
- [ ] **8.5** Full regressionstest: alla node-tester + alla C#-tester + manuell UI-rundtur
- [ ] **8.6** Final publish + restart, screenshot fÃ¶r dokumentation

**Verifiering**: hela suiten grÃ¶n, manuell rundtur OK.

## Tidsuppskattning totalt

| Fas | Tid | Kumulativt |
|-----|-----|-----------|
| 0 â€” MD | 30 min | 0:30 |
| 1 â€” Baseline | 3-4h | 4:30 |
| 2 â€” Vendor | 30 min | 5:00 |
| 3 â€” Brain-fÃ¶nster | 4-6h | 11:00 |
| 4 â€” Explorer-fÃ¶nster | 3-4h | 15:00 |
| 5 â€” Python server | 6-8h | 23:00 |
| 6 â€” OllamaAgent | 4-5h | 28:00 |
| 7 â€” Multi-model | 2h | 30:00 |
| 8 â€” Cleanup | 2-3h | 33:00 |

**Totalt: ~30-33 timmar effektivt arbete**, fÃ¶rdelade Ã¶ver flera sessioner.

## Risker och mitigeringar

| Risk | Sannolikhet | Mitigering |
|------|-------------|------------|
| Python-server hÃ¤nger main-appen | HÃ¶g | BrainWindow Ã¤r async, watchdog 8s, fallback statisk brain.html |
| 3D fryser pÃ¥ svaga datorer | Medel | safe-mode-flagga `?safe=1` som hoppar Ã¶ver WebGL |
| OllamaAgent kringgÃ¥r PendingApproval | HÃ¶g (kritisk om missas) | Whitelist + tester per verktyg, code review fÃ¶re merge |
| Build.ps1 third_party-bugg flyttas hit | Medel | Vi kÃ¶r `dotnet publish` direkt, ingen third_party-kopiering |
| Konflikter clean vs old i samma fil | Medel | Filerna kopieras med nya namn, jÃ¤mfÃ¶rs explicit |
| MD-filer hamnar ur synk | HÃ¶g | Fas 0 + 8 explicit MD-uppdatering |
| AnvÃ¤ndaren tappar kontext | Medel | Status uppdateras i CURRENT_STATE.md efter varje fas |

## Beslut som krÃ¤vs av anvÃ¤ndaren INNAN start

1. **BekrÃ¤fta multi-window-arkitekturen** (3 separata fÃ¶nster: Main, Brain, Explorer) â€” eller fÃ¶redrar du tabbar i ett fÃ¶nster?
2. **BekrÃ¤fta att F:\New project blir read-only-referens** efter portning â€” inga Ã¤ndringar dÃ¤r, vi bara lÃ¤ser/kopierar.
3. **BekrÃ¤fta sÃ¤kerhetsregeln**: Python NeuroLinked-server startar bara pÃ¥ explicit anvÃ¤ndarbegÃ¤ran, default OFF.
4. **BekrÃ¤fta tidsuppskattning** ~30h Ã¶ver flera sessioner â€” eller vill du ha snabbare/mindre version?
5. **BekrÃ¤fta att vi kÃ¶r Fas 0+1 fÃ¶rst** fÃ¶r att slutfÃ¶ra Jarvis-clean baseline innan vi rÃ¶r 3D.

## Verifieringskedja per fas

Varje fas slutar med samma checklista:

```
[ ] dotnet build â†’ 0 errors
[ ] dotnet run --project tests\CommandRouterV1.Tests â†’ grÃ¶n
[ ] node tests\*.test.js â†’ alla grÃ¶na
[ ] Manuell UI-rundtur (om relevant)
[ ] CURRENT_STATE.md uppdaterad
[ ] TODO_NEXT.md uppdaterad (markera klara, lÃ¤gg till nya)
[ ] Publish + restart om runtime-Ã¤ndring
[ ] Commit i git med tydligt budskap
```

## Ã…terhÃ¤mtning om nÃ¥got gÃ¥r fel

- Varje fas backas upp med `.checkpoints\<datum>\` (befintligt system)
- Build-fel â†’ kÃ¶r `dotnet clean` + `dotnet restore` + `dotnet build`
- Brain-fÃ¶nster fryser â†’ close, kÃ¶r `taskkill /F /IM Jarvis.exe` + omstart med `?safe=1`
- Python-server hÃ¤nger â†’ close Brain â†’ bridge dÃ¶dar process
- Vid total rÃ¶ra: `git reset --hard <senaste-grÃ¶na-commit>`

## Vad jag (Claude Code) Ã¤r sÃ¤ker pÃ¥ att jag kan utfÃ¶ra

âœ… Fas 0 (MD-uppdatering) â€” trivialt
âœ… Fas 1 (baseline) â€” bygger pÃ¥ existerande mÃ¶nster i clean
âœ… Fas 2 (vendor copy) â€” bara filkopiering
âœ… Fas 3 (Brain statisk) â€” ren JS+C# port av kÃ¤nda komponenter
âœ… Fas 4 (Explorer-fÃ¶nster) â€” bygger pÃ¥ Project Explorer som finns
âœ… Fas 6 (OllamaAgent) â€” koden finns, bara sÃ¤kra anpassningar
âœ… Fas 7 (ModelCatalog) â€” direkt port
âœ… Fas 8 (cleanup) â€” dokumentation
âš ï¸ Fas 5 (Python server) â€” krÃ¤ver att anvÃ¤ndaren har Python installerat och aktivt godkÃ¤nner. Tekniken Ã¤r vÃ¤lkÃ¤nd men beroenden Ã¤r komplexa.

**Slutsats**: Jag kan utfÃ¶ra hela planen. Fas 5 Ã¤r den enda med extern beroende-risk men har tydlig fallback (statisk brain.html frÃ¥n Fas 3).

