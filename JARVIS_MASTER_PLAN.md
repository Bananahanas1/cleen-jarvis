# JARVIS_MASTER_PLAN.md

Senast uppdaterad: 2026-05-12

## Huvudmål

`cleen-jarvis` ska vara huvudprodukten: en lokal svensk developer/control
assistant som först blir expert på sitt eget repo och sedan på användarens
andra projekt.

Lokal arbetsmapp: `F:\Jarvis-clean`.

## Produktroller

### cleen-jarvis

- huvudprodukt
- allt viktigt byggs här
- ska hållas stabilt
- ska buildas/testas
- ska pushas efter lyckade ändringar

### f-drive-projects

- referens
- backup
- inspirationskälla
- kan användas för idéer/kodmönster
- ska inte behandlas som huvudprodukt

### F:\New project

- read-only reference
- får inte ändras
- får bara läsas/inspirera

## Nästa stora prioritet

**Project Index + Background Jobs MVP**

Det går före Kartan och andra stora future-features.

Målet är att Jarvis kan:

- svara direkt
- starta lång analys som background job
- indexera projektet lokalt
- återanvända summaries
- hämta relevant context via RAG
- skapa rapporter utan att blockera chatten

Se [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md).

## Stabil grund som redan finns

Se [CURRENT_PROJECT_AUDIT.md](CURRENT_PROJECT_AUDIT.md).

Kort sagt finns redan dashboard, Project Explorer, filpanel, terminalpanel,
lokal Ollama-chat, markdown-minne, CommandRouter/Validator/ToolRegistry,
PendingApproval, safe write/delete/undo, ModelRouter, ConversationHistory,
WebSearcher, SafeAppLauncher, BuilderMode, NaturalEditTool, desktop-control via
approval, vault/AI-kontext, brain-vy och tester.

## Säker kontroll-loop

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

Riskabla actions kräver routing, validation, pending preview, approval,
verification och report.

## GitHub-sync

GitHub ska ha senaste fungerande versionen av `cleen-jarvis`.

Efter större lyckad ändring:

1. `git status`
2. relevant build/test
3. `git add` för avsedda filer
4. `git commit -m "<kort tydligt meddelande>"`
5. `git push`

Om build/test failar: pusha inte som färdig fungerande version. Dokumentera
kommandot, felet, trolig orsak och nästa steg.

## Kartan

Kartan ligger i [KARTAN_INDEX.md](KARTAN_INDEX.md). Den är viktig men inte
första build. Bygg först stabil core med project index, background jobs och RAG.
