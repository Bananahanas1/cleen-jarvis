# JARVIS UPDATE TASK

Du är Claude Code och ska uppdatera Jarvis-megaprompten + planfilerna i `cleen-jarvis`.

VIKTIGT:
Du ska inte bara sammanfatta.
Du ska faktiskt uppdatera filerna i projektet.

---

# HUVUDREGEL

`cleen-jarvis` är huvudprodukten.

`f-drive-projects` är endast referens, backup och inspirationskälla från F-disken.

`F:\New project` är read-only reference och får inte ändras.

När du utvecklar Jarvis ska du alltid prioritera `cleen-jarvis`.

---

# MARKDOWN-REGEL

Alla Markdown-filer måste följa regeln:

- Max 14 000 tecken per `.md`-fil.
- Helst 8 000–12 000 tecken.
- Om en fil blir för lång, dela upp i `PART_01`, `PART_02` osv.
- Ingen information får försvinna.
- Skapa indexfiler som länkar ihop allt.
- `JARVIS_MEGA_MASTER_PROMPT.md` ska inte vara en gigantisk fil, utan en översikt/index.

---

# NY HUVUDPRIORITET

Nästa riktiga build ska vara:

**Jarvis Project Index + Background Jobs MVP**

Detta ska prioriteras före Kartan, liveflyg, livebåtar, avancerad 3D Earth, weather animations och andra stora future-features.

Anledning:
Jarvis är redan en fungerande huvudprodukt med mycket grundfunktioner. Det största problemet nu är att Jarvis kan bli seg när den försöker läsa igenom allt. Lösningen är att Jarvis ska svara snabbt direkt och köra djup analys i bakgrunden.

---

# PRODUKTSTATUS SOM SKA DOKUMENTERAS

Dokumentera tydligt i `README.md`, `CURRENT_STATE.md`, `JARVIS_MASTER_PLAN.md` och relevanta indexfiler:

- `cleen-jarvis` är huvudprodukten.
- `f-drive-projects` är referens/backup/inspiration.
- `F:\New project` är read-only reference.
- GitHub är källan ChatGPT/AI-agenter kan läsa från.
- Efter varje lyckad build/test ska `cleen-jarvis` pushas till GitHub.
- Om ändringar bara finns lokalt men inte är pushade kan externa AI-agenter inte läsa dem.

---

# DET SOM REDAN FINNS I CLEEN-JARVIS

Skriv in i `CURRENT_PROJECT_AUDIT.md` och `CURRENT_STATE.md` att `cleen-jarvis` redan har mycket grund:

- C# WinForms/WebView2 dashboard
- Project Explorer
- filpanel/kodvisare
- terminalpanel
- Jarvis-chat med lokal Ollama
- lokalt markdown-minne
- CommandRouter V1
- CommandValidator V1
- ToolRegistry V1
- PendingApproval V1
- safe file write/delete/undo-loop
- approval popup
- review/diff UI
- ModelRouter
- ConversationHistory
- WebSearcher via browser
- SafeAppLauncher
- BuilderMode
- NaturalEditTool
- desktop-control via pending approval
- vault/AI-kontext
- brain-vy
- tests
- säkerhetsregler

Slutsats:
Det som saknas är inte fler stora idéer.
Det som saknas är bättre produktordning, bakgrundsjobb, projektindex, RAG och stabil build/test/push-loop.

---

# JARVIS PROJECT INDEX + BACKGROUND JOBS MVP

Skapa/uppdatera `JARVIS_BACKGROUND_JOBS_PLAN.md`.

Den ska beskriva MVP för:

## 1. Snabbt första svar

När användaren skriver:

- “läs filerna”
- “analysera projektet”
- “läs hela repo”
- “gå igenom allt”
- “förstå projektet”
- “skapa audit”

ska Jarvis svara direkt:

> Jag börjar läsa och indexera projektet i bakgrunden. Du kan fortsätta skriva under tiden.

## 2. Background jobs

Jarvis ska kunna starta lång analys som bakgrundsjobb.

Krav:

- job queue
- background worker
- progress/status
- kunna pausa
- kunna fortsätta
- kunna avbryta
- logga vad som händer
- spara resultat
- inte blockera chatten

## 3. Project index

Jarvis ska skapa ett lokalt projektindex.

Indexet ska spara:

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

## 4. Incremental scan

Första gången:

- full scan

Efter det:

- läs bara filer som ändrats
- använd filhash/modified time
- återanvänd sparade summaries
- uppdatera index stegvis

## 5. RAG / smart context

Jarvis ska inte skicka hela projektet till Ollama varje gång.

Vid vanliga frågor:

- sök i projektindex
- hämta relevanta filer/delar
- skicka bara relevant context till modellen
- svara snabbare

Vid deep analysis:

- gå igenom större delar stegvis
- skapa rapport
- spara resultat i index och memory

## 6. Map-reduce analysis

För stora projekt:

1. Läs filer i chunks.
2. Sammanfatta varje chunk.
3. Sammanfatta varje fil.
4. Sammanfatta varje mapp.
5. Sammanfatta hela projektet.
6. Spara resultat i projektindex och memory.
7. Skapa audit/rapport.

## 7. Modes

Jarvis ska ha:

### Fast mode

- snabb modell
- minimal context
- kort timeout
- för enkla svar

### Normal mode

- relevant context från index
- normal timeout
- för vanlig hjälp

### Deep mode

- större analys
- längre timeout
- rapport
- körs helst som bakgrundsjobb

### Background mode

- lång uppgift
- blockerar inte chatten
- visar progress
- sparar resultat

## 8. Ollama timeout/model warmup

Jarvis ska:

- ha kort timeout för vanlig chat
- ha längre timeout för deep/background tasks
- visa “jag jobbar fortfarande” istället för att bara timeouta
- hålla vald modell varm
- inte byta modell i onödan
- använda liten snabb modell för enkla svar
- använda större modell för djup analys
- visa vilken modell som används för varje jobb

---

# AI-VÄNLIGA PLANFILER

Skapa/uppdatera dessa indexfiler:

- `PLANNING_INDEX.md`
- `JARVIS_CORE_INDEX.md`
- `KARTAN_INDEX.md`
- `NEXT_AI_AGENT_TODO.md`

Regler:

- varje fil under 14 000 tecken
- `JARVIS_MEGA_MASTER_PROMPT.md` ska vara översikt/index
- detaljer ska ligga i mindre planfiler
- om något blir för långt, skapa `PART_01`, `PART_02` osv
- indexfiler ska länka till alla delar
- ingen viktig info får försvinna

---

# STÄDA PRODUKTROLLEN

Uppdatera dokumentationen så denna regel finns tydligt:

## cleen-jarvis

- huvudprodukt
- allt viktigt ska byggas här
- ska hållas stabilt
- ska buildas/testas
- ska pushas efter lyckade ändringar

## f-drive-projects

- referens
- backup
- inspirationskälla
- kan användas för att hämta idéer/kodmönster
- ska inte behandlas som huvudprodukt

## F:\New project

- read-only reference
- får inte ändras
- får bara läsas/inspirera

---

# GITHUB-SYNC REGEL

Efter varje större ändring i `cleen-jarvis` ska Claude Code/Jarvis:

1. Kontrollera `git status`.
2. Köra relevant build-kommando om projektet har ett.
3. Köra relevanta tester om projektet har tester.
4. Om build lyckas och tester passerar:
   - `git add .`
   - `git commit -m "kort tydligt meddelande"`
   - `git push`
5. Om tester saknas:
   - dokumentera tydligt att tester saknas.
6. Om build eller tester failar:
   - kalla inte arbetet klart
   - pusha inte som färdig fungerande version
   - dokumentera vilket kommando som kördes
   - dokumentera vad som failade
   - dokumentera trolig orsak
   - dokumentera exakt nästa steg för att fixa

Säkerhetskrav:

- pusha aldrig `.env`
- pusha aldrig tokens
- pusha aldrig lösenord
- pusha aldrig API-nycklar
- respektera `.gitignore`
- håll alla Markdown-filer under 14 000 tecken
- om en Markdown-fil blir för lång ska den delas upp i PART-filer

Syfte:
GitHub ska alltid ha senaste fungerande versionen av `cleen-jarvis`, så ChatGPT eller nästa AI-agent kan läsa projektet direkt från GitHub utan tillgång till datorn.

---

# KARTAN SKA INTE VARA FÖRSTA BUILD

Kartan är fortfarande en viktig framtida feature, men den ska inte prioriteras före background jobs, project index och RAG.

Kartan ska dokumenteras så här:

## MVP

- egen sida “Kartan”
- CesiumJS eller liknande 3D-glob research
- enkel 3D-glob
- fly-to-city
- markörer
- enkel provider-arkitektur
- enkel UI-plan
- mini-chat i hörn som inte täcker kartan

## Later

- map scenes
- kart-rapporter
- mätverktyg
- routing
- offline packs
- places/POI
- score 0–100
- top 5 platsrekommendationer

## Premium/API-dependent

- Google Photorealistic 3D Tiles
- Google Places
- live flyg
- live båtar
- avancerade väderlager
- global företagsdata

## Research-needed

- offline-kartformat
- MBTiles/PMTiles/3D Tiles pipeline
- offline geocoding
- offline routing
- OSM building extraction
- lagringsstorlek för Sverige/Skåne
- exakt GPU/RAM-budget för 60 FPS
- licenser för externa kart- och platsdatakällor

Viktigt:
Bygg inte full Google Earth direkt.
Bygg först stabil Jarvis-kärna.

---

# VALIDERING

När uppdateringen är klar ska du kontrollera:

- `JARVIS_MEGA_MASTER_PROMPT.md` är under 14 000 tecken.
- Alla MD-filer är under 14 000 tecken.
- För långa filer är uppdelade i PART-filer.
- `PLANNING_INDEX.md` finns.
- `JARVIS_CORE_INDEX.md` finns.
- `KARTAN_INDEX.md` finns.
- `NEXT_AI_AGENT_TODO.md` finns.
- `JARVIS_BACKGROUND_JOBS_PLAN.md` finns och prioriterar Project Index + Background Jobs MVP.
- `README.md` och `CURRENT_STATE.md` förklarar produktrollen:
  - `cleen-jarvis` = huvudprodukt
  - `f-drive-projects` = referens/backup/inspiration
  - `F:\New project` = read-only reference
- GitHub-sync-regeln finns.
- Kartan är dokumenterad som MVP/Later/Premium/Research, inte som första huvudbuild.
- Om kod ändras: kör build/test.
- Om build/test lyckas: commit och push.
- Om build/test failar: dokumentera fel och nästa steg.

---

# FÖRSTA UPPGIFTEN NU

Utför detta nu:

1. Läs projektstrukturen i `cleen-jarvis`.
2. Uppdatera `JARVIS_MEGA_MASTER_PROMPT.md` eller skapa den om den saknas.
3. Skapa/uppdatera `PLANNING_INDEX.md`.
4. Skapa/uppdatera `JARVIS_CORE_INDEX.md`.
5. Skapa/uppdatera `KARTAN_INDEX.md`.
6. Skapa/uppdatera `JARVIS_BACKGROUND_JOBS_PLAN.md`.
7. Skapa/uppdatera `NEXT_AI_AGENT_TODO.md`.
8. Uppdatera `README.md`, `CURRENT_STATE.md`, `JARVIS_MASTER_PLAN.md` om de finns.
9. Se till att allt är under 14 000 tecken per MD-fil.
10. Dela upp för långa filer i PART-filer.
11. Gör inga stora riskabla kodändringar.
12. Om du bara ändrar dokumentation, dokumentera det tydligt.
13. Kör relevant snabb validering om möjligt.
14. Om allt är okej: commit och push till `cleen-jarvis`.

Kom ihåg:

- Sammanfatta inte bara.
- Uppdatera filerna.
- Behåll all viktig information.
- MVP först: Project Index + Background Jobs.