# Habit: Läs alla markdown-filer innan planering

Senast uppdaterad: 2026-05-06

Syfte
- Införa en enkel vana att läsa igenom alla md-filer i projektet innan vi startar nya planeringar eller större ändringar. Detta ger kontext, minskar missförstånd och dokumenterar lärdomar i backloggen.

Varför detta är viktigt
- Vi bygger på en växande kunskapsgraf i Obsidian-valvet och i repo-dokumentationen. Att systematiskt läsa md-filer minskar risken för regressionsmissar och glömda beslut.

Vad som räknas som md-filer
- Alla filer med ändelsen .md inom workspace, särskilt: docs, root-nivåer som README.md, PLAN.md, etc.

Hur man gör det i praktiken (vara vanan)
- Vid varje avslutat arbetsdag eller innan varje större planering:
  - Läs igenom alla md-filer i repo (använd glob-sökning: **/*.md).
  - Sammanfatta viktiga insikter i en kort bullets i backlog eller i plan-dokumentet.
  - Uppdatera PLAN/MVP-backlog.md om relevanta nya insikter krävs.
  - Om nya beslut eller risker upptäcks, skapa en kort PendingChange i docs/change_archive eller i backlog.

Anteckning
- Detta är en arbetsmetod, inte ett kodcommit-säkringskrav. Det hjälper oss att hålla en konsekvent kontext.
