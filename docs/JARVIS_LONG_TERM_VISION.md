# JARVIS_LONG_TERM_VISION.md

Senast uppdaterad: 2026-05-05

## Syfte

Jarvis ska inte bara vara en chatbot. Jarvis ska bli en lokal dator- och developer-agent som kan förstå projekt, hjälpa till med kodarbete, använda lokala verktyg och agera stegvis med tydliga säkerhetsgränser.

Den viktiga riktningen är:

1. Jarvis ska först bli expert på `F:\Jarvis-clean`.
2. Sedan ska Jarvis bli expert på användarens andra kodprojekt.
3. Först därefter ska Jarvis växa till en bredare datorassistent.
4. Desktop-, browser- och skärmkontroll ska komma senare och vara extra säkert.
5. Jarvis får aldrig bli ett okontrollerat "gör vad som helst på min dator"-verktyg.

## Grundloop

Jarvis ska arbeta genom en säker kontroll-loop:

```text
Observe
-> Think
-> Plan
-> Ask if risky
-> Act
-> Verify
-> Report
-> Remember
```

Loopens betydelse:

- Observe: samla lokal kontext från projektet, UI, aktiv fil, terminal och minne.
- Think: förstå vad användaren vill göra och om det är chat, läsning, ändring eller riskabel handling.
- Plan: dela upp arbetet i små steg innan något ändras.
- Ask if risky: begär pending preview och användarens godkännande för skrivning, radering, terminalkörning, externa verktyg och UI-automation.
- Act: utför bara den validerade åtgärden.
- Verify: bygg, testa, läsa tillbaka filen eller kontrollera UI-resultatet.
- Report: berätta vad som hände, vad som ändrades och hur användaren testar.
- Remember: föreslå minne/logg efter lyckad verifiering, inte skriva blint.

## Capability Layers

### 1. Eyes / Observation

Jarvis ska stegvis kunna förstå:

- aktiv fil
- vald mapp
- Project Explorer-state
- öppna paneler
- Terminal-panelens output
- senaste terminaltranskript
- screenshots
- synliga appfönster
- UI-state

Nuvarande nivå:

- aktiv fil och Project Explorer-state finns delvis
- Terminal-panel V1 finns
- senaste terminaltranskript finns i runtime-minne
- screenshots och synliga appfönster är framtida arbete

### 2. Hands / Tools

Jarvis ska stegvis kunna:

- läsa filer
- öppna filer
- söka filer
- föreslå filändringar
- skriva filer endast efter pending preview och godkännande
- lägga till i filer endast efter pending preview och godkännande
- radera filer endast efter pending preview och godkännande
- köra terminalkommandon endast efter preview och godkännande
- öppna program
- styra browser senare
- klicka och skriva i UI senare
- kopiera och klistra in senare
- hantera projekttasks

Regel:

Workers, LLM:er och externa tools får aldrig skriva direkt. De får läsa, sammanfatta och föreslå. Main Jarvis måste validera, skapa pending preview och kräva användargodkännande.

### 3. Brain / Routing

Jarvis ska använda:

- `CommandRouterV1`
- `CommandValidatorV1`
- `ToolRegistryV1`
- `PendingApprovalV1`
- slash-kommandon för exakt säker kontroll
- naturligt språk för svenska instruktioner
- LLM endast för resonemang och språkförståelse
- lokala C# tools för faktisk execution

Viktig routingregel:

1. Lokala kommandon fångas före Ollama.
2. Slash-kommandon går direkt till router och validator.
3. Naturligt språk översätts till ett säkert intent.
4. Riskabla intents skapar pending preview.
5. Bara normal chat och resonemang går till Ollama.

### 4. Memory

Jarvis ska ha flera typer av minne:

- project memory
- user preference memory
- task memory
- decision memory
- safety memory

Memory ska inte auto-skrivas blint.

Framtida säkert minnesflöde:

1. Jarvis läser projektdokumentation.
2. Jarvis föreslår ett project memory.
3. Användaren granskar.
4. Användaren godkänner.
5. Jarvis sparar minnet.

Minnet ska kunna hjälpa Jarvis att komma ihåg beslut, projektregler, användarpreferenser och riskområden. Secrets, lösenord och tokens får inte sparas i minne.

### 5. Task Workspaces

Jarvis ska senare kunna skapa task-mappar inspirerade av Octogent:

```text
.jarvis/tasks/<task-id>/CONTEXT.md
.jarvis/tasks/<task-id>/TODO.md
.jarvis/tasks/<task-id>/NOTES.md
.jarvis/tasks/<task-id>/CHANGES.md
.jarvis/tasks/<task-id>/RESULT.md
.jarvis/tasks/<task-id>/SESSION_LOG.md
```

Task workspace ska göra det möjligt att:

- skapa task
- öppna task
- lista tasks
- lägga till todo
- markera todo klar
- sammanfatta task
- se ändringar
- handoff till nästa AI/Codex-session

Task workspaces ska börja inom `F:\Jarvis-clean`. Multi-root och andra projekt kommer senare och ska vara read-only som default.

### 6. Worker Agents

Senare kan Jarvis använda worker-modeller eller worker-agenter inspirerade av Claude Coworker Model.

Workers kan hjälpa med:

- läsa många filer
- sammanfatta mappar
- hitta relevant kod
- drafta dokumentation
- drafta kodändringar

Worker-regler:

