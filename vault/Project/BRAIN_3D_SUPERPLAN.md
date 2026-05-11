---
type: project-doc
source_file: "docs/BRAIN_3D_SUPERPLAN.md"
updated: 2026-05-10
tags: [project, mirrored]
---

# BRAIN_3D_SUPERPLAN.md â€” 3D Brain View fÃ¶r Jarvis-clean

Skapad: 2026-05-10
Driver: anvÃ¤ndarens feedback om att 3D ska likna `tmp-jarvis-2.0.16-check.png` (NeuroLinked-stil), noderna ska representera projektfiler + vault, och vaulten ska anvÃ¤ndas som AI-kontext.

## MÃ¥l frÃ¥n anvÃ¤ndaren

1. **3D-noder = riktiga filer/mappar** i Project Explorer (klick Ã¶ppnar fil)
2. **Vault Ã¤r ETT enda valv** i `F:\Jarvis-clean\vault\` dÃ¤r all info som MD-filer (Obsidian-stil)
3. **Jarvis lÃ¤ser vaulten innan varje svar** â€” semantisk kontext direkt frÃ¥n MD-filer
4. **Ska likna `tmp-jarvis-2.0.16-check.png`** (sci-fi mÃ¶rk dashboard med glow, paneler, color-coded noder) snarare Ã¤n Obsidian 2D
5. **Inte bara visuellt** â€” funktionalitet pÃ¥ riktigt: klick, hover, sÃ¶k, rensa cache

## NulÃ¤ge (efter senaste fix)

- âœ… FileGraphBuilder fungerar: 168 projekt-filer + 250 vault-noter, 272 edges
- âœ… Performance: 29s â†’ 331ms tack vare vault-cap + pre-check skip
- âœ… Cache till `.checkpoints/.brain-graph-cache.json` (5 min TTL)
- âœ… Brain-mode dÃ¶ljer Project Explorer och edit-knappar â€” bara 3D + chat
- âŒ Visuellt fortfarande fÃ¶r "platt" â€” ingen glow, generisk Three.js-look
- âŒ Vault lÃ¤ses inte automatiskt av Ollama innan svar
- âŒ Klick pÃ¥ Project Explorer-fil â†’ ingen highlight i Brain
- âŒ Rensa cache via UI saknas

## Arkitektur â€” slutmÃ¥l

```
â”Œâ”€â”€â”€â”€ MAIN-FÃ–NSTER â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                                                           â”‚
â”‚  [Brain-lÃ¤ge aktivt]                                     â”‚
â”‚                                                           â”‚
â”‚  â”Œâ”€ STATS â”€â”  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€ 3D BRAIN â”€â”€â”€â”€â”€â”€â”€â”€â”  â”Œâ”€ CHAT â”€â”€â”â”‚
â”‚  â”‚ Filer   â”‚  â”‚                            â”‚  â”‚ Jarvis  â”‚â”‚
â”‚  â”‚ Noter   â”‚  â”‚   â—â”€â”€â”€â—  â†â”€â”€ projekt       â”‚  â”‚  lÃ¤ser  â”‚â”‚
â”‚  â”‚ Edges   â”‚  â”‚    \  /                    â”‚  â”‚  vault  â”‚â”‚
â”‚  â”‚ Vault   â”‚  â”‚   â—â”€â—â—â—  â†â”€â”€ vault         â”‚  â”‚  innan  â”‚â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚      \ /                   â”‚  â”‚  svar   â”‚â”‚
â”‚  â”Œâ”€ FILTER â”  â”‚   â—  â—                     â”‚  â”‚         â”‚â”‚
â”‚  â”‚ â–¡ cs    â”‚  â”‚                            â”‚  â”‚  Du:    â”‚â”‚
â”‚  â”‚ â–¡ js    â”‚  â”‚  Klick â†’ Ã¶ppna fil         â”‚  â”‚  Jarvis:â”‚â”‚
â”‚  â”‚ â˜‘ md    â”‚  â”‚  Hover â†’ highlight grannar â”‚  â”‚         â”‚â”‚
â”‚  â”‚ â˜‘ vault â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜                                              â”‚
â”‚  â”Œâ”€ INSPECTOR â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”                   â”‚
â”‚  â”‚ docs/UNIFICATION_PLAN.md          â”‚                   â”‚
â”‚  â”‚ Typ: md  Â· KÃ¤lla: project Â· Deg:7 â”‚                   â”‚
â”‚  â”‚ [Ã–ppna i editor] [Visa connections]â”‚                  â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜                   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

## Faser

### Fas BR1 â€” Visual NeuroLinked-stil (~2h)

**MÃ¥l:** 3D ska se ut som `tmp-jarvis-2.0.16-check.png`.

