# Jarvis-clean / cleen-jarvis

`cleen-jarvis` är huvudprodukten. Lokal arbetsmapp i den här maskinen är:

```text
F:\Jarvis-clean
```

GitHub-repot är källan som ChatGPT och andra externa AI-agenter kan läsa från.
Om ändringar bara finns lokalt men inte är pushade kan de inte läsas från
GitHub.

## Produktroller

- `cleen-jarvis` - huvudprodukt, ska hållas stabil, buildas/testas och pushas.
- `f-drive-projects` - referens, backup och inspiration.
- `F:\New project` - read-only reference; får aldrig ändras.

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
- ModelRouter och ConversationHistory
- WebSearcher via browser
- SafeAppLauncher
- BuilderMode och NaturalEditTool
- desktop-control via pending approval
- vault/AI-kontext
- brain-vy
- tester och säkerhetsregler

## Nästa huvudprioritet

Nästa riktiga build är:

**Jarvis Project Index + Background Jobs MVP**

Detta går före Kartan, liveflyg, livebåtar, avancerad 3D Earth och andra
stora future-features. Jarvis ska svara snabbt och köra lång analys i
bakgrunden.

Se [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md).

## Viktiga regler

- Ändra aldrig `F:\New project`.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.
- Lokala kommandon ska hanteras före Ollama/LLM.
- Bara vanlig chat/resonemang ska gå till LLM.
- Filskrivning, append, delete, terminalkörning och desktop-control kräver pending preview och approval.
- Alla Markdown-filer ska vara under 14 000 tecken; långa dokument delas i PART-filer.

## Dokumentation

- [JARVIS_MEGA_MASTER_PROMPT.md](JARVIS_MEGA_MASTER_PROMPT.md)
- [PLANNING_INDEX.md](PLANNING_INDEX.md)
- [JARVIS_MASTER_PLAN.md](JARVIS_MASTER_PLAN.md)
- [JARVIS_CORE_INDEX.md](JARVIS_CORE_INDEX.md)
- [CURRENT_PROJECT_AUDIT.md](CURRENT_PROJECT_AUDIT.md)
- [CURRENT_STATE.md](CURRENT_STATE.md)
- [TODO_NEXT.md](TODO_NEXT.md)
- [NEXT_AI_AGENT_TODO.md](NEXT_AI_AGENT_TODO.md)
- [KARTAN_INDEX.md](KARTAN_INDEX.md)
- [docs/PROJECT_INDEX.md](docs/PROJECT_INDEX.md)
- [docs/SESSION_LOG.md](docs/SESSION_LOG.md)

## Start

Starta Jarvis via:

```text
F:\Jarvis-clean\Starta-Jarvis.vbs
```

eller via publicerad release i `dist\Jarvis.exe`.
