# Jarvis-clean

Jarvis-clean är en ren, offline-first rebuild av Jarvis i `F:\Jarvis-clean`.

Målet är en svensk lokal developer/control assistant som först blir expert på Jarvis-clean, sedan på användarens andra kodprojekt, och först mycket senare blir en bredare datorassistent.

Originalprojektet finns i `F:\New project` och är read-only reference. Det ska inte ändras.

## Nuvarande funktioner

- Safe dashboard i C# WinForms/WebView2.
- Project Explorer och filpanel/kodvisare.
- Terminal-panel V1 för full terminaloutput.
- Jarvis Översikt-panel för aktiv fil, terminal, pending approval, minne, Obsidian-status och säker arbetsloop.
- Jarvis Chat med lokal Ollama.
- Lokalt markdown-minne.
- CommandRouter V1 med slash commands.
- CommandValidator V1 och ToolRegistry V1.
- PendingApproval V1 för file write, append, delete, undo och terminal preview/approval.
- PendingApproval V1 source support för `/fil skapa` och file panel pending save.
- Dashboard smart-open guardrails.
- File write/delete/undo safety med approval popup och review/diff UI.

## Viktiga säkerhetsregler

- Ändra aldrig `F:\New project`.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.
- Riskabla actions ska kräva pending preview och approval.
- Bara normal chat/resonemang ska gå till Ollama; lokala commands ska hanteras lokalt först.
- Real 3D, Obsidian sync/write, NeuroLink och desktop/browser control är senare arbete och ska vara extra säkert.

## Start

Starta Jarvis via:

```text
F:\Jarvis-clean\Starta-Jarvis.vbs
```

eller via publicerad release i `dist\Jarvis.exe`.

## Aktuell dokumentation

- `CURRENT_STATE.md`
- `TODO_NEXT.md`
- `docs\CODEX_HANDOFF.md`
- `docs\JARVIS_LONG_TERM_VISION.md`
- `docs\VISUAL_PANEL_PLAN.md`
- `docs\SESSION_LOG.md`
