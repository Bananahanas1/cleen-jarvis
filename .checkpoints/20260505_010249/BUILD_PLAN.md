# BUILD_PLAN.md — Jarvis-clean

## Fas 0: Regler och säkerhet

- Läs README.md och AGENTS.md.
- Ändra aldrig F:\New project.
- Skriv bara i F:\Jarvis-clean.
- Starta inga tunga servrar eller simulationer.

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
