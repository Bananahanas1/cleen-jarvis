# MASTER_PLAN.md - Jarvis långsiktig plan

Senast uppdaterad: 2026-05-12

## Läs först

Den aktiva masterplanen finns i:

- [JARVIS_MASTER_PLAN.md](JARVIS_MASTER_PLAN.md)
- [PLANNING_INDEX.md](PLANNING_INDEX.md)
- [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md)
- [JARVIS_CORE_INDEX.md](JARVIS_CORE_INDEX.md)

Detta dokument finns kvar som kompatibel kortlänk för äldre handoffar.

## Huvudmål

`cleen-jarvis` ska vara huvudprodukten. Lokal arbetsmapp är `F:\Jarvis-clean`.
`f-drive-projects` är referens/backup/inspiration. `F:\New project` är
read-only reference och får aldrig ändras.

## Aktuell prioritet

Nästa riktiga build:

**Project Index + Background Jobs MVP**

Detta går före Kartan, liveflyg, livebåtar, avancerad 3D Earth,
weather-animationer och andra stora future-features.

## Säker kontroll-loop

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

Riskabla actions ska alltid gå via routing, validation, pending preview,
approval, verification och report.

## GitHub-sync

Efter större lyckad ändring:

1. Kontrollera `git status`.
2. Kör relevant build/test.
3. Commit och push till GitHub om allt passerar.

Pusha aldrig secrets, `.env`, tokens, API-nycklar eller orelaterade
runtime/cache-filer.
