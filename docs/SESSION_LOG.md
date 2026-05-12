# SESSION_LOG.md

## 2026-05-12 - Documentation split + Project Index/Background Jobs priority

Utgångspunkt: användaren bad att läsa `JARVIS_UPDATE_TASK.md` från början till
slut och faktiskt uppdatera filerna. Detta var ett docs-only pass.

Ändringar:

- Skapade `JARVIS_MEGA_MASTER_PROMPT.md` som kort index, inte jätteprompt.
- Skapade `PLANNING_INDEX.md`.
- Skapade `JARVIS_CORE_INDEX.md`.
- Skapade `KARTAN_INDEX.md`.
- Skapade `JARVIS_BACKGROUND_JOBS_PLAN.md`.
- Skapade `NEXT_AI_AGENT_TODO.md`.
- Skapade `JARVIS_MASTER_PLAN.md`.
- Skapade `CURRENT_PROJECT_AUDIT.md`.
- Uppdaterade `README.md`, `CURRENT_STATE.md`, `TODO_NEXT.md`, `MASTER_PLAN.md`
  och `docs/PROJECT_INDEX.md`.
- Dokumenterade att `cleen-jarvis` är huvudprodukten, `f-drive-projects` är
  referens/backup/inspiration och `F:\New project` är read-only reference.
- Dokumenterade GitHub-sync-regeln.
- Dokumenterade att Project Index + Background Jobs MVP går före Kartan.
- Delade alla Markdown-filer över 14 000 tecken i PART-filer.

Verifiering:

- Markdown-längdkontroll körd: alla `.md` är under 14 000 tecken.
- `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj` lyckades med 0 errors
  och känd `MSB3277` WindowsBase/WebView2-warning.
- Docs-only arbete: ingen publish/restart av Jarvis.

Kvar:

- Commit/push om build och git-läge tillåter.

## Historisk session-logg

Den tidigare långa `docs/SESSION_LOG.md` är bevarad i delar:

- [PART 01](SESSION_LOG_PART_01.md)
- [PART 02](SESSION_LOG_PART_02.md)
- [PART 03](SESSION_LOG_PART_03.md)
- [PART 04](SESSION_LOG_PART_04.md)
- [PART 05](SESSION_LOG_PART_05.md)
- [PART 06](SESSION_LOG_PART_06.md)
- [PART 07](SESSION_LOG_PART_07.md)
- [PART 08](SESSION_LOG_PART_08.md)
