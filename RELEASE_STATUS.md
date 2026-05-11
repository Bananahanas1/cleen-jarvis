# RELEASE_STATUS.md — Jarvis

Senast uppdaterad: 2026-05-10

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
- NaturalEditTool B3 första pass: `/edit <fil> = <beskrivning>` och naturliga fraser skapar pending `FileWrite` preview via coder-modellen.
- BuilderMode B4 första pass: `/bygg <idé>` ställer frågor, `/bygg svar` sparar svar och `/bygg plan` skapar pending `FileCreate` för `vault/builds/<slug>/PLAN.md`.
- UI-TARS/Desktop-control safe pass: `/desktop på`, `/skärm`, `/desktop klick/skriv/hotkey/scroll/drag`, `/desktop fråga` och `/desktop av`; varje action kräver pending approval.

## Verifierad status

Senaste loggade verifiering:

- Node safety/routing/UI tests passed.
- CommandRouterV1 C# tests passed.
- `dotnet build` passed.
- Known warning remains: WindowsBase/WebView2 version conflict warning.
- Build har 0 errors.

## Aktiverat per UNIFICATION_PLAN (2026-05-10)

- **Brain inbyggd vy** (3D NeuroLinked-stil med UnrealBloomPass) — `/brain`. Inbäddad i mittpanelen, inte separat fönster.
- **Project Explorer** — vänsterpanel är _the_ explorer (egen Explorer-vy borttagen).
- **Always-on Python NeuroLinked** auto-startar i bakgrunden.
- **Read-only agent** — `/agent <task>`. Skrivverktyg explicit blockerade.
- **ModelCatalog** — `/modell`, `/modell byt <name|role>`, shortcuts: snabb/smart/kod/reason/general.
- **VaultSearcher** — `/vault status/sök/skapa/på/av`. Auto-läsning ON: Jarvis läser topp-5 vault-noter innan varje chat-svar.
- **Named checkpoints** — `/checkpoint skapa <namn>`, `/checkpoint lista`, `/checkpoint återställ <namn>`.
- **InternetProbe** — internet-status med 30s cache + offline-graceful fallback.
- **Autocomplete** — alla slash-kommandon listas med TAB.
- **NaturalEditTool** — säker NL→kod-edit via PendingApproval, ingen direkt filskrivning.
- **BuilderMode** — säker idé→plan-flow via `/bygg`; plan sparas bara efter PendingApproval.
- **UI-TARS desktop-control** — default OFF, screenshot, UI-TARS action parser, pending click/type/scroll/drag/hotkey, hard-kill Ctrl+Shift+Alt+J.

## Fortfarande senare/blockerat

- ultraPass-vault (kräver säkerhetsgranskning)
- OllamaAgent skrivverktyg via PendingApproval (Fas-8.5)
- BuilderMode nästa fas: skapa filer från godkänd plan stegvis via PendingApproval
- UI-TARS vision kräver extern/egen UI-TARS-kompatibel API-konfig innan `/desktop fråga` kan föreslå verkliga actions
- Desktop/browser-control nästa polish: approval thumbnail, multi-monitor och bättre action verifiering
