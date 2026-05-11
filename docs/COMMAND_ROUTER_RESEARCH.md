# COMMAND_ROUTER_RESEARCH.md  Research for Jarvis CommandRouter V1

Senast uppdaterad: 2026-05-05

## Syfte

Den här filen samlar idéer för Jarvis CommandRouter V1.
Jarvis ska förstå normalt språk, men lokala kommandon måste fångas innan Ollama.

## Inspiration

### Local File Assistant
https://github.com/lukeneal-code/local-file-assistant

Idé för Jarvis:
- naturligt språk för filhantering
- skilj mellan read, write och admin-liknande rättigheter
- modellen ska inte ha fritt skrivläge till filsystemet

### OpenCode
https://github.com/opencode-ai/opencode

Idé för Jarvis:
- tool registry
- file tools
- bash/terminal tools
- permission dialogs
- användaren ska godkänna riskabla actions

### VoltAgent
https://github.com/VoltAgent/voltagent

Idé för Jarvis:
- typed tools
- tool registry
- memory adapters
- model provider abstraction
- guardrails före tool execution

### GitHub Copilot CLI / agent mode
https://docs.github.com/copilot/how-tos/copilot-cli/cli-best-practices
https://github.blog/ai-and-ml/github-copilot/copilot-ask-edit-and-agent-modes-what-they-do-and-when-to-use-them/

Idé för Jarvis:
- allowed tools
- denied tools
- review innan riskabla kommandon
- ask/chat mode, edit mode och agent mode

## CommandRouter V1 plan

Jarvis ska ha:
- CommandIntent
- CommandRisk
- CommandResult
- CommandValidator
- ToolRegistry
- PendingApproval

## Viktig routingregel

1. Normalize input
2. Expand aliases
3. Detect intent
4. Validate arguments
5. Run local tool if command is local
6. If write/terminal: create pending/preview first
7. Only normal chat goes to Ollama

## Säkerhetsregler

Memory commands får aldrig spara tomt:
- kom ihåg:
- smart minne:
- viktigt minne:
- projektminne:

Search commands får aldrig söka tomt:
- sök minne:
- sök arkiv:
- glöm minne:

File write commands ska senare kräva pending approval:
- skriv fil: path | text
- lägg till fil: path | text

Terminal commands ska alltid kräva preview först:
- terminal preview: dotnet build
- bekräfta kör
- avbryt kör

## Teknisk skuld

Program.cs har flera gamla smart-open-versioner:
- OpenProjectFileSmartV4Async
- OpenProjectFileSmartV5Async
- OpenProjectFileSmartV6Async
- OpenProjectFileSmartV7Async

Nästa AI-agent ska helst inte lägga till V8/V9.
Rätt lösning är en central CommandRouter V1.

## Slutmål

Jarvis ska förstå normal svenska:
- öppna readme
- kolla programfilen
- gå in i app
- leta efter ollama i minnet
- lägg till detta som viktigt minne
- bygg projektet men fråga först
- föreslå ändring i aktiva filen
