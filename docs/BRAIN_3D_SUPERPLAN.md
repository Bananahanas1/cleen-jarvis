# BRAIN_3D_SUPERPLAN.md — 3D Brain View för Jarvis-clean

Skapad: 2026-05-10
Driver: användarens feedback om att 3D ska likna `tmp-jarvis-2.0.16-check.png` (NeuroLinked-stil), noderna ska representera projektfiler + vault, och vaulten ska användas som AI-kontext.

## Mål från användaren

1. **3D-noder = riktiga filer/mappar** i Project Explorer (klick öppnar fil)
2. **Vault är ETT enda valv** i `F:\Jarvis-clean\vault\` där all info som MD-filer (Obsidian-stil)
3. **Jarvis läser vaulten innan varje svar** — semantisk kontext direkt från MD-filer
4. **Ska likna `tmp-jarvis-2.0.16-check.png`** (sci-fi mörk dashboard med glow, paneler, color-coded noder) snarare än Obsidian 2D
5. **Inte bara visuellt** — funktionalitet på riktigt: klick, hover, sök, rensa cache

## Nuläge (efter senaste fix)

- ✅ FileGraphBuilder fungerar: 168 projekt-filer + 250 vault-noter, 272 edges
- ✅ Performance: 29s → 331ms tack vare vault-cap + pre-check skip
- ✅ Cache till `.checkpoints/.brain-graph-cache.json` (5 min TTL)
- ✅ Brain-mode döljer Project Explorer och edit-knappar — bara 3D + chat
- ❌ Visuellt fortfarande för "platt" — ingen glow, generisk Three.js-look
- ❌ Vault läses inte automatiskt av Ollama innan svar
- ❌ Klick på Project Explorer-fil → ingen highlight i Brain
- ❌ Rensa cache via UI saknas

## Arkitektur — slutmål

```
┌──── MAIN-FÖNSTER ────────────────────────────────────────┐
│                                                           │
│  [Brain-läge aktivt]                                     │
│                                                           │
│  ┌─ STATS ─┐  ┌──────── 3D BRAIN ────────┐  ┌─ CHAT ──┐│
│  │ Filer   │  │                            │  │ Jarvis  ││
│  │ Noter   │  │   ●───●  ←── projekt       │  │  läser  ││
│  │ Edges   │  │    \  /                    │  │  vault  ││
│  │ Vault   │  │   ●─●●●  ←── vault         │  │  innan  ││
│  └─────────┘  │      \ /                   │  │  svar   ││
│  ┌─ FILTER ┐  │   ●  ●                     │  │         ││
│  │ □ cs    │  │                            │  │  Du:    ││
│  │ □ js    │  │  Klick → öppna fil         │  │  Jarvis:││
│  │ ☑ md    │  │  Hover → highlight grannar │  │         ││
│  │ ☑ vault │  └────────────────────────────┘  └─────────┘│
│  └─────────┘                                              │
│  ┌─ INSPECTOR ───────────────────────┐                   │
│  │ docs/UNIFICATION_PLAN.md          │                   │
│  │ Typ: md  · Källa: project · Deg:7 │                   │
│  │ [Öppna i editor] [Visa connections]│                  │
│  └────────────────────────────────────┘                   │
└──────────────────────────────────────────────────────────┘
```

## Faser

### Fas BR1 — Visual NeuroLinked-stil (~2h)

**Mål:** 3D ska se ut som `tmp-jarvis-2.0.16-check.png`.

- [ ] **BR1.1** Lägg till `UnrealBloomPass` post-processing för glow på noder
- [ ] **BR1.2** Byt `MeshPhongMaterial` → `Points` med custom shader (mer prestanda + glow per nod)
- [ ] **BR1.3** Pulsing emissive intensity baserat på nod-degree (mer kopplade = stör glow)
- [ ] **BR1.4** Subtilare lines: `LineBasicMaterial` med opacity 0.12, additive blending
- [ ] **BR1.5** Mörk djup-blå bakgrund med subtil grid eller star-field
- [ ] **BR1.6** Sci-fi-paneler runtom: STATS (vänster top), FILTER (vänster mid), INSPECTOR (höger bottom)
- [ ] **BR1.7** Glas-effekt på paneler: `backdrop-filter: blur`, border 1px cyan med opacity

**Verifiering:** Screenshot bredvid `tmp-jarvis-2.0.16-check.png` — ska kännas i samma stil.

### Fas BR2 — Vault som AI-kontext (~1.5h)

**Mål:** Innan Ollama svarar läser Jarvis topp 3-5 mest relevanta vault-noter och skickar som system-prompt-kontext.

- [ ] **BR2.1** `app/Brain/VaultSearcher.cs` — TF-IDF lite eller bara substring-match över alla MD-filer i `F:\Jarvis-clean\vault\`
- [ ] **BR2.2** Cache vault-index i minne (rebuild om någon `.md` ändrats)
- [ ] **BR2.3** I `HandleMessageAsync` när intent är NormalChat → kalla `VaultSearcher.TopMatches(userMessage, k=5)` → bygg system-prompt prefix
- [ ] **BR2.4** UI: i Översikt-panelen, ny cell "Vault: N noter aktiva (senast använt: X för 3 min sen)"
- [ ] **BR2.5** Slash-kommando `/vault sök <query>` → visar topp 10 träffar i chat
- [ ] **BR2.6** Slash-kommando `/vault skapa <namn> = <text>` → skapa ny MD-fil i vaulten via PendingApproval
- [ ] **BR2.7** Auto-promotion: efter `kom ihåg` → spara i memory.md OCH vault som `vault/auto/<datum>-<topic>.md`

**Verifiering:** Skriv "vad sa vi om unification-planen?" → Jarvis svarar med kontext från `vault/UNIFICATION_PLAN.md` om den finns.

### Fas BR3 — Project Explorer ↔ Brain sync (~30 min)

**Mål:** Klicka på en fil i Project Explorer (vänster) → motsvarande nod highlightas i Brain.

- [ ] **BR3.1** Project Explorer file-row click → posta `jarvis_highlight_brain_node` med path
- [ ] **BR3.2** Brain-canvas lyssnar och pulsar noden i 1.5s med extra glow
- [ ] **BR3.3** Brain-nod-klick → posta tillbaka `jarvis_highlight_explorer_path` så Project Explorer markerar raden

**Verifiering:** Klicka `app/Program.cs` i vänsterpanelen → noden lyser upp i Brain-vyn.

### Fas BR4 — Filter, sök, och cache-mgmt (~45 min)

**Mål:** Användaren ska kunna filtrera vad som visas i 3D + manuellt rensa cache.

- [ ] **BR4.1** Filter-checkboxar i panel: `cs`, `js`, `html`, `css`, `py`, `md`, `json`, `vault` — toggle visibility
- [ ] **BR4.2** Sökfält i panel: live-filter på `path` substring → noder som inte matchar fadar till 8% opacity
- [ ] **BR4.3** Knapp `Bygg om grafen` (raderar cache → C# rebuild)
- [ ] **BR4.4** Inställning: max-noder-cap (slider 100-500)

**Verifiering:** Bocka av `vault` → bara projekt-noder syns. Sök "Program" → bara Program.cs och Program-relaterade noder lyser.

### Fas BR5 — Vault som "ETT valv" enligt användarens spec (~30 min)

**Mål:** Konsolidera all "info" Jarvis behöver veta i `F:\Jarvis-clean\vault\` enligt Obsidian-konvention.

- [ ] **BR5.1** Skapa initial struktur:
  ```
  vault/
    Index.md            ← startpunkt med [[wikilinks]] till resten
    Project/            ← projekt-info (porterad från docs/)
      UNIFICATION_PLAN.md
      MULTI_WINDOW_DESIGN.md
      MIGRATION_FROM_NEW_PROJECT.md
    Sessions/           ← varje session-log som egen fil (auto-skapad)
    Memory/             ← promovera viktigt minne hit
    Decisions/          ← arkitektur-beslut
    Issues/             ← öppna buggar
  ```
- [ ] **BR5.2** Migrera `docs/UNIFICATION_PLAN.md` etc. till `vault/Project/` (eller länka)
- [ ] **BR5.3** `vault/Index.md` med `[[wikilinks]]` så Brain-grafen får en uppenbar startpunkt
- [ ] **BR5.4** Auto-uppdatering: när `CURRENT_STATE.md` ändras → spegla till `vault/Project/CURRENT_STATE.md`

**Verifiering:** `Index.md` syns i Brain som central nod med många edges utåt.

### Fas BR6 — AI auto-läsning innan svar (~1h)

**Mål:** Varje gång Jarvis svarar normal chat ska den ha läst de mest relevanta vault-noterna.

- [ ] **BR6.1** I `AskOllamaAsync` (eller motsvarande): innan request, kalla `VaultSearcher.TopMatches`
- [ ] **BR6.2** Bygg system-prompt:
  ```
  Du är Jarvis. Läs först dessa kontext-noter från vaulten:

  --- vault/Project/UNIFICATION_PLAN.md ---
  [första 500 tecken]

  --- vault/Memory/Azu_preferences.md ---
  [första 500 tecken]

  Svara nu på användarens fråga med denna kontext i åtanke.
  ```
- [ ] **BR6.3** Loggning: chip i Översikt visar "Senaste svar använde: 3 vault-noter (UNIFICATION_PLAN, ...)"
- [ ] **BR6.4** Kostnad/storlek-skydd: max 4000 tecken vault-kontext per request
- [ ] **BR6.5** Slash `/vault av` / `/vault på` för att toggle kontext-injektion

**Verifiering:** Skriv "vad är Fas 5?" → Jarvis svarar relevant utan att jag förklarar projektet.

## Tidsuppskattning

| Fas | Vad | Tid |
|---|---|---|
| BR1 | Visual NeuroLinked-stil | 2h |
| BR2 | Vault som AI-kontext (basic) | 1.5h |
| BR3 | Project Explorer ↔ Brain sync | 30m |
| BR4 | Filter, sök, cache-mgmt | 45m |
| BR5 | Vault-struktur enligt spec | 30m |
| BR6 | AI auto-läsning innan svar | 1h |

**Totalt: ~6 timmar**

## Beslut som krävs av användaren INNAN start

1. **Vault-källa**: Bara `F:\Jarvis-clean\vault\` (litet, snabbt) eller även `F:\New project\obsidian-vault\` (3694 noter)?
2. **AI auto-läsning**: Default ON eller OFF? (ON = Jarvis blir smartare men varje request laddar ~2KB extra; OFF = användaren skriver `/vault på`)
3. **Sci-fi vs Obsidian-stil**: Bekräftar du att tmp-jarvis-2.0.16-check.png är riktningen, eller mer Obsidian-graph-aktigt?
4. **Vault-struktur**: Är min föreslagna `vault/Project/`, `vault/Sessions/`, `vault/Memory/` OK?

## Risker

| Risk | Mitigering |
|------|------------|
| UnrealBloomPass har dålig prestanda på svaga GPU | Render-toggle "Lågt grafikläge" som hoppar över bloom |
| Vault-search lägger 1-3s latens på varje chat | Async pre-fetch under användarens skrivande, max-cap 4KB kontext |
| Auto-läsning gör svar repetitiva ("som vi diskuterade i Fas 5...") | Specifik system-prompt-instruktion: "läs men hänvisa bara om relevant" |
| Vault växer ohållbart | Auto-archive: noter > 30 dagar utan modifikation flyttas till `vault/Archive/` |

## Vad som händer om du säger "kör"

Jag startar med Fas BR1 (visual stil) eftersom det ger mest synlig effekt direkt. Sedan BR2 (vault-kontext för AI) som är funktionellt viktigast. BR3-BR6 i ordning.

Verifierings-checklista per fas:
- `dotnet build` → 0 errors
- Alla node-tester gröna
- Alla C#-tester gröna
- Manuell rundtur i UI
- Skärmdumpsjämförelse mot referens
