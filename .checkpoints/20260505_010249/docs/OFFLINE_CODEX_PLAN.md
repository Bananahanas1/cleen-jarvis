# OFFLINE_CODEX_PLAN.md — Lokal Codex-liknande kodagent

Senast uppdaterad: 2026-05-04

## Slutmål

Jarvis ska i framtiden kunna fungera som en lokal/offline Codex-liknande kodagent.

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

## Viktig säkerhetsregel

Jarvis får inte få fri tillgång till hela datorn.

I början får Offline Codex-läget bara arbeta i:

F:\Jarvis-clean

Jarvis får inte ändra:

F:\New project
C:\
Downloads
Desktop
Documents
bilder
lösenord
API-nycklar
ultraPass-data

## Implementeringsordning

### Fas 1 — Säkra filverktyg

Kommandon:
- läs fil: docs/SESSION_LOG.md
- skriv fil: docs/test.md | text
- lägg till fil: docs/test.md | text

Regler:
- bara relativa sökvägar
- bara inom F:\Jarvis-clean
- blockera bin, obj, dist, .git och node_modules
- blockera farliga filtyper

### Fas 2 — Projekt-index

Jarvis ska kunna skapa en översikt:
- mappar
- viktiga filer
- dokumentation
- TODO-filer
- aktuellt läge

Exempelkommando:
- indexera projekt

### Fas 3 — Läs-förstå-föreslå

Jarvis ska kunna läsa en fil och föreslå ändring utan att skriva direkt.

Exempelkommando:
- föreslå ändring: README.md

### Fas 4 — Diff före ändring

Jarvis ska visa skillnaden före den skriver.

Flöde:
1. Jarvis föreslår ändring
2. Jarvis visar diff
3. Användaren skriver godkänn ändring
4. Jarvis skriver filen

### Fas 5 — Checkpoint/rollback

Innan ändringar ska Jarvis kunna skapa checkpoint.

Kommandon:
- skapa checkpoint
- lista checkpoints
- återställ senaste checkpoint

### Fas 6 — Build/test-runner

Jarvis ska kunna köra säkra kommandon:
- dotnet build
- dotnet publish
- node --check
- python -m py_compile

Men bara inom F:\Jarvis-clean.

### Fas 7 — Enkel agent-loop

Jarvis ska kunna:
1. förstå målet
2. välja ett säkert verktyg
3. köra verktyget
4. läsa resultatet
5. föreslå nästa steg

### Fas 8 — Kodagent med bättre modell

Snabb modell:
- qwen2.5-coder:1.5b

Kodmodell senare:
- qwen2.5-coder:7b

Jarvis ska kunna välja modell beroende på uppgift.

### Fas 9 — Offline dependency-cache

Jarvis ska kunna kontrollera:
- NuGet cache på F:
- Ollama-modeller på F:
- pip-cache på F:
- npm-cache på F:
- inga internet-timeouts

### Fas 10 — Full lokal Codex-liknande assistent

När allt ovan fungerar kan Jarvis börja hjälpa till på riktigt med:
- fixa buildfel
- uppdatera dokumentation
- skapa filer
- refaktorera små delar
- köra test
- förklara kodbasen
- planera nästa utvecklingssteg

## Vad som inte ska byggas nu

Inte än:
- full autonom agent
- ändra stora delar av kodbasen själv
- köra farliga terminalkommandon
- röra gamla F:\New project
- NeuroLinked
- 3D-dashboard
- ultraPass
- internetbaserade tools
- cloud/GitHub/Docker/Kubernetes

## Nästa praktiska steg

Börja med Fas 1:

Säkra filverktyg:
- läs fil
- skriv fil
- lägg till fil

Det är första riktiga steget mot Offline Codex-läge.

## Project Explorer + Active File Context

Jarvis ska senare få en egen fil-explorer i dashboarden, liknande VS Code Explorer.

Mål:
- visa projektfiler i F:\Jarvis-clean
- klicka på fil för att öppna/visa den
- Jarvis sparar vald fil som aktiv fil
- användaren kan skriva naturligt: förklara denna fil, föreslå ändring här, lägg till rubrik här
- Jarvis ska då veta vilken fil användaren menar utan att filnamnet måste skrivas igen

Säkerhetsregler:
- bara F:\Jarvis-clean i början
- inte F:\New project
- inte C:\
- inte Downloads/Desktop/Documents
- inga lösenord eller ultraPass-filer

Implementeringsidé:
1. C# skapar endpoint/tool som listar projektfiler
2. Dashboard visar filerna i en sidebar
3. Klick på fil skickar meddelande till C#
4. C# läser filen och sparar ActiveFilePath
5. Kommandon som 'denna fil', 'här', 'öppna filen', 'föreslå ändring här' använder ActiveFilePath

Detta byggs efter checkpoint/rollback och modellhantering.