- [ ] **BR1.1** LÃ¤gg till `UnrealBloomPass` post-processing fÃ¶r glow pÃ¥ noder
- [ ] **BR1.2** Byt `MeshPhongMaterial` â†’ `Points` med custom shader (mer prestanda + glow per nod)
- [ ] **BR1.3** Pulsing emissive intensity baserat pÃ¥ nod-degree (mer kopplade = stÃ¶r glow)
- [ ] **BR1.4** Subtilare lines: `LineBasicMaterial` med opacity 0.12, additive blending
- [ ] **BR1.5** MÃ¶rk djup-blÃ¥ bakgrund med subtil grid eller star-field
- [ ] **BR1.6** Sci-fi-paneler runtom: STATS (vÃ¤nster top), FILTER (vÃ¤nster mid), INSPECTOR (hÃ¶ger bottom)
- [ ] **BR1.7** Glas-effekt pÃ¥ paneler: `backdrop-filter: blur`, border 1px cyan med opacity

**Verifiering:** Screenshot bredvid `tmp-jarvis-2.0.16-check.png` â€” ska kÃ¤nnas i samma stil.

### Fas BR2 â€” Vault som AI-kontext (~1.5h)

**MÃ¥l:** Innan Ollama svarar lÃ¤ser Jarvis topp 3-5 mest relevanta vault-noter och skickar som system-prompt-kontext.

