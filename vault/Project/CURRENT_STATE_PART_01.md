# CURRENT_STATE PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

---
type: project-doc
source_file: "CURRENT_STATE.md"
updated: 2026-05-10
tags: [project, mirrored]
---

# CURRENT_STATE.md — Jarvis

Senast uppdaterad: 2026-05-09

## Status

Jarvis har nu en stabil första grundversion i:

F:\Jarvis-clean

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

1. Bli expert på `F:\Jarvis-clean`.
2. Bli expert på användarens andra kodprojekt.
3. Bli bredare datorassistent.
4. Lägg till desktop/browser/screen control senare och extra säkert.

Säkerhetslinjen är oförändrad:
- ingen fri skrivåtkomst till hela F-disken
- `F:\New project` är read-only reference
- andra F-drive roots ska vara read-only som default
- filskrivning, append, delete, terminalkörning, externa tools och framtida UI-automation ska gå via safety checks, pending preview och approval
- Jarvis ska verifiera efter action och rapportera vad som ändrades

3D/Visual Lab är kvar som framtida visuellt lager. Routing, approval, developer workspace och verifiering kommer först.

Dokumentationspass verifierat:
- `dotnet build` kördes i `F:\Jarvis-clean\app`.
- Resultat: build succeeded, 0 errors.
- Känd varning kvar: WindowsBase/WebView2 version conflict warning.
- Ingen publish/restart gjordes eftersom detta bara var dokumentation.

## 2026-05-06 — Jarvis Översikt, minne och Obsidian-status

Visual Lab-idén har styrts om till en praktisk `Jarvis Översikt`-panel.

Nytt i denna riktning:
- mittpanelen har knappen `Översikt`
- `/översikt` och `översikt` öppnar översiktspanelen lokalt
- `/minne status` visar lokal minnesstatus utan Ollama
- `/obsidian status` visar säker Obsidian-status utan att skriva till någon vault
- översikten visar aktiv fil, pending approval, senaste terminalstatus, memory-state, Obsidian-state och Jarvis kontroll-loop
- verifierat med Node/C# routing/UI-safety tests och `dotnet build`
- publicerat och omstartat; observerad process: `Jarvis.exe` PID 66812

## 2026-05-06 — Dark scrollbar polish

Dashboarden har nu global mörk scrollbar-styling för scrollbara ytor:
- Project Explorer
- filpanel/editor
- terminaloutput
- Jarvis Översikt
- chat
- autocomplete
- approval preview
- diff/review

Detta är en ren UI-polish och ändrar inte command routing eller approval-regler.

## 2026-05-07 — Färgkodade autocomplete-förslag

Suggestion-rader får olika färg per del:
- kommandoprefix (`/fil skapa`, `skriv fil:`, `öppna `) → vit (`#f4fbff`)
- mappar (slutar med `/`) → gul (`#ffd966`)
- filer → grön (`#80ff96`)

Implementation:
- Ny CSS `.suggestion-command`, `.suggestion-folder`, `.suggestion-file`.
- Ny helper `colorizeSuggestionText(suggestion)` som splittar via regex som matchar alla kända kommandoprefix.
- `renderSuggestions` lindar varje del i `<span class="suggestion-...">`.
- Bevarar befintlig `.suggestion-row.active` highlight-bg.

Test: `tests\suggestion-colors.test.js` — markers + 7 colorize-cases (slash-fil, naturligt språk, /hjälp, mapp vs fil).

## 2026-05-07 — TAB folder-suggestions för create + SPACE locks valt förslag

Nytt skapande-flöde:
1. Skriv `/fil skapa ` (eller `skapa fil: `) → autocomplete listar **mappar** (med `/` på slutet).
2. **TAB** cyclar genom mappar (filtreras live när du fortsätter skriva, t.ex. `/fil skapa do` → `docs/`).
3. **SPACE** låser valt förslag — input behåller `/fil skapa docs/`, suggestion-listan stängs, cursorn på slutet.
4. Skriv `nyfil.md = innehåll` fritt.
5. Enter → pending approval popup.

Implementation:
- `splitFileCommandV11` taggar varje pattern med `mode: "create" | "open"`. Nya patterns för `/fil skapa` och `skapa fil:`.
- `fileSuggestions` returnerar `allFolders`-baserade förslag med trailing `/` när `mode === "create"`. Stoppar visa förslag när `=` dyker upp (innehållsfasen).
- Ny SPACE-handler i input-keydown: om suggestion-listan är synlig OCH input matchar exakt `currentSuggestions[suggestionIndex]` → preventDefault, hideSuggestions, cursor till slutet.
- Hint-texten i suggestion-listan uppdaterad: `TAB = byt/fyll förslag • Space = lås valt förslag • Enter = skicka • Esc = stäng`.

Test: `tests\create-folder-suggestions.test.js` — 4 markers + splitFileCommandV11 mode-cases + fileSuggestions folder output (med och utan filter, samt `=`-stopp).

Verifiering: alla 19 node-tester gröna, C# router-tester gröna, `dotnet build` 0 errors.

Publish/restart: stoppade gamla Jarvis (PID 74224), publish lyckades, ny Jarvis igång som PID 86668 (SessionId 11).

## 2026-05-06 — Enklare separator: `=` istället för `|` i filkommandon

Skäl: `|` kräver `AltGr+<` på svensk kb och är besvärligt. Användaren valde `=`.

Ändringar:
- Ny helper `CommandRouterV1.SplitFileCommandArguments(raw, maxParts)` — väljer vilken separator (`=` eller `|`) som dyker upp först i raden. `|` behålls som fallback så befintliga muskelminne/docs/tester fortsätter funka.
- Alla 7 parse-platser använder helpern: `/fil skapa`, `skriv fil:`, `lägg till fil:`, `skapa fil:`, `föreslå rubrik:`, `föreslå ändring:`, `radera fil:` (path-cleanup).
- Hjälptext, `BuildHelp`, `ToolRegistryV1`-exempel och felmeddelanden visar nu `=` som förstaval.
- C# router-test utökat med 4 nya cases för helpern + `/fil skapa` med `=`.

Verifiering:
- `dotnet run --project tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` — alla tester gröna inkl. nya `=`-cases.
- Alla 17 node-tester gröna.
- `dotnet build` 0 errors, känd `MSB3277` warning kvar.

**Publish/restart slutförd** efter att användaren stängde gamla Jarvis manuellt. `dotnet publish` lyckades, ny Jarvis igång som PID 74224 (SessionId 11 — korrekt user-session). `=`-separatorn är aktiv i UI:t.

Användarexempel efter omstart:
- `/fil skapa docs/test-eq.md = hej från eq-separator`
- `skriv fil: docs/test-agent.md = TESTAR =-separator`
- `föreslå rubrik: docs/test-agent.md = Test Agent`

## 2026-05-06 — Översikt live-state: aktiv mapp + senaste filändring
