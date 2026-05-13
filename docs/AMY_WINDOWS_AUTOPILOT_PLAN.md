# AMY Windows Autopilot Plan

## Beslut

Amy-idén får byggas in i Jarvis-clean, men Windows-native och enligt Jarvis
säkerhetsmodell.

Det här är inte en separat Amy-app. Det är en framtida Jarvis-modul som ger
Jarvis samma typ av kraft:

- röst in och röst ut
- tasks, memory och notes
- browser-agent
- desktop-agent
- brand DNA, ads och landing pages
- bakgrundsjobb och arbetsstatus
- agent som kan ta beslut inom ett godkänt scope

## OperaGX-only browser policy

Jarvis ska inte planeras runt Chrome, Edge, Firefox eller generisk browser.
Alla framtida browserflöden ska använda OperaGX/Opera som enda godkända
browser.

Regler:

- Webbsök öppnas i OperaGX/Opera.
- Browser Autopilot styr bara OperaGX/Opera.
- Playwright/browser-agent ska konfigureras för OperaGX/Opera om möjligt.
- Om Playwright inte kan styra OperaGX direkt ska Jarvis använda en isolerad
  Chromium-motor endast som automation engine, inte som användarens synliga
  standardbrowser.
- Synlig användarbrowser ska vara OperaGX/Opera.
- Ingen framtida plan ska lägga till Chrome/Edge/Firefox som primärt agentmål.

Nuvarande kod har redan `TryOpenUrlInOpera` för webbsök. Framtida arbete ska
dra browser-agenten åt samma håll i stället för att bredda whitelistan.

## Autopilot med scope

Jarvis ska kunna jobba självständigt, men inte ha permanent fri makt över hela
datorn.

Autopilot ska startas per uppdrag:

- Browser Autopilot: får söka, klicka och läsa i OperaGX/Opera inom uppdraget.
- Desktop Autopilot: får styra whitelistade appar inom uppdraget.
- Build Agent: får ändra och testa kod i `F:\Jarvis-clean`.

Allt utanför scope stoppas:

- inget `F:\New project` skrivläge
- ingen fri skrivåtkomst till hela F-disken
- inga betalningar
- inga admin/systemfönster
- inga lösenord/secrets
- ingen publicering/skickning utan tydligt godkänt läge

Jarvis ska alltid ha kill-switch och logg.

## Background thinking/status

När Jarvis tänker, indexerar, bygger, kör background jobs eller väntar på ett
långt svar ska användaren se kort status i chatten eller dashboarden.

Status ska vara praktisk, inte privat chain-of-thought.

Visa:

- vad Jarvis jobbar med just nu
- vilket steg som körs
- ungefärlig progress
- senaste säkra handling
- nästa planerade handling
- uppskattad token/context-användning när den finns
- modell eller backend när det är relevant

Exempel:

```text
Jarvis: Jobbar i bakgrunden: project audit.
Steg: indexerar filer 420/1260.
Context: ca 6k tokens för aktiv analys.
Nästa: skriver kort rapport och öppnar resultatet.
```

För lokala C#-jobb finns inte alltid exakt tokenvärde. Då ska Jarvis säga
`token: n/a` eller visa context-tecken/antal filer i stället.

## Codex-style work summaries

När Codex eller annan agent arbetar i projektet ska den ge korta statusrader
under arbetet:

- vad den läser
- vad den ändrar
- varför ändringen görs
- hur den verifierar

Slutsvaret ska säga:

- vilka filer som ändrades
- vad som inte ändrades
- vilka tester eller checks som kördes
- kända risker eller nästa steg

Detta är samma princip som Jarvis senare ska använda i dashboarden.

## Rekommenderad implementation

1. Lägg till `AgentAutopilotModeV1` med lägen: `Safe`, `Approval`,
   `BrowserAutopilot`, `DesktopAutopilot`, `BuildAgent`.
2. Lägg till `BrowserPolicyV1` där OperaGX/Opera är enda synliga browsermål.
3. Lägg till background status events i `BackgroundJobQueueV1`.
4. Lägg till token/context-estimat i LLM- och jobbsvar där det går.
5. Lägg till dashboard-widget för `Jarvis jobbar nu`.
6. Lägg till röst/TTS så Jarvis kan säga kort status högt.

Prioritet:

1. Project Index + Background Jobs polish.
2. Voice MVP.
3. SQLite tasks/memory/notes.
4. OperaGX Browser Autopilot.
5. Desktop Autopilot.
6. Brand DNA / ads / landing pages.

## Runtime-slice klar 2026-05-13

Första panel-first-slicen är implementerad:

- Översiktspanelen visar livearbete, bakgrundsjobb, tasks, pending approval,
  terminal/build och mini-agent.
- Snabbknappar gör att användaren slipper minnas alla kommandon.
- `TaskStoreV1` finns med röd/orange/blå prioritet.
- Task-skrivningar går via `PendingApprovalTypeV1.TaskChange`.

Nästa Amy-paritet bör bygga vidare på panelen: Voice MVP och en mer komplett
agent-status/tidslinje för kodarbete.
