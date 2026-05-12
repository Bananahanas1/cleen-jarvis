# NEXT_AI_AGENT_TODO.md

Senast uppdaterad: 2026-05-12

## Läs först

1. [AGENTS.md](AGENTS.md)
2. [JARVIS_MEGA_MASTER_PROMPT.md](JARVIS_MEGA_MASTER_PROMPT.md)
3. [PLANNING_INDEX.md](PLANNING_INDEX.md)
4. [CURRENT_STATE.md](CURRENT_STATE.md)
5. [TODO_NEXT.md](TODO_NEXT.md)
6. [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md)
7. [JARVIS_CORE_INDEX.md](JARVIS_CORE_INDEX.md)

## Viktigaste regel

Nästa build är **Project Index + Background Jobs MVP**.
Bygg inte Kartan först.

## Produktroll

- `cleen-jarvis` är huvudprodukten.
- `F:\Jarvis-clean` är lokal arbetsmapp.
- `f-drive-projects` är referens/backup/inspiration.
- `F:\New project` är read-only reference och får inte ändras.

## Nästa implementation när kodarbete startar

1. Lägg till job data model.
2. Lägg till background job queue.
3. Lägg till worker som kan köra read-only project scan.
4. Lägg till `/jobb status`, `/jobb lista`, `/jobb avbryt`.
5. Spara jobblogg/resultat under `data/jobs`.
6. Skapa `data/project-index` med filmetadata och hashes.
7. Gör scan incremental.
8. Lägg till enkel sökning i projektindex.
9. Koppla normal chat till relevant context från index.
10. Dokumentera och testa varje steg.

## Test/build-policy

Efter runtime-ändring:

- kör relevanta Node-tester
- kör C# routertester om routing ändras
- kör `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj`
- publish/restarta bara efter runtime-ändringar och gröna tester

Efter docs-only:

- kontrollera Markdown-längder
- kör `dotnet build` om rimligt
- publish/restarta inte Jarvis

## Git-policy

- Kontrollera `git status`.
- Stage:a bara avsedda filer.
- Commit tydligt.
- Push efter lyckad build/test när repo och credentials tillåter.
- Pusha aldrig `.env`, tokens, lösenord eller API-nycklar.
- Stage:a inte runtime-cache eller orelaterade användarändringar.

## Kom ihåg

Jarvis har redan många features. Nästa värde är ordning, index, bakgrundsjobb,
RAG och snabb respons.