- [ ] **BR2.1** `app/Brain/VaultSearcher.cs` â€” TF-IDF lite eller bara substring-match Ã¶ver alla MD-filer i `F:\Jarvis-clean\vault\`
- [ ] **BR2.2** Cache vault-index i minne (rebuild om nÃ¥gon `.md` Ã¤ndrats)
- [ ] **BR2.3** I `HandleMessageAsync` nÃ¤r intent Ã¤r NormalChat â†’ kalla `VaultSearcher.TopMatches(userMessage, k=5)` â†’ bygg system-prompt prefix
- [ ] **BR2.4** UI: i Ã–versikt-panelen, ny cell "Vault: N noter aktiva (senast anvÃ¤nt: X fÃ¶r 3 min sen)"
- [ ] **BR2.5** Slash-kommando `/vault sÃ¶k <query>` â†’ visar topp 10 trÃ¤ffar i chat
- [ ] **BR2.6** Slash-kommando `/vault skapa <namn> = <text>` â†’ skapa ny MD-fil i vaulten via PendingApproval
- [ ] **BR2.7** Auto-promotion: efter `kom ihÃ¥g` â†’ spara i memory.md OCH vault som `vault/auto/<datum>-<topic>.md`

**Verifiering:** Skriv "vad sa vi om unification-planen?" â†’ Jarvis svarar med kontext frÃ¥n `vault/UNIFICATION_PLAN.md` om den finns.

### Fas BR3 â€” Project Explorer â†” Brain sync (~30 min)

**MÃ¥l:** Klicka pÃ¥ en fil i Project Explorer (vÃ¤nster) â†’ motsvarande nod highlightas i Brain.

- [ ] **BR3.1** Project Explorer file-row click â†’ posta `jarvis_highlight_brain_node` med path
- [ ] **BR3.2** Brain-canvas lyssnar och pulsar noden i 1.5s med extra glow
- [ ] **BR3.3** Brain-nod-klick â†’ posta tillbaka `jarvis_highlight_explorer_path` sÃ¥ Project Explorer markerar raden

**Verifiering:** Klicka `app/Program.cs` i vÃ¤nsterpanelen â†’ noden lyser upp i Brain-vyn.

### Fas BR4 â€” Filter, sÃ¶k, och cache-mgmt (~45 min)

**MÃ¥l:** AnvÃ¤ndaren ska kunna filtrera vad som visas i 3D + manuellt rensa cache.

- [ ] **BR4.1** Filter-checkboxar i panel: `cs`, `js`, `html`, `css`, `py`, `md`, `json`, `vault` â€” toggle visibility
- [ ] **BR4.2** SÃ¶kfÃ¤lt i panel: live-filter pÃ¥ `path` substring â†’ noder som inte matchar fadar till 8% opacity
- [ ] **BR4.3** Knapp `Bygg om grafen` (raderar cache â†’ C# rebuild)
- [ ] **BR4.4** InstÃ¤llning: max-noder-cap (slider 100-500)

**Verifiering:** Bocka av `vault` â†’ bara projekt-noder syns. SÃ¶k "Program" â†’ bara Program.cs och Program-relaterade noder lyser.

### Fas BR5 â€” Vault som "ETT valv" enligt anvÃ¤ndarens spec (~30 min)

**MÃ¥l:** Konsolidera all "info" Jarvis behÃ¶ver veta i `F:\Jarvis-clean\vault\` enligt Obsidian-konvention.

- [ ] **BR5.1** Skapa initial struktur:
  ```
  vault/
    Index.md            â† startpunkt med [[wikilinks]] till resten
    Project/            â† projekt-info (porterad frÃ¥n docs/)
      UNIFICATION_PLAN.md
      MULTI_WINDOW_DESIGN.md
      MIGRATION_FROM_NEW_PROJECT.md
    Sessions/           â† varje session-log som egen fil (auto-skapad)
    Memory/             â† promovera viktigt minne hit
    Decisions/          â† arkitektur-beslut
    Issues/             â† Ã¶ppna buggar
  ```
- [ ] **BR5.2** Migrera `docs/UNIFICATION_PLAN.md` etc. till `vault/Project/` (eller lÃ¤nka)
- [ ] **BR5.3** `vault/Index.md` med `[[wikilinks]]` sÃ¥ Brain-grafen fÃ¥r en uppenbar startpunkt
- [ ] **BR5.4** Auto-uppdatering: nÃ¤r `CURRENT_STATE.md` Ã¤ndras â†’ spegla till `vault/Project/CURRENT_STATE.md`

**Verifiering:** `Index.md` syns i Brain som central nod med mÃ¥nga edges utÃ¥t.

### Fas BR6 â€” AI auto-lÃ¤sning innan svar (~1h)

**MÃ¥l:** Varje gÃ¥ng Jarvis svarar normal chat ska den ha lÃ¤st de mest relevanta vault-noterna.

- [ ] **BR6.1** I `AskOllamaAsync` (eller motsvarande): innan request, kalla `VaultSearcher.TopMatches`
- [ ] **BR6.2** Bygg system-prompt:
  ```
  Du Ã¤r Jarvis. LÃ¤s fÃ¶rst dessa kontext-noter frÃ¥n vaulten:

  --- vault/Project/UNIFICATION_PLAN.md ---
  [fÃ¶rsta 500 tecken]

  --- vault/Memory/Azu_preferences.md ---
  [fÃ¶rsta 500 tecken]

  Svara nu pÃ¥ anvÃ¤ndarens frÃ¥ga med denna kontext i Ã¥tanke.
  ```
- [ ] **BR6.3** Loggning: chip i Ã–versikt visar "Senaste svar anvÃ¤nde: 3 vault-noter (UNIFICATION_PLAN, ...)"
- [ ] **BR6.4** Kostnad/storlek-skydd: max 4000 tecken vault-kontext per request
- [ ] **BR6.5** Slash `/vault av` / `/vault pÃ¥` fÃ¶r att toggle kontext-injektion

**Verifiering:** Skriv "vad Ã¤r Fas 5?" â†’ Jarvis svarar relevant utan att jag fÃ¶rklarar projektet.

## Tidsuppskattning

| Fas | Vad | Tid |
|---|---|---|
| BR1 | Visual NeuroLinked-stil | 2h |
| BR2 | Vault som AI-kontext (basic) | 1.5h |
| BR3 | Project Explorer â†” Brain sync | 30m |
| BR4 | Filter, sÃ¶k, cache-mgmt | 45m |
| BR5 | Vault-struktur enligt spec | 30m |
| BR6 | AI auto-lÃ¤sning innan svar | 1h |

**Totalt: ~6 timmar**

## Beslut som krÃ¤vs av anvÃ¤ndaren INNAN start

1. **Vault-kÃ¤lla**: Bara `F:\Jarvis-clean\vault\` (litet, snabbt) eller Ã¤ven `F:\New project\obsidian-vault\` (3694 noter)?
2. **AI auto-lÃ¤sning**: Default ON eller OFF? (ON = Jarvis blir smartare men varje request laddar ~2KB extra; OFF = anvÃ¤ndaren skriver `/vault pÃ¥`)
3. **Sci-fi vs Obsidian-stil**: BekrÃ¤ftar du att tmp-jarvis-2.0.16-check.png Ã¤r riktningen, eller mer Obsidian-graph-aktigt?
4. **Vault-struktur**: Ã„r min fÃ¶reslagna `vault/Project/`, `vault/Sessions/`, `vault/Memory/` OK?

## Risker

| Risk | Mitigering |
|------|------------|
| UnrealBloomPass har dÃ¥lig prestanda pÃ¥ svaga GPU | Render-toggle "LÃ¥gt grafiklÃ¤ge" som hoppar Ã¶ver bloom |
| Vault-search lÃ¤gger 1-3s latens pÃ¥ varje chat | Async pre-fetch under anvÃ¤ndarens skrivande, max-cap 4KB kontext |
| Auto-lÃ¤sning gÃ¶r svar repetitiva ("som vi diskuterade i Fas 5...") | Specifik system-prompt-instruktion: "lÃ¤s men hÃ¤nvisa bara om relevant" |
| Vault vÃ¤xer ohÃ¥llbart | Auto-archive: noter > 30 dagar utan modifikation flyttas till `vault/Archive/` |

## Vad som hÃ¤nder om du sÃ¤ger "kÃ¶r"

Jag startar med Fas BR1 (visual stil) eftersom det ger mest synlig effekt direkt. Sedan BR2 (vault-kontext fÃ¶r AI) som Ã¤r funktionellt viktigast. BR3-BR6 i ordning.

Verifierings-checklista per fas:
- `dotnet build` â†’ 0 errors
- Alla node-tester grÃ¶na
- Alla C#-tester grÃ¶na
- Manuell rundtur i UI
- SkÃ¤rmdumpsjÃ¤mfÃ¶relse mot referens

