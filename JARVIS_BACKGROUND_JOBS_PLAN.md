# JARVIS_BACKGROUND_JOBS_PLAN.md

Senast uppdaterad: 2026-05-12

## Mål

Bygg **Jarvis Project Index + Background Jobs MVP** före Kartan och andra
stora future-features.

Jarvis ska svara snabbt direkt och köra lång analys i bakgrunden.

## 1. Snabbt första svar

När användaren skriver till exempel:

- "läs filerna"
- "analysera projektet"
- "läs hela repo"
- "gå igenom allt"
- "förstå projektet"
- "skapa audit"

ska Jarvis svara direkt:

> Jag börjar läsa och indexera projektet i bakgrunden. Du kan fortsätta skriva under tiden.

Sedan skapas ett background job. Chatten får inte blockeras.

## 2. Background jobs

MVP ska ha:

- job queue
- background worker
- progress/status
- pausa
- fortsätta
- avbryta
- logg
- sparat resultat
- icke-blockerande chat

Föreslagen lagring:

- `data/jobs/jobs.jsonl` - jobbhistorik
- `data/jobs/<job-id>/status.json` - status och progress
- `data/jobs/<job-id>/log.md` - läsbar logg
- `data/jobs/<job-id>/result.md` - rapport/resultat

All terminalkörning ska fortfarande gå via pending approval. Själva indexjobbet
ska bara läsa projektfiler inom tillåten scope.

## 3. Project index

Jarvis ska skapa ett lokalt projektindex:

- projektstruktur
- filnamn
- filtyper
- senaste ändringstid
- filhash
- sammanfattning per fil
- sammanfattning per mapp
- viktiga funktioner/klasser
- TODOs
- imports/beroenden
- relationer mellan filer
- embeddings/sökindex
- relevanta kodutdrag
- projektets viktigaste moduler

Föreslagen lagring:

- `data/project-index/index.json`
- `data/project-index/files/<safe-id>.json`
- `data/project-index/folders/<safe-id>.json`
- `data/project-index/search.db` eller enkel JSONL för MVP

## 4. Incremental scan

Första gången:

- full scan inom `F:\Jarvis-clean`
- exkludera `.git`, `bin`, `obj`, `dist`, `node_modules`, stora generated filer
- använd filstorleksgränser

Efter det:

- läs bara filer som ändrats
- jämför modified time och filhash
- återanvänd summaries
- uppdatera mappar stegvis
- markera borttagna filer som deleted i index

## 5. RAG / smart context

Jarvis ska inte skicka hela projektet till Ollama.

Vid vanlig fråga:

- sök i projektindex
- hämta relevanta filer/delar
- skicka bara relevant context
- svara snabbt

Vid deep analysis:

- skapa background job
- gå igenom större delar stegvis
- spara rapport i job/result och index
- uppdatera memory om användaren godkänner eller regeln tillåter det

## 6. Map-reduce analysis

För stora projekt:

1. Läs filer i chunks.
2. Sammanfatta varje chunk.
3. Sammanfatta varje fil.
4. Sammanfatta varje mapp.
5. Sammanfatta hela projektet.
6. Spara resultat i projektindex och memory.
7. Skapa audit/rapport.

Chunk-summaries ska kunna återanvändas om filhash inte ändrats.

## 7. Modes

### Fast mode

- snabb modell
- minimal context
- kort timeout
- enkla svar

### Normal mode

- relevant context från index
- normal timeout
- vanlig hjälp

### Deep mode

- större analys
- längre timeout
- rapport
- helst background job

### Background mode

- lång uppgift
- blockerar inte chatten
- visar progress
- sparar resultat

## 8. Ollama timeout och warmup

Jarvis ska:

- ha kort timeout för vanlig chat
- ha längre timeout för deep/background tasks
- visa "jag jobbar fortfarande" i stället för tyst timeout
- hålla vald modell varm
- undvika onödiga modellbyten
- använda liten snabb modell för enkla svar
- använda större modell för djup analys
- visa vilken modell som används för varje jobb

## MVP-slice

Bygg i små steg:

1. Job-modell + statuskommandon: `/jobb`, `/jobb status`, `/jobb avbryt`.
2. Read-only background scan av `F:\Jarvis-clean`.
3. Spara filmetadata och hashes.
4. Incremental scan.
5. Enkel text-sökning i index.
6. Fil- och mappsummary via Ollama med background progress.
7. RAG-context för vanlig chat.
8. Audit-jobb som sparar rapport.

## Säkerhetsgränser

- Indexjobbet får bara läsa godkända projektfiler.
- Det får inte skriva kodfiler.
- Det får skriva till `data/project-index` och `data/jobs`.
- Terminalkommandon kräver fortfarande PendingApproval.
- Ingen fri skrivåtkomst till F-disken.
- `F:\New project` är read-only reference.
