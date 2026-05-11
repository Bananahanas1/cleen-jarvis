# SESSION_LOG.md

## 2026-05-04

Added safe local commands to Jarvis:

- hjälp
- status
- lista filer
- lista filer i app
- lista filer i dashboard
- öppna projektmapp
- öppna dashboard
- öppna app

Still disabled:
- NeuroLinked
- 3D/WebGL
- Graphify
- Obsidian
- ultraPass
- internet tools

## Local memory added

Added simple offline memory commands:
- kom ihåg: text
- visa minne
- minnesstatus

## Memory format changed

Jarvis local memory now saves to data\memory.md instead of data\memory.txt.

## Memory context added to Ollama

Added BuildMemoryContext() in Program.cs. The latest part of data\memory.md is now included in the Ollama system prompt.

## Smart Memory commands added

Added local Smart Memory commands:
- smart minne: text
- viktigt minne: text
- projektminne: text

Confirmed that smart minne is saved locally instead of going to Ollama.

## Smart Memory continued

Added/verified Smart Memory improvements: typo-tolerant commands, command history with arrow keys, important/project memory views, memory summary, and safe archive-based forgetting.

## Diskvakt added

Added and tested Diskvakt. Jarvis can preview and safely clear selected cache/temp folders. First cleanup completed successfully with some locked files skipped.

## Diskvakt added

Added and tested Diskvakt. Jarvis can preview and safely clear selected cache/temp folders. First cleanup completed successfully with some locked files skipped.

## Offline Codex Fas 1 started

Added safe project file tools:
- läs fil
- skriv fil
- lägg till fil

Tested with docs/test-agent.md. Commands now execute locally instead of going to Ollama.

## Command help added

Added local usability commands:
- kommandohjälp
- lista md filer
- lista projektfiler

Tested successfully in Jarvis. Commands stayed local and did not go to Ollama.

## Offline Codex Fas 3 completed

Added safe pending-change workflow:
- propose heading
- pending change file
- approve change
- cancel change

Tested successfully with docs/test-agent.md. File now starts with # Test Agent.
