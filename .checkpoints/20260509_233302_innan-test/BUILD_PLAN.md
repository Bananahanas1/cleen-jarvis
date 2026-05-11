# BUILD_PLAN.md — Jarvis-clean

## Long-term Jarvis direction

Se också: `docs\JARVIS_LONG_TERM_VISION.md`.

Jarvis ska inte bara vara en chatbot. Jarvis ska bli en lokal developer- och datoragent med en säker kontroll-loop:

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

Prioriteringsordning:

1. Bli expert på `F:\Jarvis-clean`.
2. Bli expert på användarens andra kodprojekt.
3. Bli bredare datorassistent.
4. Lägg till desktop/browser/screen control först senare och med extra säkerhet.

Grundregel:

- Jarvis får aldrig bli ett okontrollerat "gör vad som helst på datorn"-verktyg.
- All risky action måste gå via routing, validation, pending preview, approval, verification och report.

## Fas 0: Regler och säkerhet

- Läs README.md och AGENTS.md.
- Ändra aldrig F:\New project (read-only-referens; kopiera därifrån, skriv aldrig dit).
- Skriv bara i F:\Jarvis-clean.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.
- Andra F-drive roots ska vara read-only som default.
- All filskrivning från alla fönster går genom `PendingApprovalV1`.

## Aktuell unifieringsplan (2026-05-09)

Detaljerad fasplan finns i `docs\UNIFICATION_PLAN.md`. Faserna nedan (Fas 1-20) är den ursprungliga clean-bygget. Unifieringsplanens Fas 0-8 körs ovanpå dessa när användaren godkänt — vi har redan gått igenom Fas 1-15 i clean och är redo att lägga till 3D, Brain-fönster och File Explorer-fönster.

## Fas 1: Minimal WinForms/WebView2-app

Mål:
- Ett enkelt Jarvis-fönster.
- WebView2 laddar en lokal safe dashboard.
- Ingen 3D.
- Ingen NeuroLinked simulation.

## Fas 2: Safe dashboard utan 3D/WebGL

Mål:
- Enkel HTML-dashboard.
- Chatbox syns.
- Ingen Three.js.
- Ingen WebGL.
- Ingen Graphify-karta.
- Ingen animation-loop.

## Fas 3: Chatbox dashboard till C#

Mål:
- Text från dashboard skickas till C#.
- C# kan skriva svar tillbaka till dashboarden.

## Fas 4: Lokal calculator/tool

Mål:
- Användaren skriver: räkna 2+2
- Jarvis svarar: 4
- Detta ska fungera utan internet och utan AI-modell.

## Fas 5: Ollama chat offline

Mål:
- Chat → "hej"
- Ollama svarar på svenska inom 10 sekunder.
- Modell: qwen3:1.7b eller qwen2.5-coder:7b.

## Fas 6: Lokala agentverktyg

Mål:
- Agenten kan lista filer.
- Agenten kan läsa säkra filer.
- Agenten får inte röra F:\New project.

## Fas 7: NeuroLinked basic utan tung brain_state

Mål:
- Enkel NeuroLinked-status.
- Ingen tung 3D.
- Ingen gammal saved brain_state.
- Ingen tung simulation i första versionen.

## Fas 8: Offline fallback för web-tools

Mål:
- Webb, väder och nyheter ska aldrig hänga UI:t.
- Om internet saknas ska Jarvis säga:
  "Internet saknas just nu, hoppar över."

## Fas 9: OpenAI/Codex bara opt-in

Mål:
- OpenAI används aldrig som default.
- Om offline och användaren skriver openai:, routa tillbaka till Ollama eller visa tydligt fel.

## Fas 10: Graphify/Obsidian senare

Mål:
- Lägg till små limits först.
- Ingen automatisk tung graph-load vid start.

## Fas 11: ultraPass sist

Mål:
- Inga lösenord i chat.
- Inga lösenord i loggar.
- Inga lösenord i AI-minne.

## Fas 12: 3D-dashboard sist och bara valfritt

Mål:
- 3D ska vara avstängt som default.
- 3D ska bara starta om användaren väljer det.

## Framtida större idéer

Större integrationer som Docker, GitHub, cloud, Kubernetes, TensorFlow, PyTorch, Slack, Teams och databaser ska inte byggas nu. De är dokumenterade i docs\FUTURE_IDEAS.md och ska bara läggas till senare, ett steg i taget.

## Smart Memory

Smart Memory planeras i docs\SMART_MEMORY_PLAN.md. Detta är nästa större lokala funktion före agentläge.

## Fas 13: Offline Codex-läge

Jarvis ska senare kunna läsa projektfiler, föreslå ändringar, visa diff, skapa checkpoints, köra build/test och hjälpa med kod offline. Se docs\OFFLINE_CODEX_PLAN.md.

Första praktiska del: säkra filverktyg.

## Fas 14: Safe control core

Mål:
- CommandRouter
- CommandValidator
- ToolRegistry
- PendingApproval
- slash-kommandon
- dashboard routing safety

