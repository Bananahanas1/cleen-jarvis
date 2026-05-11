# CURRENT_STATE.md — Jarvis

Senast uppdaterad: 2026-05-04

## Status

Jarvis har nu en stabil första grundversion i:

F:\Jarvis-clean

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

status
lista filer
lista filer i app
2+2
test csharp
hej

Alla dessa ska fungera utan att datorn fryser.

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
