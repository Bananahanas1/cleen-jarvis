---
type: project-doc
source_file: "docs/MIGRATION_FROM_NEW_PROJECT.md"
created: 2026-05-10
tags: [project, mirrored]
---

# MIGRATION_FROM_NEW_PROJECT.md

Skapad: 2026-05-10
Driver: `docs\UNIFICATION_PLAN.md` Fas 8.

## Vad som porterats frÃ¥n `F:\New project` till `F:\Jarvis-clean`

| Komponent | KÃ¤lla (`F:\New project\`) | MÃ¥l (`F:\Jarvis-clean\`) | Anpassningar |
|-----------|---------------------------|--------------------------|--------------|
| Three.js + addons | `app\vendor\` | `dashboard\vendor\` | Inga |
| Graphify-graf | `graphify-out\graph.json` | `graphify-out\graph.json` | Inga (114 MB) |
| Graph manifest + report | `graphify-out\manifest.json`, `GRAPH_REPORT.md` | Samma | Inga |
| NeuroLinked Python | `neurolinked\` | `neurolinked\` | `__pycache__`, `brain_state` exkluderade |
| Python verktyg | `python\` | `python\` | `__pycache__` exkluderat |
| OllamaAgentHarness | `app\Agents\OllamaAgentHarness.cs` (325 rader) | `app\Agents\OllamaAgentHarness.cs` (~270 rader) | **FÃ¶renklad till read-only**. Skrivverktyg blockerade tills Fas 8 binder dem till PendingApprovalV1. AgentTools-beroendet ersatt med inline sÃ¤kra implementationer. |
| ModelCatalog | `app\Core\ModelCatalog.cs` | `app\Core\ModelCatalog.cs` | Namespace Ã¤ndrat till `JarvisClean`. `FindByName` utÃ¶kat till `FindByNameOrRole`. `FormatList` tillagt. Records gjorda `public` fÃ¶r test-projektets tillgÃ¤nglighet. |

## Vad som **inte** porterats (medvetet utelÃ¤mnat)

| Komponent | VarfÃ¶r ej porterat |
|-----------|---------------------|
| `app\Bridges\NeuroLinkedBridge.cs` (gamla) | Vi skrev en ny minimal version i clean som auto-startar och offline-grace. Gamla hade fler hooks som vi inte behÃ¶ver Ã¤n. |
| `app\Tools\AgentTools.cs` (17 verktyg) | FÃ¶r osÃ¤kert utan PendingApproval-integration. Read-only varianter i clean's OllamaAgentHarness ersÃ¤tter dem. Skriv-tools kommer i framtida fas. |
| `app\UI\JarvisForm.cs` | Clean har sin egen 3-panel-struktur med CommandRouterV1/PendingApprovalV1. Inget i gamla JarvisForm var bÃ¤ttre. |
| `app\Clients\OllamaChatClient.cs` | Clean anvÃ¤nder direkt HTTP mot localhost:11434 i Program.cs â€” enklare. |
| `app\Core\IntentClassifier.cs` | Clean har CommandRouterV1 + naturligt sprÃ¥k via aliases. Ej nÃ¶dvÃ¤ndigt. |
| `app\Core\JarvisRuntimePolicy.cs` | Inget motsvarande i clean â€” clean Ã¤r Ollama-only by design. |
| `plugins\` | Plugin-systemet Ã¤r experimentellt och osÃ¤kert (executerar JSON triggers). Clean ersÃ¤tter med slash-commands + CommandRouterV1. |
| `dist\` | Vi gÃ¶r fresh `dotnet publish`. |
| `third_party\` | Enbart referensrepos. LÃ¤mnas i gamla. |
| `obsidian-vault\` | Stor (3700+ noter). Ligger redan pÃ¥ F-disken som delad vault, inget byte behÃ¶vs. |
| `tests\dashboard_startup_visual_contract_test.mjs` m.fl. | Specifika fÃ¶r gamla dashboard-arkitekturen. Clean har egen test-suite. |

## Status

- **Build**: `dotnet build app\JarvisClean.csproj` â†’ 0 errors, 1 known MSB3277 warning
- **Tester**: 27 node-tester + 58 C#-tester, alla grÃ¶na
- **Storlek**:
  - C#-app: ~5400 rader Program.cs + 5 separata moduler
  - Dashboard: 3 HTML-filer (index, brain, explorer) + 1.4 MB vendor
  - Python: 84 filer kopierade
  - Graf: 114 MB graph.json (63052 noder)

## Konflikter och hur de lÃ¶stes

### 1. `_brainWindow` / `_fileExplorerWindow` static vs instance
**Konflikt**: SekundÃ¤ra fÃ¶nster behÃ¶vdes anropas frÃ¥n static `HandleMessageAsync`.
**LÃ¶sning**: Gjorde fÃ¶nster-fÃ¤lt och Open*Window-metoder static. JarvisForm Ã¤r de facto singleton i clean (inte multi-instance), sÃ¥ det Ã¤r sÃ¤kert.

### 2. `Microsoft.Extensions.FileSystemGlobbing` saknades
**Konflikt**: OllamaAgentHarness ville anvÃ¤nda paketet fÃ¶r glob_files men det Ã¤r inte installerat.
**LÃ¶sning**: Skrev en regex-baserad glob ([\*\\*] â†’ `.*`, `*` â†’ `[^/\\]*`).

### 3. AgentTools 17-verktyg vs PendingApproval
**Konflikt**: Gamla agenten kunde skriva filer direkt vilket bryter clean's PendingApproval-regel.
**LÃ¶sning**: Bara read-tools porterade. Skrivverktyg returnerar ett tydligt block-meddelande som pekar tillbaka till `/fil skapa` / `terminal preview:` (som har popup-godkÃ¤nnande).

### 4. Python-server bind-host
**Konflikt**: SÃ¤kerhetskravet Ã¤r 127.0.0.1-only binding.
**LÃ¶sning**: NeuroLinkedBridge sÃ¤tter `JARVIS_BIND_HOST=127.0.0.1` env-variabel innan Python startas (servern bÃ¶r respektera det; om inte Ã¤r det en framtida Ã¥tgÃ¤rd fÃ¶r server.py).

## Ã…terstÃ¥ende cleanup-arbete (efter denna fas)

- Bind PendingApproval-flow till FileExplorerWindow.SaveAsync (just nu skriver explorer direkt; Fas 4 lÃ¤mnade detta som fÃ¶renkling).
- Verifiera att `neurolinked\server.py` faktiskt respekterar `JARVIS_BIND_HOST`. Om inte, patch.
- LÃ¤gg till write-tools i OllamaAgentHarness som triggar PendingApproval-popup i main.
- Manuell UI-rundtur av alla tre fÃ¶nster.

