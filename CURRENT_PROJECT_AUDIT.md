# CURRENT_PROJECT_AUDIT.md

Senast uppdaterad: 2026-05-12

## Slutsats

`cleen-jarvis` har redan mycket grund. Det som saknas är inte fler stora idéer,
utan bättre produktordning, bakgrundsjobb, projektindex, RAG och stabil
build/test/push-loop.

## Produktroll

- `cleen-jarvis` är huvudprodukten.
- Lokal arbetsmapp är `F:\Jarvis-clean`.
- `f-drive-projects` är referens/backup/inspiration.
- `F:\New project` är read-only reference och får inte ändras.

## Redan på plats

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
- tests
- säkerhetsregler

## Styrkor

- Lokala kommandon kan fångas före Ollama.
- Riskabla actions har en gemensam PendingApproval-väg.
- Dashboarden har tydliga ytor för filer, terminal, översikt, brain och chat.
- Projektet har både Node-regressionstester och C# routertester.
- Jarvis kan redan bygga vidare på ModelRouter och ConversationHistory.

## Risker

- För långa dokument gör handoff långsam.
- Jarvis kan bli seg om den försöker läsa hela repo direkt i chatten.
- För många future-features kan dra fokus från core-stabilitet.
- Orelaterade lokala runtime/cache-filer kan råka stage:as om git hanteras slarvigt.
- Kartan och 3D kan bli tungt om de byggs före project index/background jobs.

## Rekommendation

Bygg Project Index + Background Jobs MVP först:

1. Snabbt första svar.
2. Background job queue.
3. Read-only project scan.
4. Filmetadata + hashes.
5. Incremental scan.
6. Summaries per fil/mapp.
7. RAG/smart context.
8. Deep audit som sparad rapport.

Se [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md).
