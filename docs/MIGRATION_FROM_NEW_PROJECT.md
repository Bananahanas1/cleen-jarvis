# MIGRATION_FROM_NEW_PROJECT.md

Skapad: 2026-05-10
Driver: `docs\UNIFICATION_PLAN.md` Fas 8.

## Vad som porterats från `F:\New project` till `F:\Jarvis-clean`

| Komponent | Källa (`F:\New project\`) | Mål (`F:\Jarvis-clean\`) | Anpassningar |
|-----------|---------------------------|--------------------------|--------------|
| Three.js + addons | `app\vendor\` | `dashboard\vendor\` | Inga |
| Graphify-graf | `graphify-out\graph.json` | `graphify-out\graph.json` | Inga (114 MB) |
| Graph manifest + report | `graphify-out\manifest.json`, `GRAPH_REPORT.md` | Samma | Inga |
| NeuroLinked Python | `neurolinked\` | `neurolinked\` | `__pycache__`, `brain_state` exkluderade |
| Python verktyg | `python\` | `python\` | `__pycache__` exkluderat |
| OllamaAgentHarness | `app\Agents\OllamaAgentHarness.cs` (325 rader) | `app\Agents\OllamaAgentHarness.cs` (~270 rader) | **Förenklad till read-only**. Skrivverktyg blockerade tills Fas 8 binder dem till PendingApprovalV1. AgentTools-beroendet ersatt med inline säkra implementationer. |
| ModelCatalog | `app\Core\ModelCatalog.cs` | `app\Core\ModelCatalog.cs` | Namespace ändrat till `JarvisClean`. `FindByName` utökat till `FindByNameOrRole`. `FormatList` tillagt. Records gjorda `public` för test-projektets tillgänglighet. |

## Vad som **inte** porterats (medvetet utelämnat)

| Komponent | Varför ej porterat |
|-----------|---------------------|
| `app\Bridges\NeuroLinkedBridge.cs` (gamla) | Vi skrev en ny minimal version i clean som auto-startar och offline-grace. Gamla hade fler hooks som vi inte behöver än. |
| `app\Tools\AgentTools.cs` (17 verktyg) | För osäkert utan PendingApproval-integration. Read-only varianter i clean's OllamaAgentHarness ersätter dem. Skriv-tools kommer i framtida fas. |
| `app\UI\JarvisForm.cs` | Clean har sin egen 3-panel-struktur med CommandRouterV1/PendingApprovalV1. Inget i gamla JarvisForm var bättre. |
| `app\Clients\OllamaChatClient.cs` | Clean använder direkt HTTP mot localhost:11434 i Program.cs — enklare. |
| `app\Core\IntentClassifier.cs` | Clean har CommandRouterV1 + naturligt språk via aliases. Ej nödvändigt. |
| `app\Core\JarvisRuntimePolicy.cs` | Inget motsvarande i clean — clean är Ollama-only by design. |
| `plugins\` | Plugin-systemet är experimentellt och osäkert (executerar JSON triggers). Clean ersätter med slash-commands + CommandRouterV1. |
| `dist\` | Vi gör fresh `dotnet publish`. |
| `third_party\` | Enbart referensrepos. Lämnas i gamla. |
| `obsidian-vault\` | Stor (3700+ noter). Ligger redan på F-disken som delad vault, inget byte behövs. |
| `tests\dashboard_startup_visual_contract_test.mjs` m.fl. | Specifika för gamla dashboard-arkitekturen. Clean har egen test-suite. |

## Status

- **Build**: `dotnet build app\JarvisClean.csproj` → 0 errors, 1 known MSB3277 warning
- **Tester**: 27 node-tester + 58 C#-tester, alla gröna
- **Storlek**:
  - C#-app: ~5400 rader Program.cs + 5 separata moduler
  - Dashboard: 3 HTML-filer (index, brain, explorer) + 1.4 MB vendor
  - Python: 84 filer kopierade
  - Graf: 114 MB graph.json (63052 noder)

## Konflikter och hur de löstes

### 1. `_brainWindow` / `_fileExplorerWindow` static vs instance
**Konflikt**: Sekundära fönster behövdes anropas från static `HandleMessageAsync`.
**Lösning**: Gjorde fönster-fält och Open*Window-metoder static. JarvisForm är de facto singleton i clean (inte multi-instance), så det är säkert.

### 2. `Microsoft.Extensions.FileSystemGlobbing` saknades
**Konflikt**: OllamaAgentHarness ville använda paketet för glob_files men det är inte installerat.
**Lösning**: Skrev en regex-baserad glob ([\*\\*] → `.*`, `*` → `[^/\\]*`).

### 3. AgentTools 17-verktyg vs PendingApproval
**Konflikt**: Gamla agenten kunde skriva filer direkt vilket bryter clean's PendingApproval-regel.
**Lösning**: Bara read-tools porterade. Skrivverktyg returnerar ett tydligt block-meddelande som pekar tillbaka till `/fil skapa` / `terminal preview:` (som har popup-godkännande).

### 4. Python-server bind-host
**Konflikt**: Säkerhetskravet är 127.0.0.1-only binding.
**Lösning**: NeuroLinkedBridge sätter `JARVIS_BIND_HOST=127.0.0.1` env-variabel innan Python startas (servern bör respektera det; om inte är det en framtida åtgärd för server.py).

## Återstående cleanup-arbete (efter denna fas)

- Bind PendingApproval-flow till FileExplorerWindow.SaveAsync (just nu skriver explorer direkt; Fas 4 lämnade detta som förenkling).
- Verifiera att `neurolinked\server.py` faktiskt respekterar `JARVIS_BIND_HOST`. Om inte, patch.
- Lägg till write-tools i OllamaAgentHarness som triggar PendingApproval-popup i main.
- Manuell UI-rundtur av alla tre fönster.