- workers får aldrig skriva direkt
- workers får aldrig köra terminal direkt
- workers får aldrig röra `F:\New project`
- workers får bara läsa, sammanfatta och föreslå
- main Jarvis validerar alla förslag
- main Jarvis kräver pending approval för skrivning, radering och terminal

### 7. Control Modes

Jarvis ska ha tydliga framtida kontrollägen.

#### Chat Mode

- prata och förklara
- inga filändringar
- inga terminalkörningar
- passar för frågor, planering och resonemang

#### Read Mode

- läsa projektfiler
- läsa terminaloutput
- läsa aktiv fil och projektstate
- ingen skrivning

#### Assist Mode

- föreslå ändringar
- skapa diff/preview
- användaren godkänner innan skrivning

#### Agent Mode

- utföra flerstegsuppgifter
- planera, agera och verifiera
- riskabla handlingar kräver approval
- rapporterar varje större steg

#### Desktop Mode

- senare läge för app-, browser- och skärmkontroll
- kräver extra permission
- klick/skriv/automation kräver säkerhetsregler
- aldrig okontrollerat

## Safety Rules

Grundregler:

- Jarvis får aldrig få fri skrivåtkomst till hela F-disken.
- `F:\New project` är read-only reference.
- Andra F-drive roots ska vara read-only som default.
- Skrivning utanför `F:\Jarvis-clean` är blockerad tills ett explicit permission-system finns.
- File write, append, delete, terminal run, external tool actions och UI automation måste kräva safety checks.
- Riskabla actions ska skapa pending preview.
- Användaren ska godkänna innan riskabla actions utförs.
- Jarvis ska verifiera efter action.
- Jarvis ska rapportera vad som ändrades.
- Jarvis ska uppdatera loggar/minne efter lyckat arbete, men inte blint.
- Desktop/browser-control måste kräva explicit approval och komma mycket senare.

## Roadmap

### Phase 1: Safe Core

Mål:

- CommandRouter
- CommandValidator
- ToolRegistry
- PendingApproval
- slash-kommandon
- dashboard routing safety

Status:

- påbörjat och delvis stabilt
- terminal preview/approval finns
- file write/append/delete pending approval finns
- dashboard smart-open har fått flera skydd

### Phase 2: Developer Workspace

Mål:

- Project Explorer tree
- file panel edit mode
- terminal panel
- build/test/publish tools
- undo/checkpoints
- clean old smart-open V4/V5/V6/V7 if any stale references return

Status:

- Terminal-panel V1 finns
- one-step undo finns för senaste filoperation
- checkpoint commands finns men behöver mer integration
- edit mode och pending save återstår

### Phase 3: Smart Natural Language

Mål:

- svensk natural language till safe intent
- `öppna programfilen`
- `leta efter buildfelet`
- `fixa detta men fråga först`
- naturligt språk måste mappa till validerade intents

Regel:

Naturligt språk får vara mjukt och mänskligt, men execution måste vara strikt och validerad.

### Phase 4: Task Workspace

Mål:

- `.jarvis/tasks`
- `CONTEXT.md`
- `TODO.md`
- `NOTES.md`
- `CHANGES.md`
- `RESULT.md`
- handoff
- task status

### Phase 5: Worker Agents

Mål:

- worker-read
- worker-summarize
- worker-find-files
- worker-draft-change
- no direct writing by workers

### Phase 6: Desktop Control

Mål:

- open programs
- browser automation
- screenshot understanding
- click/type with approval
- never uncontrolled

Detta ska inte byggas innan developer workspace, routing, pending approval, verification och safety logs är stabila.

### Phase 7: Voice Jarvis

Mål:

- voice input
- voice output
- hands-free project assistant

Voice ska vara ett säkert UI-lager ovanpå samma router/validator/tools. Voice får inte kringgå approvals.

## Latest Stable Progress

Senaste stabila läge:

- Terminal-panel V1 finns.
- Terminaloutput ska gå till Terminal-panelen i stället för att spamma chatten.
- Chatten ska visa kortare terminalsammanfattningar.
- Terminal preview/approval finns via `PendingApprovalV1`.
- Approval popup har säkrare beteende: `Avbryt` får fokus och `Godkänn` låses kort.
- `visa terminal` och `vad stod i terminalen` routing har fixats.
- Generic `avbryt` har gjorts context-aware.
- Känd build-varning: WindowsBase/WebView2, men build har 0 errors.

## Not Yet Finished

Aktuella områden som inte är klara:

- manuellt verifiera terminal routing i UI
- verifiera pending file write approval flow i UI
- finish Project Explorer tree polish
- finish file panel edit mode with pending save
- clean old V4/V5/V6/V7 smart-open references later if any return
- improve terminal transcript formatting
- build task workspace later
- build worker delegation later
- local Ollama/Claude Code setup docs/scripts later
- Jarvis Översikt finns som praktisk lätt panel; real 3D är inte current priority och ska komma senare

## Visual / 3D Position

Visuellt arbete är viktigt, men real 3D ska inte byggas före säker kärna och developer workspace är stabilt.

Rätt första visuella steg nu:

- `Jarvis Översikt` som separat panel bredvid Workspace Panel
- visa project status, approvals, terminal/build-state, minne, Obsidian-status och task-state
- ingen tung WebGL/Three.js som default
- ingen simulation-loop vid start
- lätt att stänga av

3D ska vara ett visuellt lager ovanpå säkra state-signaler, inte ett nytt kontrollsystem.

Se även `docs\VISUAL_PANEL_PLAN.md`.
