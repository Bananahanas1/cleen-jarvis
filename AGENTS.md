# AGENTS.md — Regler för AI-agenter

## Viktigaste regler

- Ändra aldrig F:\New project — den är read-only-referens. Vi får läsa och kopiera filer därifrån till F:\Jarvis-clean, men inte skriva tillbaka.
- Skriv bara i F:\Jarvis-clean.
- Efter lyckade runtime-ändringar får agenten stoppa, publicera och starta om Jarvis-clean automatiskt så användaren kan testa direkt.
- Publicera/starta inte om Jarvis vid docs-only/research-only arbete.
- **NeuroLinked + 3D**: får implementeras enligt `docs\UNIFICATION_PLAN.md`. Always-on Python-server är godkänd (auto-start vid main-app-start, offline-graceful). Stora arkitekturändringar måste fortfarande gå genom planen.
- All filskrivning från alla fönster (main, Brain, Explorer) går genom `PendingApprovalV1`.
- Inga lösenord, API-nycklar eller secrets i loggar.
- Gör små steg.
- Testa efter varje steg.
- Dokumentera varje ändring.

## Arbetsprincip

Gamla projektet är bara referens/läromaterial.

Jarvis-clean ska byggas steg för steg från en stabil minsta version.

## Publish/restart-regel

När kod eller dashboard-runtime har ändrats:

1. Kör relevanta tester.
2. Kör `dotnet build`.
3. Om allt passerar med 0 errors får Codex stoppa `JarvisClean`/`Jarvis`, köra `dotnet publish` och starta `Starta-Jarvis.vbs`.
4. Rapportera publish-resultat, eventuell känd varning och ny Jarvis PID om den går att läsa.

När bara dokumentation, research eller planering ändras:

- Kör `dotnet build` om det är rimligt.
- Publicera/starta inte om Jarvis.
