# CODEX_HANDOFF.md — Jarvis-clean handoff for Codex

Senast uppdaterad: 2026-05-05

## Kort sammanfattning

Jarvis-clean är ett lokalt Windows/C# projekt som ska bli en svensk developer/control assistant, inte bara en chatbot.

Projektets huvudmapp:

```text
F:\Jarvis-clean
```

Viktig regel:

- Jarvis får bara skriva/ändra i `F:\Jarvis-clean`, och bara genom säkra regler.
- `F:\New project` är read-only reference och ska aldrig ändras.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.

Den större riktningen finns i `docs\JARVIS_LONG_TERM_VISION.md`.

Säker kontroll-loop:

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

## Nuvarande stabil status

Det här är inte längre bara en skeleton-fas.

Fungerar nu:

- Safe dashboard med 3-panel-layout.
- Project Explorer till vänster.
- Filpanel/kodvisare i mitten.
- Jarvis Chat till höger.
- Terminal-panel V1 i mittenytan.
- Jarvis Översikt-panel finns som praktisk statusyta för aktiv fil, pending approval, terminal, minne, Obsidian-status och Jarvis kontroll-loop.
- Lokal Ollama-chat.
- Lokal markdown-memory.
- CommandRouter V1 finns och slash-kommandon är implementerade för help/status/minne/fil/terminal-delar.
- CommandValidator V1 finns och blockerar saknade/riskabla argument.
- ToolRegistry V1 finns som tool metadata.
- PendingApproval V1 hanterar file write, append, delete, undo och terminal preview/approval.
- PendingApproval V1 hanterar även source-implementerad file create och file-panel pending save.
- Dashboard smart-open har guardrails så router-only/risky commands inte blir file-open.
- Gamla smart-open V3/V4/V5/V6/V7-implementationer har rensats till en canonical smart-open path.
- Terminal preview/approval använder popup och PendingApproval.
- Full terminaloutput går till Terminal-panelen; chatten får kort sammanfattning.
- File panel `Edit-läge` / `Spara med godkännande` finns i source och skapar pending write-preview.
- `/fil skapa docs/test.md | text` finns i source och skapar pending file-create-preview.
- `visa terminal`, `vad stod i terminalen`, `senaste terminal`, `terminal output` och `/terminal visa` routas lokalt.
- Generic `avbryt`, `avbryt allt`, `cancel` och `stoppa` är context-aware.
- Known warning: WindowsBase/WebView2 version conflict warning. Build har 0 errors.

## Viktig arkitektur

UI:

- `dashboard/index.html` är Jarvis dashboard.
- Filpanelen ska visa kod/text utan att spamma chatten.
- Terminal-panelen ska bära full terminaloutput.
- Chatten ska visa korta summaries och beslut.
- Project Explorer behöver mer tree polish.

C#:

- `app/Program.cs` är fortfarande huvud-runtimefilen.
- `app/CommandRouterV1.cs` är central routing för flera lokala intents.
- `app/CommandValidatorV1.cs` validerar CommandResult-objekt.
- `app/ToolRegistryV1.cs` beskriver tool-metadata.
- `app/PendingApprovalV1.cs` är gemensam approval-bas för riskabla actions.

## Routingregel

All input ska i praktiken följa:

1. Normalize input.
2. Slash commands går till CommandRouter V1.
3. Natural language försöker mappas till safe local intent.
4. Validate arguments.
5. Kör safe local tool.
6. Om write/append/delete/undo/terminal/risky action: skapa pending preview och kräv user approval.
7. Bara normal chat och resonemang går till Ollama.

## Slash commands

Implementerade eller delvis implementerade:

- `/hjälp`
- `/status`
- `/minne visa`
- `/minne viktiga`
- `/minne projekt`
- `/minne sök <text>`
- `/minne arkiv sök <text>`
- `/fil öppna <path>`
- `/fil läs <path>`
- `/fil skapa <path> = <text>` (separator `=` föredragen, `|` accepteras som fallback)
- `/terminal preview <command>`
- `/terminal godkänn`
- `/terminal avbryt`
- `/terminal visa`
- `/översikt`
- `/minne status`
- `/obsidian status`

Naturligt språk med samma separator-regel:
- `skriv fil: <path> = <text>`
- `lägg till fil: <path> = <text>`
- `skapa fil: <path> = <text>`
- `föreslå rubrik: <path> = <heading>`
- `föreslå ändring: <path> = <instruction>`

Fortsätt inte om från `/hjälp` och `/status`. Bygg vidare från aktuellt läge.

## Safety rules

- `F:\New project` är read-only reference.
- Andra F-drive roots ska vara read-only som default.
- Skrivning utanför `F:\Jarvis-clean` är blockerad tills ett explicit permission-system finns.
- File write, append, delete och undo kräver PendingApproval.
- Terminal run kräver preview och approval.
- Externa tools och framtida UI/browser automation ska kräva safety checks, approval, verification och report.
- Workers/LLM får aldrig skriva direkt. De får läsa, sammanfatta och föreslå.
- Real 3D/Obsidian-sync/NeuroLink ska vara valfritt och off by default tills safety/workspace är stabilt.

## Tester att känna till

Viktiga testfiler:

- `tests\dashboard-routing.test.js`
- `tests\terminal-approval-safety.test.js`
- `tests\approval-popup.test.js`
- `tests\help-text.test.js`
- `tests\file-write-safety.test.js`
- `tests\file-delete-safety.test.js`
- `tests\undo-safety.test.js`
- `tests\smart-open-cleanup.test.js`
- `tests\CommandRouterV1.Tests\Program.cs`

Senaste verifierade status enligt loggar:

- dashboard routing tests passed
- terminal approval safety tests passed
- approval popup tests passed
- help text tests passed
- CommandRouterV1 tests passed
- `dotnet build` passed med 0 errors och känd WindowsBase/WebView2 warning
- publish/restart har tidigare lyckats och får göras efter lyckade runtime-ändringar; gör det inte för docs-only work

## Nästa rekommenderade arbete

1. Manuell UI-verifiering:
   - `visa terminal`
   - `vad stod i terminalen`
   - `avbryt`
   - `terminal preview: dotnet build`
2. Manuell UI-verifiering av pending file write/append/delete/undo.
3. Manuell UI-verifiering av Jarvis Översikt: klicka `Översikt`, `/översikt`, `/minne status`, `/obsidian status`.
4. Manuell UI-verifiering av Project Explorer active-file/active-folder highlight efter att fil öppnas.
5. Improve terminal transcript formatting.
6. Expand undo into named checkpoints/history.
7. Build `.jarvis/tasks` task workspace later.
8. Add worker delegation later; workers read/summarize/propose only.
9. Add local Ollama/Claude Code setup docs/scripts later.
10. Keep real 3D later, optional and off by default.

## Do not regress

- Do not add smart-open V8/V9 patch layers.
- Do not reintroduce old V3/V4/V5/V6/V7 duplicate methods.
- Do not send local commands to Ollama.
- Do not write files directly from natural language.
- Do not publish/restart Jarvis for docs-only changes.
- After runtime changes pass relevant tests and `dotnet build`, Codex may stop, publish and restart Jarvis-clean so the user can test immediately.
