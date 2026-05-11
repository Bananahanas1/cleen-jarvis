# OFFLINE_CODEX_PLAN.md — Lokal Codex-liknande kodagent

Senast uppdaterad: 2026-05-05

## Statusnotering

Flera tidiga faser är nu implementerade eller delvis implementerade:

- safe file tools
- project index
- pending approval
- diff/review
- undo V1
- terminal preview/build
- CommandRouter
- CommandValidator
- ToolRegistry
- Terminal-panel V1

Återstående arbete ska fokusera på:

- file panel edit mode med pending save
- terminal transcript formatting
- explicit `/fil skapa`
- named checkpoints/history
- task workspace
- worker delegation
- safe local model/provider setup

## Slutmål

Jarvis ska fungera som en lokal/offline Codex-liknande kodagent.

Det betyder att Jarvis ska kunna:

- läsa projektfiler
- förstå projektstruktur
- föreslå ändringar
- visa diff innan ändring
- skriva små ändringar efter godkännande
- skapa checkpoint/backup innan större ändringar
- köra build/test säkert
- hjälpa till att fixa fel
- arbeta offline med lokal Ollama-modell
- rapportera vad som ändrades och hur användaren testar

## Viktig säkerhetsregel

Jarvis får inte få fri tillgång till hela datorn.

I början får Offline Codex-läget bara arbeta i:

```text
F:\Jarvis-clean
```

Jarvis får inte ändra:

- `F:\New project`
- `C:\`
- Downloads
- Desktop
- Documents
- bilder
- lösenord
- API-nycklar
- ultraPass-data

Andra F-drive roots ska vara read-only som default tills ett explicit permission-system finns.

## Implementeringsstatus

### Fas 1 — Säkra filverktyg

Status: implemented and hardened.

Fungerar:

- läsa säkra projektfiler
- write/append requests
- safe path/type checks
- no direct write before approval
- no silent create for missing files

Kvar:

- explicit `/fil skapa` med pending approval
- file panel edit mode med pending save

### Fas 2 — Projekt-index

Status: implemented and refreshed.

Filer:

- `docs\PROJECT_INDEX.md`

Kvar:

- förbättra indexering när task workspace och multi-root senare byggs

### Fas 3 — Läs-förstå-föreslå

Status: partially implemented.

Jarvis kan läsa och öppna projektfiler. Naturligt språk behöver fortsatt mappas säkrare till validated intents innan större edits föreslås.

Kvar:

- bättre safe intent mapping
- editor-save flow med pending preview
- proposed change flow kopplad till aktiv fil

### Fas 4 — Diff före ändring

Status: implemented for latest approved file write/append.

Fungerar:

- `Granska ändringar`
- green/red diff view
- close button
- Project Explorer highlight attempt

Kvar:

- toggle mellan normal view och diff view
- extend diff review to pending editor-save flow

### Fas 5 — Checkpoint/rollback

Status: partial.

Fungerar:

- basic checkpoint commands finns enligt tidigare logg
- one-step undo V1 finns för latest approved file write/append/delete

Kvar:

- named checkpoints/history
- tydligare UI för checkpoint/rollback
- koppla undo/checkpoint till task workspace

### Fas 6 — Build/test-runner

Status: partially implemented through terminal preview/approval.

Fungerar:

- `terminal preview: dotnet build`
- `/terminal preview dotnet build`
- approval popup
- terminal output to Terminal-panel V1
- compact chat summary
- latest terminal transcript in runtime memory

Kvar:

- terminal transcript formatting
- build/test/publish presets
- clearer allowlist/policy documentation

### Fas 7 — Enkel agent-loop

Status: planned.

Jarvis ska kunna:

1. observe project/UI/terminal state
2. map user request to safe intent
3. plan small steps
4. ask if risky
5. act
6. verify
7. report
8. remember/log after approval

Kvar:

- task workspace
- better natural-language intent mapping
- verification/report templates

### Fas 8 — Kodagent med bättre modell

Status: planned.

Nuvarande modeller hanteras lokalt via model commands/config. Kvar är tydligare setup docs/scripts för local Ollama/Claude Code provider workflows.

### Fas 9 — Offline dependency-cache

Status: planned.

Jarvis ska senare kunna kontrollera:

- NuGet cache på F:
- Ollama-modeller på F:
- pip-cache på F:
- npm-cache på F:
- inga internet-timeouts

### Fas 10 — Full lokal Codex-liknande assistent

Status: future.

När safety/workspace är stabilt kan Jarvis hjälpa mer aktivt med:

- fixa buildfel
- uppdatera dokumentation
- skapa filer efter approval
- refaktorera små delar
- köra test efter approval
- förklara kodbasen
- planera nästa utvecklingssteg

## Task workspace plan

Bygg senare:

```text
.jarvis/tasks/<task-id>/CONTEXT.md
.jarvis/tasks/<task-id>/TODO.md
.jarvis/tasks/<task-id>/NOTES.md
.jarvis/tasks/<task-id>/CHANGES.md
.jarvis/tasks/<task-id>/RESULT.md
.jarvis/tasks/<task-id>/SESSION_LOG.md
```

Task workspace ska stödja:

- create task
- open task
- list tasks
- add todo
- mark todo done
- summarize task
- handoff task to next AI/Codex session

## Worker delegation plan

Senare kan Jarvis använda worker-modeller/agenter för:

- läsa många filer
- sammanfatta mappar
- hitta relevant kod
- drafta dokumentation
- drafta kodändringar

Regler:

- workers får aldrig skriva direkt
- workers får aldrig köra terminal direkt
- workers får bara läsa, sammanfatta och föreslå
- main Jarvis validerar och kräver PendingApproval för writes/runs

## Vad som inte ska byggas nu

Inte än:

- full autonom agent
- ändra stora delar av kodbasen själv
- köra farliga terminalkommandon
- röra gamla `F:\New project`
- NeuroLinked
- tung 3D/WebGL
- ultraPass
- internetbaserade tools
- cloud/GitHub/Docker/Kubernetes
- desktop/browser control

## Nästa praktiska steg

Se också `TODO_NEXT.md`.

1. Manually verify terminal routing/cancel/terminal panel in Jarvis UI.
2. Manually verify pending file write/append/delete/undo in Jarvis UI.
3. Improve Project Explorer tree polish.
4. Build file panel edit mode with pending save.
5. Improve terminal transcript formatting.
6. Add explicit `/fil skapa` with pending approval.
7. Add named checkpoint/history beyond one-step undo.
8. Build task workspace later.
9. Build worker delegation later.
10. Add local Ollama/Claude Code setup docs/scripts later.
