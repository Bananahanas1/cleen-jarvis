# JARVIS_CORE_INDEX.md

Senast uppdaterad: 2026-05-12

## Syfte

Detta är core-indexet för Jarvis. Läs detta före nya runtime-ändringar.

## Core-princip

Jarvis ska vara en lokal svensk developer/control assistant med en säker loop:

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

## Produktroll

- `cleen-jarvis` är huvudprodukten.
- Lokal huvudmapp är `F:\Jarvis-clean`.
- `f-drive-projects` och `F:\New project` är referens/inspiration, inte huvudprodukt.
- `F:\New project` får aldrig ändras.

## Core som redan finns

- C# WinForms/WebView2 dashboard
- Project Explorer
- filpanel/kodvisare
- terminalpanel
- Jarvis-chat med lokal Ollama
- lokalt markdown-minne
- CommandRouter V1
- CommandValidator V1
- ToolRegistry V1
- PendingApproval V1
- file write/delete/undo approval-loop
- approval popup
- review/diff UI
- ModelRouter
- ConversationHistory
- WebSearcher via browser
- SafeAppLauncher
- BuilderMode
- NaturalEditTool
- desktop-control via pending approval
- vault/AI-kontext
- brain-vy
- Node/C# tester

## Viktiga runtime-filer

- `app/Program.cs` - main runtime, WebView2 bridge och tools.
- `app/CommandRouterV1.cs` - slash/local routing.
- `app/CommandValidatorV1.cs` - validering av command results.
- `app/ToolRegistryV1.cs` - tool metadata.
- `app/PendingApprovalV1.cs` - pending approval-modell.
- `app/Brain/ModelRouter.cs` - modellval.
- `app/Brain/ConversationHistory.cs` - konversationens sliding window.
- `app/Brain/VaultSearcher.cs` - vault-kontext.
- `app/Brain/BuilderMode.cs` - idé till plan med pending file-create.
- `app/Brain/NaturalEditTool.cs` - naturlig edit till pending file-write.
- `app/Desktop/*` - desktop-control, alltid via approval.
- `dashboard/index.html` - UI.

## Routingregel

1. Normalisera input.
2. Lokala slash-kommandon går till CommandRouter V1.
3. Naturligt språk får mappas till safe local intent.
4. Validera argument.
5. Riskabla actions skapar pending preview.
6. Användaren godkänner eller avbryter.
7. Bara vanlig chat/resonemang går till Ollama.

## Riskabla actions

Kräver pending preview och approval:

- file write
- file append
- file delete
- undo/restore
- terminal run
- desktop click/type/scroll/hotkey
- framtida externa tools
- framtida browser automation

## Nästa core-MVP

Nästa build ska vara:

**Project Index + Background Jobs MVP**

Målet är att Jarvis svarar snabbt och startar lång läsning/analys i bakgrunden.
Se [JARVIS_BACKGROUND_JOBS_PLAN.md](JARVIS_BACKGROUND_JOBS_PLAN.md).

## Vad som inte ska prioriteras nu

- Full Google Earth/Kartan
- liveflyg/livebåtar
- avancerade väderlager
- stor autonom worker som skriver filer
- ny smart-open patchfamilj

## Testprincip

Efter runtime-ändring:

- kör relevanta Node-tester
- kör C# routertester
- kör `dotnet build F:\Jarvis-clean\app\JarvisClean.csproj`
- publicera/starta om bara efter runtime-ändringar och gröna tester
- docs-only ska inte publish/restarta Jarvis
