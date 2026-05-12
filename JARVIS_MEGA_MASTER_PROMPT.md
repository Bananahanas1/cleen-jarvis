# JARVIS_MEGA_MASTER_PROMPT.md

Senast uppdaterad: 2026-05-12

## Roll

Detta är en kort mega-översikt för nästa AI-agent som tar över Jarvis.
Den ska inte växa till en gigantisk prompt. Detaljer ligger i länkade
planfiler.

## Produktregel

- `cleen-jarvis` är huvudprodukten och GitHub-repot externa AI-agenter kan läsa.
- Lokal arbetsmapp just nu är `F:\Jarvis-clean`.
- `f-drive-projects` är bara referens, backup och inspiration.
- `F:\New project` är read-only reference och får aldrig ändras.
- Om ändringar bara finns lokalt men inte är pushade kan ChatGPT och andra AI-agenter inte läsa dem från GitHub.

## Huvudprioritet

Nästa riktiga build är:

**Jarvis Project Index + Background Jobs MVP**

Detta går före Kartan, liveflyg, livebåtar, avancerad 3D Earth,
weather-animationer och andra stora future-features.

Varför:
- Jarvis har redan mycket grundfunktioner.
- Det största praktiska problemet är att Jarvis kan bli seg när den försöker läsa allt.
- Jarvis ska svara snabbt direkt och köra djup analys i bakgrunden.

## Nuvarande grund

Jarvis har redan:

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
- tester
- säkerhetsregler

Slutsats: bygg bättre produktordning, bakgrundsjobb, projektindex,
RAG och stabil build/test/push-loop. Jaga inte fler stora idéer först.

## Viktiga index

- [PLANNING_INDEX.md](PLANNING_INDEX.md)
- [JARVIS_MASTER_PLAN.md](JARVIS_MASTER_PLAN.md)
- [JARVIS_CORE_INDEX.md](JARVIS_CORE_INDEX.md)
- [CURRENT_PROJECT_AUDIT.md](CURRENT_PROJECT_AUDIT.md)
- [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md)
- [KARTAN_INDEX.md](KARTAN_INDEX.md)
- [NEXT_AI_AGENT_TODO.md](NEXT_AI_AGENT_TODO.md)
- [CURRENT_STATE.md](CURRENT_STATE.md)
- [TODO_NEXT.md](TODO_NEXT.md)
- [docs/PROJECT_INDEX.md](docs/PROJECT_INDEX.md)
- [docs/SESSION_LOG.md](docs/SESSION_LOG.md)

## Säkerhetsregler

- Ändra aldrig `F:\New project`.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.
- Lokala kommandon ska hanteras före Ollama/LLM.
- Bara vanlig chat och resonemang ska gå till LLM.
- Filskrivning, append, delete, terminalkörning och desktop-control ska kräva pending preview och användarens godkännande.
- Workers får läsa, sammanfatta och föreslå, men aldrig skriva direkt.
- Undvik fler V4/V5/V6/V7/V8 smart-open patchar; bygg ren CommandRouter V1.

## GitHub-sync

Efter större lyckad ändring:

1. Kontrollera `git status`.
2. Kör relevant build/test.
3. Om allt passerar, stage:a bara avsedda filer.
4. Commit med tydligt meddelande.
5. Push till GitHub.

Pusha aldrig `.env`, tokens, lösenord eller API-nycklar.

## Markdown-regel

Alla Markdown-filer ska vara under 14 000 tecken, helst 8 000-12 000.
Långa filer ska delas i `*_PART_01.md`, `*_PART_02.md` osv med en kort
indexfil som länkar till delarna.
