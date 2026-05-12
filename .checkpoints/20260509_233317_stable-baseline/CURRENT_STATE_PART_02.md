# CURRENT_STATE PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


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

Decision:
- Do not integrate either project directly into Jarvis-clean right now.
- Keep them as reference/inspiration only.
- Jarvis-clean must first finish CommandRouter V1, CommandValidator, and safe file write approval.

Reason:
Jarvis-clean currently has routing and command-safety technical debt. Adding external agent/workspace systems directly now would make the architecture harder to control and could create unsafe file/terminal behavior.

Ideas to reuse later:

From Octogent:
- task/workspace folders
- CONTEXT.md per task
- TODO.md per task
- NOTES.md per task
- agent workspace UI
- terminal/task status panels
- handoff notes between AI agents

From claude-coworker-model:
- worker model delegation
- cheap/local worker for large file reading
- worker summaries before main Jarvis reasoning
- separate "main brain" and "worker/helper" responsibilities

Current rule:
External systems can be cloned/read as references, but they should not be connected to Jarvis runtime until local command routing and write safety are stable.

## Future access plan: F drive roots

Jarvis should later browse approved folders on F:\ through Project Explorer. This must start as read-only. F:\New project is reference only and must never be changed. Writing outside F:\Jarvis-clean stays blocked until a permission system and pending approval flow exists.

## 2026-05-05 — Codex handoff and slash-command decision

Created docs/CODEX_HANDOFF.md and docs/CODEX_START_PROMPT.md. Important decision: Jarvis should support exact slash-commands plus natural language routing. Slash commands should go directly to CommandRouter and never to Ollama. Natural language may use LLM reasoning but must convert actions into safe validated intents before execution.

## 2026-05-05 — Current command-safety state

Dashboard smart-open now refuses router-only commands before it can treat them as file-open. This covers slash commands, pending approval commands, file write/append/propose/delete commands and terminal preview/confirm/cancel.

`skriv fil` and `lägg till fil` now create pending approval only for existing safe text files inside `F:\Jarvis-clean`. They do not create new files automatically. A future `/fil skapa` command is still needed for safe new-file creation.

Approved file write/append/delete creates a latest-file undo snapshot. The dashboard shows an `Ångra` button beside `Granska ändringar`; clicking it creates a pending undo preview and does not touch disk until approved. Undo V1 is one-level and file-only, not a global app undo system.

Latest verification:
- CommandRouterV1 tests passed.
- Dashboard routing, help text, file write safety, file delete safety, approval popup, change review, smart-open cleanup and undo safety tests passed.
- `dotnet build` passed.
- `dotnet publish F:\Jarvis-clean\app\JarvisClean.csproj -c Release -o F:\Jarvis-clean\dist --no-self-contained` passed.
- Known warning remains: WindowsBase/WebView2 version conflict warning.
- Jarvis was restarted from `Starta-Jarvis.vbs`.

Manual tests for the user:
- `skriv fil: docs/test-agent.md | TESTAR PENDING APPROVAL`
- approve/cancel with popup buttons
- `skriv fil: docs/test-safe-write.md | text`
- `radera fil: docs/test-agent.md`
- `Granska ändringar`, close `×`, and `Ångra`
- `/hjälp`, `/status`, `/minne viktiga`, `/fil öppna app/Program.cs`, `/fil läs docs/PROJECT_INDEX.md`

## 2026-05-05 — Terminal approval state

Terminal preview/confirm/cancel now uses the shared `PendingApprovalV1` popup flow.
