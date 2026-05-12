# TODO_NEXT.md - nästa praktiska steg

Senast uppdaterad: 2026-05-12

## Huvudprioritet

Nästa riktiga build:

**Jarvis Project Index + Background Jobs MVP**

Detta går före Kartan, liveflyg, livebåtar, avancerad 3D Earth,
weather-animationer och andra stora future-features.

## Aktiv nästa-lista

- [x] Skapa job data model för background jobs.
- [x] Skapa enkel job queue och background worker.
- [x] Lägg till status/progress/log/result för jobs.
- [x] Lägg till `/jobb`, `/jobb status`, `/jobb start`, `/jobb avbryt`.
- [x] Starta read-only project scan när användaren ber Jarvis läsa/analysera allt.
- [x] Svara direkt: "Jag börjar läsa och indexera projektet i bakgrunden. Du kan fortsätta skriva under tiden."
- [x] Skapa `data/project-index` med filmetadata, modified time och filhash.
- [x] Gör scan incremental: återanvänd hash/summaries för oförändrade filer.
- [x] Lägg till summaries per fil och mapp.
- [x] Lägg till enkel sökning och RAG/smart context från projektindex.
- [x] Lägg till deep audit som background job och sparad rapport.
- [x] Dokumentera Project Index incremental/search/audit-slicen i `docs/SESSION_LOG.md`.
- [x] Kör relevanta tester och `dotnet build` efter denna runtime-ändring.
- [x] Publish/restart efter grön runtime-ändring.
- [x] Commit och push efter gröna build/test för denna slice.

## Nästa efter denna slice

- [ ] Lägg till riktig pause/resume för background jobs.
- [ ] Lägg till mer avancerad chunk/map-reduce summary vid stora filer.
- [ ] Fortsätt Program.cs-refaktor: flytta terminal-, memory- och file-tool-logik till små services.

## Produktroll

- `cleen-jarvis` är huvudprodukt.
- `F:\Jarvis-clean` är lokal arbetsmapp.
- `f-drive-projects` är referens/backup/inspiration.
- `F:\New project` är read-only reference.

## Docs-pass 2026-05-12

- [x] Läste `JARVIS_UPDATE_TASK.md`.
- [x] Delade långa Markdown-filer i PART-filer.
- [x] Skapade `JARVIS_MEGA_MASTER_PROMPT.md`.
- [x] Skapade `PLANNING_INDEX.md`.
- [x] Skapade `JARVIS_CORE_INDEX.md`.
- [x] Skapade `KARTAN_INDEX.md`.
- [x] Skapade `JARVIS_BACKGROUND_JOBS_PLAN.md`.
- [x] Skapade `NEXT_AI_AGENT_TODO.md`.
- [x] Skapade `JARVIS_MASTER_PLAN.md`.
- [x] Skapade `CURRENT_PROJECT_AUDIT.md`.
- [x] Verifierade att alla Markdown-filer är under 14 000 tecken.
- [x] Kör `dotnet build` för docs-only sanity check: 0 errors, känd `MSB3277`.

## Historik

Den tidigare långa TODO-listan är bevarad i delar:

- [PART 01](TODO_NEXT_PART_01.md)
- [PART 02](TODO_NEXT_PART_02.md)
- [PART 03](TODO_NEXT_PART_03.md)
