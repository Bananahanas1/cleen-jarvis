# CURRENT_STATE PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

# CURRENT_STATE.md — Jarvis

Senast uppdaterad: 2026-05-10

## Status

Jarvis har nu en stabil första grundversion i:

F:\Jarvis-clean

## 2026-05-10 — D1/D3/D4 UI-TARS desktop-control safe pass implementerat

**UI-TARS/Desktop-control** — samma action-familj som UI-TARS, men genom Jarvis säkerhetsloop:
- Ny bridge: `app\Bridges\UiTarsBridge.cs`
- Nya desktop-filer: `ScreenCapture.cs`, `DesktopActionRequestV1.cs`, `DesktopActionGate.cs`, `DesktopActionExecutor.cs`
- Slash: `/desktop status`, `/desktop på`, `/desktop av`, `/desktop tars start`, `/desktop tars stop`, `/skärm`
- Manual actions: `/desktop klick`, `/desktop dubbelklick`, `/desktop högerklick`, `/desktop drag`, `/desktop skriv`, `/desktop hotkey`, `/desktop scroll`
- Vision action: `/desktop fråga <instruktion>` tar screenshot, frågar UI-TARS-kompatibel VLM om en action och skapar pending preview.
- Ingen desktop-action körs direkt. Varje action blir `PendingApprovalV1.DesktopAction`.
- Ctrl+Shift+Alt+J är hard-kill för desktop-control.

**Begränsning**:
- Vision kräver egen UI-TARS-kompatibel API-konfig via `JARVIS_UITARS_BASE_URL`, `JARVIS_UITARS_API_KEY`, `JARVIS_UITARS_MODEL` eller `config\uitars.json`.
- Utan API-konfig fungerar manual action-flow, screenshot och UI-TARS subprocess start/stop, men `/desktop fråga` kan inte få en modellprediction.

**Verifiering**:
- `node F:\Jarvis-clean\tests\desktop-control.test.js` → passed
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` → passed
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` → 0 errors, känd `MSB3277` warning kvar

## 2026-05-10 — B4 BuilderMode första pass implementerat

**B4 BuilderMode** — trygg idé → frågor → plan:
- Ny fil: `app\Brain\BuilderMode.cs`
- Slash: `/bygg <idé>`, `/bygg svar <svar>`, `/bygg plan`, `/bygg status`, `/bygg avbryt`
- Jarvis kan starta en builder-session, ställa 3-5 klargörande frågor och spara användarens svar i runtime.
- `/bygg plan` genererar en plan och skapar pending `FileCreate` för `vault/builds/<slug>/PLAN.md`.
- Ingen builder-plan skrivs direkt. Användaren måste godkänna popupen först.
- Första passet skapar inte app-filer från planen ännu. Nästa fas ska skapa filer en i taget via `PendingApprovalV1`.

**Verifiering**:
- `node F:\Jarvis-clean\tests\builder-mode.test.js` → passed
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` → passed
- `node F:\Jarvis-clean\tests\dashboard-routing.test.js` → passed
- `node F:\Jarvis-clean\tests\help-text.test.js` → passed
- `node F:\Jarvis-clean\tests\file-write-safety.test.js` → passed
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` → 0 errors, känd `MSB3277` warning kvar

## 2026-05-10 — B3 NaturalEditTool första pass implementerat

**B3 NaturalEditTool** — naturligt språk till säker kod-edit:
- Ny fil: `app\Brain\NaturalEditTool.cs`
- Slash: `/edit <fil> = <beskrivning>`
- Naturlig fras: `gå in i docs/test.md och gör texten tydligare`
- Jarvis läser målfilen lokalt, ber `qwen2.5-coder:7b` generera komplett nytt filinnehåll och skapar `PendingApprovalV1.FileWrite`.
- Ingen fil skrivs direkt. Användaren måste godkänna popupen först.
- Natural edit körs före smart-open så `öppna/gå in i <fil> och ändra...` inte råkar bli vanlig filöppning.
- Begränsning första passet: max 24000 tecken per fil, säkra textfiltyper bara.

