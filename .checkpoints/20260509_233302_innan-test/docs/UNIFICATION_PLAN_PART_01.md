# UNIFICATION_PLAN PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

# UNIFICATION_PLAN.md — Jarvis-clean + F:\New project → Ett projekt

Skapad: 2026-05-09
Ägare: Claude Code (planerare), Codex/Claude (utförare)
Status: **AWAITING APPROVAL**

## Mål (från användaren)

1. Aktivera 3D/NeuroLinked i Jarvis-clean (idag avstängt enligt MASTER_PLAN/AGENTS.md).
2. Lägg till **File Explorer som sekundärt huvudfönster** (egen window/sida).
3. Bygg klart Jarvis-clean först så den fungerar med dessa tillägg.
4. Integrera sedan med `F:\New project` → ETT projekt.
5. När konflikter: ta den bästa lösningen från respektive (gamla vs nya).
6. Uppdatera alla MD-filer så planen är dokumenterad.

## Slutmål — arkitektur

```
F:\Jarvis-clean\               ← unifierat projekt (slutligt hem)
├── app\                       ← C# WinForms, multi-window
│   ├── Program.cs             ← JarvisForm (huvud, 3-panel: Explorer | Editor | Chat)
│   ├── BrainWindow.cs         ← NY: separat 3D-fönster (WebView2)
│   ├── FileExplorerWindow.cs  ← NY: separat fullscreen file explorer
│   ├── CommandRouterV1.cs     ← bevaras
│   ├── CommandValidatorV1.cs  ← bevaras
│   ├── ToolRegistryV1.cs      ← bevaras
│   ├── PendingApprovalV1.cs   ← bevaras
│   ├── Agents\                ← NY: OllamaAgentHarness (från gamla, 17 verktyg)
│   ├── Core\
│   │   └── ModelCatalog.cs    ← NY: 5 modellprofiler (från gamla)
│   └── Bridges\
│       └── NeuroLinkedBridge.cs ← NY: opt-in start av Python-server
├── dashboard\
│   ├── index.html             ← huvud, 3-panel (befintlig)
│   ├── brain.html             ← NY: 3D-visualisering
│   ├── explorer.html          ← NY: file explorer fullscreen
│   ├── css\style.css          ← gemensam (porteras från gamla)
│   ├── js\
│   │   ├── brain3d.js         ← porterad från gamla (1949 rader)
│   │   ├── knowledge_panel.js ← porterad
│   │   ├── jarvis_bridge.js   ← porterad
│   │   └── ...
│   └── vendor\                ← NY: Three.js + addons (offline)
├── neurolinked\               ← NY: Python brain-service (opt-in)
│   ├── server.py              ← FastAPI :8000
│   ├── run.py
│   └── ...                    ← porterad från gamla
├── python\                    ← NY: weather/news/TTS/STT/web (porterad)
├── graphify-out\              ← NY: graph.json (porterad)
├── tests\                     ← befintliga + nya
├── docs\                      ← MD-filer uppdaterade
└── data\, vault\, config\     ← lokal state, oförändrad
```

**Tre fönster:**

```
┌─ MAIN (Jarvis-fönstret) ──────────────────────────────────┐
│  Explorer | Editor | Chat                                  │
│  Knappar: [Brain] [File Explorer] [Översikt]              │
└────────────────────────────────────────────────────────────┘
        ↓ (klick på knapp eller /kommando)
┌─ BRAIN (eget fönster) ─────┐  ┌─ FILE EXPLORER (eget) ────┐
│  3D NeuroLinked-visualization│  │  Fullscreen file tree +  │
│  Hjärnregioner, knowledge    │  │  multi-tab editor         │
│  panel, packets              │  │  Sök, filter, multi-root  │
└──────────────────────────────┘  └───────────────────────────┘
```

Alla fönster delar samma C#-process och bridge — main-fönstret är fortfarande primärt och hanterar Ollama, command routing, pending approval. Brain/Explorer är sekundära visualiseringar.

## Säkerhetslinje (ändrad från strikt avstängd → always-on med offline-fallback)

Idag säger MASTER_PLAN och AGENTS.md att 3D/NeuroLinked **inte ska startas**. Den nya regeln (per användarens beslut 2026-05-09):

