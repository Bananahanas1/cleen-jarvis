---
type: issue
created: 2026-05-10
tags: [jarvis, desktop-control, ui-tars, pending-approval, open-issue]
source_file: app/Bridges/UiTarsBridge.cs
---

# DesktopControl

Jarvis har nu ett säkert första hela pass för D1/D3/D4: UI-TARS bridge, screenshot och klick/typ-actions.

## Klart

- `/desktop status`
- `/desktop på`
- `/desktop av`
- `/desktop tars start`
- `/desktop tars stop`
- `/skärm`
- `/desktop klick 100 200`
- `/desktop dubbelklick 100 200`
- `/desktop högerklick 100 200`
- `/desktop drag 100 200 300 400`
- `/desktop skriv text`
- `/desktop hotkey ctrl+l`
- `/desktop scroll down 3`
- `/desktop fråga <instruktion>`

## Säkerhetsregel

Desktop-control är av som default. Varje action måste bli pending preview först. UI-TARS eller annan VLM får bara föreslå action; Jarvis kör den inte utan användarens godkännande.

Hard kill: Ctrl+Shift+Alt+J.

## Nästa polish

- Visa screenshot-thumbnail i approval-popup.
- Lägg till multi-monitor-stöd.
- Lägg till post-action verifiering: ny screenshot efter action och kort status.
- Förbättra `/desktop fråga` när användaren har valt UI-TARS-kompatibel API.