Status 2026-05-05:
- CommandRouter V1 finns.
- PendingApproval V1 används för filskrivning, append, delete, undo och terminal preview/approval.
- Terminal-panel V1 finns.
- `visa terminal` / `vad stod i terminalen` routing har fixats.
- Generic `avbryt` är context-aware.

## Fas 15: Developer workspace

Mål:
- Project Explorer tree polish
- file panel edit mode
- pending save approval
- terminal transcript formatting
- build/test/publish tools
- undo/checkpoint history
- task workspace plan

## Fas 16: Smart natural language

Mål:
- svensk naturlig instruktion till safe intent
- exempel: `öppna programfilen`, `leta efter buildfelet`, `fixa detta men fråga först`
- LLM får hjälpa till med språkförståelse, men execution måste ske via validerade lokala intents.

## Fas 17: Task workspaces

Mål:
- `.jarvis/tasks/<task-id>/CONTEXT.md`
- `.jarvis/tasks/<task-id>/TODO.md`
- `.jarvis/tasks/<task-id>/NOTES.md`
- `.jarvis/tasks/<task-id>/CHANGES.md`
- `.jarvis/tasks/<task-id>/RESULT.md`
- `.jarvis/tasks/<task-id>/SESSION_LOG.md`

Task workspaces ska stödja create/open/list task, todos, summaries och handoff till nästa AI/Codex-session.

## Fas 18: Worker agents

Mål:
- worker-read
- worker-summarize
- worker-find-files
- worker-draft-change

Workers får aldrig skriva direkt. De får bara läsa, sammanfatta och föreslå. Main Jarvis validerar och kräver approval för writes/runs.

## Fas 19: Desktop control

Mål:
- open programs
- browser automation
- screenshot understanding
- click/type with approval

Desktop control ska komma mycket senare och får aldrig vara okontrollerat.

## Fas 20: Voice Jarvis

Mål:
- voice input
- voice output
- hands-free project assistant

Voice ska använda samma CommandRouter/Validator/PendingApproval som text.

## Fas: InternetProbe före web-tools

Innan Jarvis får webbsök, väder, nyheter eller internetbaserade tools måste ett lokalt internetstatus-kommando finnas. C# ska kontrollera internetstatus direkt och inte låta Ollama gissa.

## Command Alias Router

Jarvis ska ha ett centralt alias-lager som översätter vanligt språk till säkra lokala kommandon. Alla nya kommandon ska få flera naturliga sätt att triggas, så användaren kan prata normalt och Jarvis väljer rätt verktyg.

## Command safety and natural language routing plan

Jarvis-clean should become a natural Swedish-first assistant. The user should be able to speak normally, and Jarvis should decide whether the request is:

- normal chat
- file open
- folder navigation
- memory save/search
- archive search
- model change
- terminal preview/run
- safe code/file edit

### Core rule

Local commands must be handled before Ollama.

Bad behavior:
- User: `öppna readme.md`
- Ollama answers with instructions about Ollama.

Correct behavior:
- Jarvis local router detects file open.
- Jarvis resolves matching files.
- If one match: open in middle file panel.
- If multiple matches: show options / autocomplete suggestions.
- Ollama is not involved.

### Safety principle

Any command that writes, deletes, archives, runs terminal commands, or changes files must be validated first.

Future file write flow:
1. User asks to edit/write.
2. Jarvis creates preview/pending file.
3. User reviews.
4. User confirms.
5. Only then does Jarvis write to disk.

### Technical debt to remove

Keep smart file-open centralized. Earlier V4/V5/V6/V7 duplication has been cleaned in previous passes and should not return.

Do not add V8/V9 style patches unless necessary. Prefer a single clean CommandRouter, CommandValidator and ToolRegistry path.

### UI direction

Project Explorer should become a tree like VS Code:

- collapsed folder: ` app`
- expanded folder: ` app`
- files stay visible under folders
- expanding one folder should not hide the project root

Autocomplete should work in stages:
- command completion first
- argument completion second
- file/folder path completion after commands like `öppna`, `läs fil:`, `skriv fil:`, `öppna mapp:`

Autocomplete should only show commands that are confirmed working.

## Reference projects list

External inspiration sources are now tracked in docs/REFERENCE_PROJECTS.md.

Known references:
- https://github.com/hesamsheikh/octogent
- https://github.com/imkunal007219/claude-coworker-model
- https://medium.com/@kunalbhardwaj598/i-was-burning-through-claude-codes-weekly-limit-in-3-days-here-s-how-i-fixed-it-0344c555abda

Important: Octogent links must be stored without mcp_token. LLM Guy link is still missing. These sources are inspiration only until Jarvis-clean has CommandRouter V1, CommandValidator and safe pending file write approval.

## Future: Multi-root Project Explorer for F drive

Jarvis Project Explorer should later support multiple roots: F:\Jarvis-clean, F:\New project, F:\AI\reference and other user-approved F-drive folders. Jarvis-clean can be writable only through pending approval. F:\New project must be read-only reference. Other F:\ folders should be read-only by default. Never give Jarvis unrestricted write access to the full F drive.
