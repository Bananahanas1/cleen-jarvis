# CURRENT_STATE PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


Jarvis Översikt visar nu fler praktiska state-signaler enligt VISUAL_PANEL_PLAN "Nästa visuella steg #1":

- ny cell `Aktiv mapp` — parent-mapp för aktiv fil (eller `(projektrot)` om filen ligger i roten).
- ny cell `Senaste filändring` — kind-label + path från `latestFileChangeReviewV1` (skapad / skriven / utökad / raderad / återställd).
- ny helper `computeActiveFolderLabelV1(path)` med stöd för flernivåmappar (`a/b/c/file.txt` -> `a/b/c`).
- `jarvisShowFileChangeReviewV1` och stäng-knappen i review-baren triggar nu `renderVisualPanelV1` så Översikt uppdateras live.
- Inga nya action-paths; panelen visar bara state.

Verifiering:
- nytt regressiontest `tests\overview-livestate.test.js` passerar.
- alla 17 node-tester passerar (inkl. project-explorer-polish, visual-panel, change-review, dashboard-routing).
- `dotnet run --project tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerar.
- `dotnet build` 0 errors, känd `MSB3277` warning kvar.
- `dotnet publish ... -c Release -o dist --no-self-contained` lyckades.
- Jarvis omstartad. Observerad process: `Jarvis.exe` PID 90520.

## 2026-05-06 — Project Explorer tree polish

Project Explorer har nu en tydlig aktiv-fil-markering:

- `.tree-row.active-file` får orange vänsterkant och svag bakgrund för aktuell aktiv fil.
- `.tree-row.active-folder` får svag bakgrund för parent-mappar till aktiv fil.
- `window.jarvisSetActiveTreeFileV1(path)` är ny gemensam helper.
- `jarvisSetEditorFile` propagerar aktiv path till tree-state automatiskt.
- File-row click sätter aktiv path lokalt direkt (innan C# echo) så markeringen syns omedelbart.
- `jarvisSetTreeFolderV7` återapplicerar aktiv-fil-state efter rerender.
- Befintlig grön review-highlight (post-write) är oförändrad och kompletterande.

Verifiering:
- nytt regressiontest `tests\project-explorer-polish.test.js` passerar.
- alla befintliga node-tester (dashboard-routing, smart-open-cleanup, visual-panel, scrollbar, approval-popup, help-text, file-write/delete-safety, editor-save, undo, change-review-ui/csharp, terminal-approval, app-project-scope) passerar.
- `dotnet run --project tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` passerar.
- `dotnet build` 0 errors, känd `MSB3277` warning kvar.
- `dotnet publish ... -c Release -o dist --no-self-contained` lyckades.
- Jarvis omstartad via `Starta-Jarvis.vbs`. Observerad process: `Jarvis.exe` PID 90828.

## 2026-05-06 — Experimental AI additions reviewed

Hittade experimentella mappar under `app`:
- `app\JarvisCLI`
- `app\PocketBridge`

Bedömning:
- idéerna kan vara användbara senare som extern/pocket/mobile bridge-inspiration
- de är inte värda att fortsätta på direkt i huvudappen just nu
- de bypassar inte längre huvudbygget och ska inte kopplas in utan CommandRouter/Validator/ToolRegistry/PendingApproval

Åtgärd:
- huvudprojektet exkluderar dessa experimentella C#-mappar från compile-scope
- `dotnet build` är åter grönt med 0 errors
- `tests\app-project-scope.test.js` skyddar mot att nested experimentkällor råkar kompileras in igen
- senaste publicerade/omstartade Jarvis-process: `Jarvis.exe` PID 42368

Viktigt:
- detta startar Obsidian/minnes-riktningen utan att ge Jarvis fri vault-skrivning
- ingen bakgrundsagent kör konstant
- "tänker konstant" betyder just nu synlig kontroll-loop och uppdaterad state, inte okontrollerad auto-execution
- riktig 3D, Obsidian-sync och NeuroLink kommer senare som separata säkra paneler

## Det som fungerar

- Safe dashboard öppnar utan att frysa datorn.
- Appen heter Jarvis.
- C# WinForms/WebView2-fönster öppnar dashboarden.
- JavaScript → C# → JavaScript-bryggan fungerar.
- Calculator fungerar, till exempel:
  - 2+2
  - räkna 2+2
- Lokala verktyg fungerar:
  - status
  - lista filer
  - lista filer i app
  - lista filer i dashboard
- Ollama-chat fungerar via lokal modell:
  - qwen2.5-coder:1.5b
- Ollama hålls varm bäst genom:
  ollama run qwen2.5-coder:1.5b

## Viktiga säkerhetsregler

- Ändra inte F:\New project.
- F:\New project är bara referens.
- Skriv bara ny kod i F:\Jarvis-clean.
- Starta inte gamla NeuroLinked-dashboarden.
- Starta inte tung brain_state.
- Starta inte 3D/WebGL än.
- Lägg inte in Graphify, Obsidian eller ultraPass ännu.
- Inga lösenord, API-nycklar eller secrets i loggar/chat/minne.

## Avstängt just nu

- NeuroLinked
- 3D-dashboard
- WebGL
- Graphify auto-load
- Obsidian
- ultraPass
- gamla plugin-systemet
- internetverktyg

## Nästa rekommenderade steg

1. Lägg till enkel session-logg i F:\Jarvis-clean\docs.
2. Lägg till kommando: hjälp.
3. Lägg till kommando: öppna projektmapp.
4. Lägg till enklare agentläge endast för F:\Jarvis-clean.
5. Lägg till offline-probe för internetverktyg.
6. Lägg till NeuroLinked basic senare, utan tung brain_state.
7. Lägg till 3D sist och bara som valfritt läge.

## Testkommandon i Jarvis

Skriv i Jarvis-chatten:

`/status`
`/hjälp`
`/minne viktiga`
`/fil öppna app/Program.cs`
`visa terminal`
`vad stod i terminalen`
`avbryt`
`terminal preview: dotnet build`
`öppna tests/terminal-approval-safety.test.js`

Lokala kommandon ska fångas före Ollama. Terminalkörning och andra riskabla actions ska kräva pending preview och approval.

## Desktop shortcut

Skrivbordsgenvägen Starta Jarvis.lnk fungerar och startar F:\Jarvis-clean\Starta-Jarvis.vbs.

## Local markdown memory

Jarvis sparar nu lokalt minne i F:\Jarvis-clean\data\memory.md.

Fungerande minneskommandon:
- kom ihåg: text
- visa minne
- minnesstatus
- öppna minne

## Memory injected into Ollama

Jarvis skickar nu med senaste delen av data\memory.md till Ollama i systemprompten. Det gör att Jarvis kan använda lokalt minne i vanliga frågor, till exempel favoritfärg.

## Future ideas documented

Jarvis bredare framtidsidéer är sparade i docs\FUTURE_IDEAS.md. De ska inte implementeras ännu.

## Smart Memory commands

Jarvis stödjer nu smartare lokala minneskommandon:
- smart minne: text
- viktigt minne: text
- projektminne: text

Dessa fångas lokalt i C# före Ollama och sparas i data\memory.md med Type, Importance och Tags.

## Latest Jarvis progress

Jarvis har nu förbättrat Smart Memory med:
- felstavningstolerans för kommandon
- pil upp/down historik i chat-input
- visa viktiga minnen
- visa projektminnen
- sammanfatta minne
- glöm minne / bekräfta glöm / arkivering till memory_archive.md
- arkivsökning planerad/testas vidare

## Diskvakt

Jarvis har nu Diskvakt-kommandon:
- diskstatus
- cachestatus
- rensa cache preview
- rensa cache bekräfta
- kontrollera cache

Cache-rensning är begränsad till säkra cache/temp-mappar och ska inte röra dokument, bilder, Downloads, F:\New project eller F:\Jarvis-clean.




## Offline Codex goal added

Planen för lokal Codex-liknande kodagent är sparad i docs\OFFLINE_CODEX_PLAN.md. Nästa praktiska steg är säkra filverktyg: läsa, skriva och lägga till filer inom F:\Jarvis-clean.

## Offline Codex Fas 1 — Säkra filverktyg

Jarvis kan nu lokalt och säkert hantera projektfiler inom F:\Jarvis-clean:
- läs fil: docs/fil.md
- skriv fil: docs/fil.md | text
- lägg till fil: docs/fil.md | text

Filverktygen fångas lokalt i C# före Ollama och går inte till AI-svar. Fulla sökvägar, .., bin, obj, dist, .git, node_modules och osäkra filtyper blockeras.

## Command help and project file listing

Jarvis har nu lokala hjälpkommandon:
- kommandohjälp
- lista md filer
- lista projektfiler

Dessa hjälper användaren att förstå vad som ska skrivas i Jarvis-fönstret och vad som ska skrivas i PowerShell.

## Offline Codex Fas 3 — Safe pending changes

Jarvis kan nu skapa och godkänna säkra ändringsförslag:
- föreslå rubrik: docs/fil.md | Rubrik
- läs fil: docs/PENDING_CHANGE.md
- godkänn ändring
- avbryt ändring

Godkända heading-ändringar skapar backup och arkiverar PENDING_CHANGE.md i docs\change_archive.

## Offline Codex Fas 3 verified

Jarvis har verifierats med:
- docs/test-agent.md börjar med # Test Agent
- docs/PENDING_CHANGE.md försvinner efter godkännande
- docs/change_archive finns och innehåller arkiverade förslag/backups

## Checkpoint / rollback

Jarvis har nu checkpoint-kommandon:
- skapa checkpoint
- lista checkpoints
- återställ senaste checkpoint

Testat: checkpoint kan skapas och listas. Återställning ska bara användas när något gått fel.

## Model management

Jarvis har nu lokal modellhantering:
- visa modell
- lista modeller
- byt modell: modellnamn

Aktiv modell sparas i F:\Jarvis-clean\config\model.txt. Testat med qwen2.5-coder:7b.

## InternetProbe planned

Jarvis ska få lokala internetstatus-kommandon så frågor som 'har jag internet' inte skickas till Ollama och inte ger gissade svar.

## File type permissions

Jarvis kan nu läsa fler projektfiltyper, men skrivning är hårdbegränsad.

Read-only exempel:
- .sln
- .csproj
- .xml
- .yml/.yaml
- .config
- .log

Writable filtyper:
- .md
- .txt
- .json
- .cs
- .html
- .css
- .js
- .ps1

Testat: skrivning till app/test.sln blockeras, läsning av app/JarvisClean.csproj fungerar.

## 2026-05-05  Aktuellt läge: Jarvis-clean UI och kommandosäkerhet

Jarvis-clean har nu en 3-panelslayout:

- vänster: Project Explorer
- mitten: filpanel/kodvisare
- höger: Jarvis Chat

Project Explorer är på väg att bli ett träd liknande VS Code, där mappar ska kunna expandera med pil:
- ` app`
- ` app`
- filer visas under mappen utan att projektroten försvinner

Filpanelen öppnar filer tyst utan att dumpa hela filen i chatten. Jarvis ska veta vilken fil som är aktiv.

Viktigt arkitekturbeslut:
- Lokala kommandon ska alltid fångas innan Ollama får meddelandet.
- Ollama ska användas för resonemang, förklaringar och kodförslag.
- Ollama ska inte själv gissa hur lokala kommandon som `öppna readme.md`, `sök minne:` eller `skriv fil:` ska utföras.

Bekräftat fungerande:
- 3-panelslayout fungerar.
- Filer kan öppnas i mittenpanelen.
- `öppna program.cs` fungerade.
- Autocomplete/TAB-förslag har börjat fungera.
- Tomma minnes-/sökkommandon blockeras nu, till exempel:
  - `viktigt minne:`
  - `sök minne:`

Känd teknisk skuld:
- Fortsätt hålla smart-open-koden central; lägg inte till nya V8/V9-patchar.
- Bevaka att gamla V4/V5/V6/V7-smart-open-referenser inte kommer tillbaka.
- Naturligt språk behöver fortfarande mappa säkrare till validerade intents.
- File panel edit mode behöver pending save approval innan skrivning.

Nästa stabila mål:
1. Fortsätta flytta beteende till CommandRouter V1 / CommandValidator V1.
2. Samla riskabla actions bakom PendingApproval V1.
3. Göra alla kommandon vattentäta:
   - saknad text blockeras
   - saknad fil blockeras
   - fel sökväg blockeras
   - farlig filtyp blockeras
   - skrivning kräver preview/pending
4. Göra Project Explorer till riktigt expanderbart träd.
5. Bygga edit-läge med säker sparning.
6. Förbättra Terminal-panelens transcript view.

## 2026-05-05  Reference projects decision

Two external projects were reviewed as inspiration:

- claude-coworker-model
- octogent