- **Jarvis är always-online** — brain (NeuroLinked Python-server) startas automatiskt med main-appen så Jarvis alltid har tillgång till brain.
- **Offline graceful degradation** — när internet saknas: brain fortsätter funka lokalt (Ollama, lokala verktyg), web-tools svarar "Internet saknas just nu, hoppar över".
- **Brain-fönstret kan öppnas/stängas separat** — Python-servern är dock alltid igång i bakgrunden så data är redo direkt.
- **F:\New project är fortfarande read-only-referens** under hela porten — ingen kod ändras där, bara läses och kopieras till F:\Jarvis-clean.
- **PendingApproval gäller fortfarande för all filskrivning** — även om Brain/Explorer visar fler filer.
- **CommandRouter fångar alla kommandon före Ollama** — oförändrat.
- **Om Python eller Three.js saknas:** main fortsätter fungera utan brain; Brain-fönstret visar "Brain-läge kräver Python/Three.js" istället för att hänga.
- **Multi-window**: Main + Brain + Explorer som tre separata `Form`-instanser i samma C#-process. Stänga ett sekundärt fönster påverkar inte main.

## Bästa-av-bägge — beslutslista

| Komponent | Gamla (`F:\New project`) | Clean (`F:\Jarvis-clean`) | **Vinnare** |
|-----------|--------------------------|---------------------------|-------------|
| 3D-dashboard | ✓ Three.js, brain3d.js | ✗ avstängt | **Gamla** (porteras) |
| Knowledge panel | ✓ Graphify+Obsidian | ✗ saknas | **Gamla** (porteras) |
| 17-tool agent | ✓ OllamaAgentHarness | ✗ saknas | **Gamla** (porteras) |
| Multi-model switch | ✓ ModelCatalog (5 profiler) | ✗ saknas (en hårdkodad modell) | **Gamla** (porteras) |
| CommandRouter | ✗ saknas | ✓ V1 stabilt | **Clean** (bevaras) |
| PendingApproval | partiellt | ✓ V1 (file write/delete/undo/terminal) | **Clean** (bevaras) |
| Project Explorer | i dashboard, mindre | ✓ aktiv-fil-highlight, tree-polish | **Clean** (bevaras) |
| File panel edit | enklare | ✓ Edit-läge + Spara med godkännande | **Clean** (bevaras) |
| Terminal-panel | saknades | ✓ V1 med pending approval | **Clean** (bevaras) |
| Smart memory | partiell, ej central | ✓ data\memory.md med kommandon | **Clean** (bevaras) |
| Översikt-panel | "Visual Lab" rörigt | ✓ Jarvis Översikt rent | **Clean** (bevaras) |
| Slash-commands | färre | ✓ CommandRouterV1 | **Clean** (bevaras) |
| Tester | många, men splittrade | ✓ 19 node + C# router | **Clean** (utökas) |
| Build/release | build.ps1 (har bugg) | dotnet publish | **Clean** (enklare) |
| Python-verktyg | weather/news/TTS/STT/web | saknas | **Gamla** (porteras opt-in) |

## Faser (i ordning, varje måste verifieras före nästa)

### Fas 0 — MD-uppdatering & beslutsbekräftelse (~30 min)

Uppdatera dessa filer så den nya riktningen är dokumenterad:

- [ ] `MASTER_PLAN.md` — 3D/NeuroLinked ändras från "avstängt" till "opt-in via knapp"
- [ ] `BUILD_PLAN.md` — Fas 7 (NeuroLinked) flyttas tidigare, ny Fas: Multi-window
- [ ] `AGENTS.md` — 3D-regeln ändras från "starta inte" till "starta bara på explicit användarbegäran"
- [ ] `CURRENT_STATE.md` — lägg till sektion "2026-05-09 Unifieringsplan startad"
- [ ] `TODO_NEXT.md` — lägg in Fas 1–8-checklista från denna plan
- [ ] Skapa `docs/MULTI_WINDOW_DESIGN.md` med fönster-arkitekturen
- [ ] `docs/PROJECT_INDEX.md` — uppdatera med nya filer

**Verifiering**: `dotnet build` passerar (docs-only ändringar).

### Fas 1 — Slutför Jarvis-clean baseline (~3-4h)

Stäng kvarstående TODO_NEXT-poster så clean fungerar fullt ut innan vi slår på nya saker.

- [ ] **1.1** Lägg till `senaste build-status` + `senaste minnesförändring` i Jarvis Översikt
- [ ] **1.2** Bygg ut named checkpoints/history bortom one-step undo (`/checkpoint skapa <namn>`, `/checkpoint lista`, `/checkpoint återställ <namn>`)
- [ ] **1.3** InternetProbe i C# (cachad TCP-koll mot 1.1.1.1:443, 800ms timeout, 30s cache) — krav från OFFLINE_PLAN
- [ ] **1.4** Initial test harness Fas A MVP: unit tests för CommandRouterV1 + CommandValidatorV1, integration test för PendingApprovalV1 mot mockad disk

