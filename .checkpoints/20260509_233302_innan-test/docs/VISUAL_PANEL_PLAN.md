# VISUAL_PANEL_PLAN.md

Senast uppdaterad: 2026-05-06

## Syfte

Allt visuellt arbete i Jarvis-clean ska byggas som paneler ovanpå den säkra developer-workspace-strukturen.

Det betyder:

- nuvarande fil/editor/diff-yta är `Workspace Panel`
- nya visuella lager ska vara separata paneler
- Jarvis Översikt är praktisk statusyta, inte en egen plan ovanpå användarens plan
- 3D, Obsidian, NeuroLink och framtida visuella experiment ska inte ersätta huvudflödet
- visuella paneler får visa state, men får inte bli en ny osäker kontrollväg

## Nuvarande paneler

### Workspace Panel

Standardytan för praktiskt arbete:

- aktiv fil
- file panel edit mode
- pending save
- diff/review
- undo
- Terminal-panel V1

Workspace Panel är default.

### Jarvis Översikt V1

Lätt och praktisk panel i dashboarden:

- visar aktiv fil
- visar pending approval-läge
- visar senaste terminalstatus
- visar lokal minnesstatus
- visar Obsidian-status utan att skriva till vault
- visar den säkra Jarvis-loopen: Observe -> Think -> Plan -> Ask -> Act -> Verify -> Report -> Remember
- visar teknisk riktning för framtida 3D, Obsidian och NeuroLink

Jarvis Översikt V1 är avstängd som default och använder ingen tung 3D, render-loop eller bakgrundsagent.

## Framtida visuella paneler

Framtida paneler ska läggas till stegvis:

- `Project State Panel`
- `Task Workspace Panel`
- `Memory Panel`
- `Voice Panel`
- `Obsidian Panel`
- `NeuroLink Panel`
- `3D Panel`

Varje panel ska vara valfri och kunna stängas av.

## Säkerhetsregler

- Visual panels får inte skriva filer direkt.
- Visual panels får inte köra terminal direkt.
- Visual panels får inte styra desktop/browser direkt.
- Obsidian-skrivning/sync ska senare gå via PendingApproval.
- Riskabla actions måste fortfarande gå via `CommandRouterV1`, `CommandValidatorV1`, `ToolRegistryV1` och `PendingApprovalV1`.
- Visual panels ska läsa state från Jarvis, inte skapa egna osäkra action paths.

## Nästa visuella steg

1. Lägg till mer live-state i Jarvis Översikt:
   - aktiv mapp ✓
   - senaste filändring ✓
   - senaste buildstatus (kvar)
   - senaste minnesändring (kvar)
2. Gör Project Explorer tydligare visuellt:
   - aktiv fil-highlight ✓
   - aktiv mapp-highlight ✓
   - bättre folder expand/collapse state (kvar)
3. Skapa Task Workspace Panel när `.jarvis/tasks` finns.
4. Lägg till real 3D först när safety/workspace är stabilt.

## Testprincip

Varje ny panel ska ha minst ett dashboard-test som verifierar:

- panelens HTML-id:n finns
- panelen kan öppnas/stängas
- den inte kringgår pending approval
- den inte startar tung rendering som default
