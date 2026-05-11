# RELEASE_STATUS.md — Jarvis

Senast uppdaterad: 2026-05-06

## Aktuell release-status

Jarvis kan startas via:

```text
F:\Jarvis-clean\Starta-Jarvis.vbs
```

eller via publicerad release:

```text
F:\Jarvis-clean\dist\Jarvis.exe
```

Skrivbordsgenvägen `Starta Jarvis.lnk` kan också starta appen.

## Fungerar nu

- Safe dashboard.
- C# WinForms/WebView2 app.
- Project Explorer och filpanel med aktiv-fil/aktiv-mapp-highlight.
- Terminal-panel V1.
- Jarvis Översikt-panel för aktiv fil, terminal, pending approval, minne, Obsidian-status och säker arbetsloop.
- Lokal Ollama-chat.
- Lokalt markdown-minne via `data\memory.md`.
- CommandRouter V1 och slash commands.
- CommandValidator V1 / ToolRegistry V1.
- PendingApproval V1 för file write, append, delete, undo och terminal preview/approval.
- Approval popup med misclick guard.
- Change review/diff bar och one-step undo.
- Dashboard smart-open guardrails.
- Canonical smart-open path; gamla V3/V4/V5/V6/V7-dupliceringar har rensats.

## Verifierad status

Senaste loggade verifiering:

- Node safety/routing/UI tests passed.
- CommandRouterV1 C# tests passed.
- `dotnet build` passed.
- Known warning remains: WindowsBase/WebView2 version conflict warning.
- Build har 0 errors.

## Avstängt / senare

- NeuroLinked.
- Tung 3D/WebGL.
- Graphify.
- Obsidian sync/write.
- ultraPass.
- Internet/web tools.
- Desktop/browser control.

Dessa ska byggas senare och bara efter routing, approval, workspace och safety är stabilt.
