# PLANNING_INDEX.md

Senast uppdaterad: 2026-05-12

## Syfte

Detta är plan-indexet för `cleen-jarvis` / `F:\Jarvis-clean`.
Det ska hjälpa nästa AI-agent att hitta rätt dokument snabbt utan att läsa
gigantiska Markdown-filer.

## Produktroll

- `cleen-jarvis` är huvudprodukt.
- `F:\Jarvis-clean` är lokal arbetsmapp för huvudprodukten.
- `f-drive-projects` är referens, backup och inspiration.
- `F:\New project` är read-only reference.
- GitHub-repot är källan externa AI-agenter kan läsa från.

## Aktuell build-prioritet

1. Jarvis Project Index + Background Jobs MVP
2. RAG/smart context ovanpå projektindexet
3. Stabil build/test/push-loop
4. Task workspace och read-only worker delegation
5. Kartan som separat framtida feature

Kartan ska inte vara första huvudbuild.

## Planfiler

- [JARVIS_MEGA_MASTER_PROMPT.md](JARVIS_MEGA_MASTER_PROMPT.md) - kort huvudprompt/index.
- [JARVIS_MASTER_PLAN.md](JARVIS_MASTER_PLAN.md) - strategisk masterplan.
- [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md) - nästa MVP.
- [JARVIS_CORE_INDEX.md](JARVIS_CORE_INDEX.md) - core-arkitektur och säkerhetskontrakt.
- [CURRENT_PROJECT_AUDIT.md](CURRENT_PROJECT_AUDIT.md) - nulägesaudit.
- [KARTAN_INDEX.md](KARTAN_INDEX.md) - Kartan-plan, nedprioriterad efter core-MVP.
- [NEXT_AI_AGENT_TODO.md](NEXT_AI_AGENT_TODO.md) - konkret nästa agent-checklista.
- [CURRENT_STATE.md](CURRENT_STATE.md) - kort aktuell status.
- [TODO_NEXT.md](TODO_NEXT.md) - aktiv nästa-lista.
- [BUILD_PLAN.md](BUILD_PLAN.md) - äldre fasplan och långsiktig riktning.
- [MASTER_PLAN.md](MASTER_PLAN.md) - äldre masterplan, hålls som kort hänvisning.

## Projektindex och handoff

- [docs/PROJECT_INDEX.md](docs/PROJECT_INDEX.md)
- [docs/CODEX_HANDOFF.md](docs/CODEX_HANDOFF.md)
- [docs/CODEX_START_PROMPT.md](docs/CODEX_START_PROMPT.md)
- [docs/SESSION_LOG.md](docs/SESSION_LOG.md)
- [docs/JARVIS_LONG_TERM_VISION.md](docs/JARVIS_LONG_TERM_VISION.md)

Långa historiska dokument är uppdelade i PART-filer. Läs indexfilen först
och därefter part-filerna i ordning vid behov.

## GitHub-sync-regel

Efter större ändring i `cleen-jarvis`:

1. Kontrollera `git status`.
2. Kör relevant build och tester.
3. Om build/test passerar: commit och push.
4. Om tester saknas: dokumentera det.
5. Om något failar: dokumentera kommando, fel, trolig orsak och nästa steg.

Stage:a inte runtime-cache, lokala configändringar, `.env`, tokens eller
andra orelaterade användarändringar.

## Markdown-regel

Alla `.md`-filer ska vara under 14 000 tecken. Om ett dokument behöver mer
plats ska det bli en kort indexfil plus `*_PART_01.md`, `*_PART_02.md` osv.