**Verifiering**:
- Alla nya tester gröna
- `dotnet run --project tests\CommandRouterV1.Tests` grön
- `node tests\*.test.js` (alla 19+nya) gröna
- `dotnet build` 0 errors
- Manuell UI-verifiering av nya kommandon

**Publish/restart**: ja efter grönt

### Fas 2 — Vendor 3D-assets till Jarvis-clean (~30 min)

Bara kopiera in statiska beroenden, ingen logik ännu.

- [ ] **2.1** Skapa `F:\Jarvis-clean\dashboard\vendor\` och kopiera:
  - `three.module.js`
  - `controls\OrbitControls.js` (+ andra som används)
  - `postprocessing\*` (om används)
  - `shaders\*` (om används)
- [ ] **2.2** Skapa `F:\Jarvis-clean\graphify-out\` och kopiera `graph.json` från gamla
- [ ] **2.3** Test: `node --check dashboard\vendor\three.module.js` (eller equivalent syntax-check)

**Verifiering**: filer på plats, syntax-check OK, inga runtime-ändringar än.

### Fas 3 — Brain-fönster (3D NeuroLinked) (~4-6h)

Skapa det nya separata 3D-fönstret. Statisk version först, ingen Python-server.

- [ ] **3.1** Skapa `F:\Jarvis-clean\dashboard\brain.html` med:
  - Importmap till lokal `/vendor/three.module.js`
  - Canvas + glass-paneler (porterat från gamla `index.html`)
  - Inline CSS (eller separat `dashboard\css\brain.css`)
- [ ] **3.2** Portera `brain3d.js` från gamla:
  - Ta bort kopplingar till `/api/knowledge/map` (Python-server) — använd `graph.json` direkt via fetch i Fas 5
  - För Fas 3: rendera bara hjärnregioner + dummy-data, inga knowledge-noder ännu
- [ ] **3.3** Skapa `F:\Jarvis-clean\app\BrainWindow.cs`:
  - Egen `Form` med `WebView2`
  - Laddar `dashboard\brain.html` via `NavigateToString` (samma pattern som main)
  - `OnClosing` → bara dölj, inte stäng processen
- [ ] **3.4** Lägg till knapp `Brain` i main-window och slash-kommando `/brain`
- [ ] **3.5** Skapa test `tests\brain-window.test.js` (laddningskontrakt) + UI-flagga via WebMessage så main vet att Brain är öppet
- [ ] **3.6** Felfallback: om `vendor\three.module.js` saknas → visa "Brain-läge kräver Three.js. Kör fas 2."

**Verifiering**:
- `node --check brain3d.js`
- Brain-fönster öppnas utan att frysa main
- Stänga Brain → main fortsätter fungera
- Build 0 errors, alla tester gröna

**Publish/restart**: ja

### Fas 4 — File Explorer-fönster (~3-4h)

Sekundär huvudskärm för fullscreen filhantering.

- [ ] **4.1** Skapa `F:\Jarvis-clean\dashboard\explorer.html`:
  - Två-panel: tree (vänster, expanderbar) + multi-tab editor (höger)
  - Sök-fält, filter, multi-root-stöd (F:\Jarvis-clean default; F:\New project read-only)
  - Återanvänd Project Explorer-koden från huvuddashboarden
- [ ] **4.2** Skapa `F:\Jarvis-clean\app\FileExplorerWindow.cs`:
  - Egen `Form` med `WebView2`
  - Delar samma WebMessage-protokoll som main för fil-läsning/skrivning
  - All skrivning går genom samma `PendingApprovalV1`
- [ ] **4.3** Lägg till knapp `File Explorer` + slash-kommando `/explorer`
- [ ] **4.4** Multi-root: `F:\Jarvis-clean` (read-write via approval), `F:\New project` (read-only)
- [ ] **4.5** Skapa `tests\file-explorer-window.test.js`

**Verifiering**:
- File Explorer öppnas separat
- Pending approval triggas för skrivning även från Explorer-fönstret
- Read-only F:\New project blockerar skrivförsök
- Build 0 errors

**Publish/restart**: ja

### Fas 5 — Python NeuroLinked-server (always-on) (~6-8h)

Nu kopplar vi in den riktiga Python-servern så brain alltid är tillgänglig.