**Verifiering**:
- `node F:\Jarvis-clean\tests\natural-edit-tool.test.js` → passed
- `dotnet run --project F:\Jarvis-clean\tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` → passed
- `node F:\Jarvis-clean\tests\dashboard-routing.test.js` → passed
- `node F:\Jarvis-clean\tests\help-text.test.js` → passed
- `node F:\Jarvis-clean\tests\file-write-safety.test.js` → passed
- `node F:\Jarvis-clean\tests\approval-popup-csharp.test.js` → passed
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` → 0 errors, känd `MSB3277` warning kvar

## 2026-05-10 (sent kväll/natt) — B1+B2+C1+D2 implementerade

Efter Codex-handoff fortsatte Claude Code på sista tokens med fyra fristående features:

**B1 ModelRouter** — Auto-routing av Ollama-modell baserat på query:
- Kod-task ("klass/funktion/fix/refaktorera...") → Coder (qwen2.5-coder:7b)
- Djupanalys ("varför/analysera/jämför...") → Reason (deepseek-r1:7b)
- Planering eller långa frågor (>200 tecken) → Smart (qwen3:8b)
- Djup dialog (>5 turns) → Smart
- Default → Fast (qwen3:1.7b)
- Manuellt val (annat än Fast) respekteras alltid
- Badge i chat-svar visar vilken modell `[fast]/[smart]/[code]/[reason]`

**B2 ConversationHistory** — Multi-turn context:
- Sliding window: max 20 turns / 8000 tecken
- Hookad i AskOllamaAsync: history skickas i varje request
- Slash: `/historik` (visa), `/glöm samtal` (rensa)

**C1 WebSearcher** — Web-sök via Google + Opera (användarens val 2026-05-10):
- `/sök <q>` → bygger Google-URL + öppnar i Opera (ingen scraping = ingen risk för CAPTCHA)
- `/läs <url>` → fetch + strip HTML för Jarvis att läsa/sammanfatta
- Offline-graceful via InternetProbe

**D2 SafeAppLauncher** — Säker program-launcher:
- Hård whitelist: notepad, calc, explorer, chrome, edge, firefox, vscode, spotify, mspaint, opera
- `TryOpenUrlInOpera(url)` används av WebSearcher
- Audit-logg till `data/desktop_actions.log`
- Inga argument tillåts (förhindrar argument-injection)
- Slash: `/öppna program <namn>`, `/lista program`

**Verifiering**:
- `dotnet build` → 0 errors
- 28 node-tester gröna (1 ny: b1-b2-c1-d2.test.js)
- Autocomplete + hjälptext uppdaterad

**Återstående för Codex**: D1, D3 och D4. B4 första pass är nu implementerat, men framtida builder-filskapande återstår.

## 2026-05-10 (kväll) — Brain-refaktor + Vault-AI-kontext klar

Stora refaktor efter användarfeedback:

**ETT program, inga lösa fönster**
- `BrainWindow.cs` och `FileExplorerWindow.cs` borttagna helt
- Brain är nu en **inbäddad vy** i mittpanelen (knapp `Brain`)
- File Explorer-vy borttagen — Project Explorer (vänster) är "the" explorer
- Brain-mode CSS döljer Project Explorer + edit/save så bara 3D + chat syns

**Brain 3D-graf — sci-fi NeuroLinked-stil**
- Helt svart bakgrund (rymden), 800-stjärnor backdrop
- UnrealBloomPass post-processing → noder glödar
- Pulse via SCALE-animation per nod (±18%, slumpmässig fas) — syns även utzoomat
- `controls.zoomToCursor = true` — zoom följer mus
- Fog borttagen så grafen syns klart utzoomat
- 4 sci-fi glas-paneler: STATS, FILTER (8 checkbox), SÖK, INSPECTOR
- Implicita folder-edges → orphans klumpas per mapp
- "Bygg om"-knapp rensar cache

**FileGraphBuilder bugfix + vault-skanning**
- `__init__.py` duplicate-key crash fixad (ToLookup)
- Vault pre-check (skip MD utan `[[` eller `source_file:`) → 29s → 1.4s
- Cache till `.checkpoints/.brain-graph-cache.json` (5 min TTL)
- 168 projekt-filer + ~10 vault-noter, 90+ edges
- Frontend timeout 8s → 60s

**Vault som AI-kontext (BR2 + BR6)**
- `app/Brain/VaultSearcher.cs` — ord-frekvens-scoring, titel-boost, svenska stoppord
- Auto-läsning **ON default**: `AskOllamaAsync` injicerar topp-5 vault-noter (max 4 KB)
- Cache med invalidation om någon `.md` ändrats
- Slash: `/vault status`, `/vault sök`, `/vault skapa`, `/vault på`, `/vault av`
- Översikt-cell "Vault (AI-kontext)" med live-status

**Vault-struktur enligt Obsidian-konvention**
- `vault/Index.md` med [[wikilinks]] till allt
- `vault/Project/` (UNIFICATION_PLAN, MULTI_WINDOW_DESIGN, MIGRATION, BRAIN_3D_SUPERPLAN, CURRENT_STATE)
- `vault/Memory/Azu_preferences.md`
- `vault/Decisions/DECISIONS_LOG.md`
- `vault/Issues/Brain_visual_polish.md`, `Vault_AI_context.md`
- Vault-scope: bara `F:\Jarvis-clean\vault\` (gamla obsidian-vault arkiverad)

**Autocomplete uppdaterad**
- `/` listar nu alla 30+ slash-kommandon (TAB cyklar)
- Nya: `/brain`, `/agent`, `/modell` (+5 shortcuts), `/vault` (+5 shortcuts), `/checkpoint` (+3 shortcuts)
- Naturligt-språk-grupper för brain/agent/vault tillagda
- Substring-matchning för fuzzy

**Verifiering 2026-05-10 (kväll)**
- `dotnet build` → 0 errors
- 27 node-tester gröna (1 ny: vault-searcher.test.js)
- 63 C#-router-tester gröna (5 nya för vault)

---

## 2026-05-10 — Unifieringsplan klar (Fas 0–8 implementerad)

Hela `docs\UNIFICATION_PLAN.md` är genomförd. Jarvis-clean är nu det unifierade projektet med multi-window, always-on brain, ModelCatalog och read-only agent. Komponentinventering i `docs\MIGRATION_FROM_NEW_PROJECT.md`.

**Vad fungerar nu**:
- Main-fönstret (3-panel: Project Explorer | Editor | Chat) — oförändrat och stabilt.
- **Brain-fönstret** (`/brain`) — separat 3D-fönster med 11 hjärnregioner, Three.js från lokal vendor (offline-säker).
- **File Explorer-fönstret** (`/explorer`) — multi-tab editor + multi-root tree (clean rw, newproject read-only).
- **Always-on Python brain** — auto-start vid app-load, status-chip i Översikt, offline-graceful.
- **Read-only agent** (`/agent <task>`) — 5 säkra läsverktyg, skrivverktyg explicit blockerade tills nästa fas integrerar dem med PendingApproval.
- **ModelCatalog** (`/modell`, `/modell byt <name|role>`) — 5 profiler, persistens till config\model.txt, auto-upgrade fast→coder för agent.
- **Översikt** har nya celler: Senaste bygge, Senaste minnesförändring, Brain (NeuroLinked).
- **Namngivna checkpoints** (`/checkpoint skapa <namn>`, lista, återställ).
- **InternetProbe** med 30s cache + offline-graceful helpers.

**Verifiering 2026-05-10**:
- `dotnet build` → 0 errors (1 känd MSB3277 warning)
- 27 node-tester gröna
- 58 C#-tester gröna

## 2026-05-09 — Unifieringsplan godkänd

Användaren har godkänt en omfattande plan att slå ihop `F:\Jarvis-clean` med `F:\New project` till ETT projekt på `F:\Jarvis-clean`.

Nyckelbeslut:
- **Multi-window**: 3 separata fönster — Main (3-panel), Brain (3D NeuroLinked), File Explorer (sekundär huvudskärm).
- **Always-on brain**: NeuroLinked Python-server auto-startas med main-appen. Offline-graceful — Ollama + lokala verktyg fungerar utan internet.
- **F:\New project blir read-only-referens** under porten — vi kopierar därifrån, skriver aldrig dit.
- **Bästa-av-bägge**: Behåll clean's CommandRouterV1, PendingApprovalV1, säkra defaults, tester. Portera in 3D-dashboard, OllamaAgentHarness (17 verktyg), ModelCatalog (5 modeller), Graphify, Obsidian från gamla.
- **Ordning**: Fas 0 (MD) → Fas 1 (slutför baseline) → Fas 2-3 (3D vendor + Brain) → Fas 4 (Explorer) → Fas 5 (Python server) → Fas 6-7 (OllamaAgent + ModelCatalog) → Fas 8 (cleanup).

Detaljerad plan: `docs\UNIFICATION_PLAN.md`.

## 2026-05-05 — Long-term Jarvis vision documented

Den större Jarvis-riktningen är nu dokumenterad i `docs\JARVIS_LONG_TERM_VISION.md`.

Jarvis ska inte bara vara en chatbot. Jarvis ska bli en lokal developer- och datoragent med en säker kontroll-loop:

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

Prioriteten är:
