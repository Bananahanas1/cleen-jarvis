# CURRENT_STATE PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


1. Bli expert på `F:\Jarvis-clean`.
2. Bli expert på användarens andra kodprojekt.
3. Bli bredare datorassistent.
4. Lägg till desktop/browser/screen control senare och extra säkert.

Säkerhetslinjen är oförändrad:
- ingen fri skrivåtkomst till hela F-disken
- `F:\New project` är read-only reference
- andra F-drive roots ska vara read-only som default
- filskrivning, append, delete, terminalkörning, externa tools och framtida UI-automation ska gå via safety checks, pending preview och approval
- Jarvis ska verifiera efter action och rapportera vad som ändrades

3D/Visual Lab är kvar som framtida visuellt lager. Routing, approval, developer workspace och verifiering kommer först.

Dokumentationspass verifierat:
- `dotnet build` kördes i `F:\Jarvis-clean\app`.
- Resultat: build succeeded, 0 errors.
- Känd varning kvar: WindowsBase/WebView2 version conflict warning.
- Ingen publish/restart gjordes eftersom detta bara var dokumentation.

## 2026-05-06 — Jarvis Översikt, minne och Obsidian-status

Visual Lab-idén har styrts om till en praktisk `Jarvis Översikt`-panel.

Nytt i denna riktning:
- mittpanelen har knappen `Översikt`
- `/översikt` och `översikt` öppnar översiktspanelen lokalt
- `/minne status` visar lokal minnesstatus utan Ollama
- `/obsidian status` visar säker Obsidian-status utan att skriva till någon vault
- översikten visar aktiv fil, pending approval, senaste terminalstatus, memory-state, Obsidian-state och Jarvis kontroll-loop
- verifierat med Node/C# routing/UI-safety tests och `dotnet build`
- publicerat och omstartat; observerad process: `Jarvis.exe` PID 66812

## 2026-05-06 — Dark scrollbar polish

Dashboarden har nu global mörk scrollbar-styling för scrollbara ytor:
- Project Explorer
- filpanel/editor
- terminaloutput
- Jarvis Översikt
- chat
- autocomplete
- approval preview
- diff/review

Detta är en ren UI-polish och ändrar inte command routing eller approval-regler.

## 2026-05-07 — Färgkodade autocomplete-förslag

Suggestion-rader får olika färg per del:
- kommandoprefix (`/fil skapa`, `skriv fil:`, `öppna `) → vit (`#f4fbff`)
- mappar (slutar med `/`) → gul (`#ffd966`)
- filer → grön (`#80ff96`)

Implementation:
- Ny CSS `.suggestion-command`, `.suggestion-folder`, `.suggestion-file`.
- Ny helper `colorizeSuggestionText(suggestion)` som splittar via regex som matchar alla kända kommandoprefix.
- `renderSuggestions` lindar varje del i `<span class="suggestion-...">`.
- Bevarar befintlig `.suggestion-row.active` highlight-bg.

Test: `tests\suggestion-colors.test.js` — markers + 7 colorize-cases (slash-fil, naturligt språk, /hjälp, mapp vs fil).

## 2026-05-07 — TAB folder-suggestions för create + SPACE locks valt förslag

Nytt skapande-flöde:
1. Skriv `/fil skapa ` (eller `skapa fil: `) → autocomplete listar **mappar** (med `/` på slutet).
2. **TAB** cyclar genom mappar (filtreras live när du fortsätter skriva, t.ex. `/fil skapa do` → `docs/`).
3. **SPACE** låser valt förslag — input behåller `/fil skapa docs/`, suggestion-listan stängs, cursorn på slutet.
4. Skriv `nyfil.md = innehåll` fritt.
5. Enter → pending approval popup.

Implementation:
- `splitFileCommandV11` taggar varje pattern med `mode: "create" | "open"`. Nya patterns för `/fil skapa` och `skapa fil:`.
- `fileSuggestions` returnerar `allFolders`-baserade förslag med trailing `/` när `mode === "create"`. Stoppar visa förslag när `=` dyker upp (innehållsfasen).
- Ny SPACE-handler i input-keydown: om suggestion-listan är synlig OCH input matchar exakt `currentSuggestions[suggestionIndex]` → preventDefault, hideSuggestions, cursor till slutet.
- Hint-texten i suggestion-listan uppdaterad: `TAB = byt/fyll förslag • Space = lås valt förslag • Enter = skicka • Esc = stäng`.

Test: `tests\create-folder-suggestions.test.js` — 4 markers + splitFileCommandV11 mode-cases + fileSuggestions folder output (med och utan filter, samt `=`-stopp).

Verifiering: alla 19 node-tester gröna, C# router-tester gröna, `dotnet build` 0 errors.

Publish/restart: stoppade gamla Jarvis (PID 74224), publish lyckades, ny Jarvis igång som PID 86668 (SessionId 11).

## 2026-05-06 — Enklare separator: `=` istället för `|` i filkommandon

Skäl: `|` kräver `AltGr+<` på svensk kb och är besvärligt. Användaren valde `=`.

Ändringar:
- Ny helper `CommandRouterV1.SplitFileCommandArguments(raw, maxParts)` — väljer vilken separator (`=` eller `|`) som dyker upp först i raden. `|` behålls som fallback så befintliga muskelminne/docs/tester fortsätter funka.
- Alla 7 parse-platser använder helpern: `/fil skapa`, `skriv fil:`, `lägg till fil:`, `skapa fil:`, `föreslå rubrik:`, `föreslå ändring:`, `radera fil:` (path-cleanup).
- Hjälptext, `BuildHelp`, `ToolRegistryV1`-exempel och felmeddelanden visar nu `=` som förstaval.
- C# router-test utökat med 4 nya cases för helpern + `/fil skapa` med `=`.

Verifiering:
- `dotnet run --project tests\CommandRouterV1.Tests\CommandRouterV1.Tests.csproj` — alla tester gröna inkl. nya `=`-cases.
- Alla 17 node-tester gröna.
- `dotnet build` 0 errors, känd `MSB3277` warning kvar.

**Publish/restart slutförd** efter att användaren stängde gamla Jarvis manuellt. `dotnet publish` lyckades, ny Jarvis igång som PID 74224 (SessionId 11 — korrekt user-session). `=`-separatorn är aktiv i UI:t.

Användarexempel efter omstart:
- `/fil skapa docs/test-eq.md = hej från eq-separator`
- `skriv fil: docs/test-agent.md = TESTAR =-separator`
- `föreslå rubrik: docs/test-agent.md = Test Agent`

## 2026-05-06 — Översikt live-state: aktiv mapp + senaste filändring

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
