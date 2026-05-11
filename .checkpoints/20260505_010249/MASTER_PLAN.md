# MASTER_PLAN.md — Jarvis långsiktig plan

Senast uppdaterad: 2026-05-04

## Huvudmål

Jarvis ska bli en lokal/offline-first AI-assistent som kan:

1. Starta snabbt utan att frysa datorn.
2. Chatta via lokal Ollama-modell.
3. Komma ihåg saker i lokalt markdown-minne.
4. Söka och sammanfatta sitt minne.
5. Använda lokala tools säkert.
6. Hjälpa till med projektfiler i F:\Jarvis-clean.
7. Ha agentläge som bara får jobba i tillåtna mappar.
8. Skydda C-disken från att fyllas.
9. Senare få röstläge.
10. Mycket senare få NeuroLinked/3D/Graphify/Obsidian/ultraPass.

## Viktig regel

Bygg inte allt på en gång.

Jarvis ska utvecklas i små stabila steg.
Varje större funktion ska testas innan nästa läggs till.

## Offline Codex-läge

Jarvis ska på lång sikt kunna fungera som en lokal/offline Codex-liknande kodagent. Planen finns i docs\OFFLINE_CODEX_PLAN.md.

Detta ska byggas stegvis och säkert, med F:\Jarvis-clean som enda tillåtna arbetsmapp i början.

## Project Explorer

Jarvis ska senare få en egen fil-explorer där användaren kan klicka på projektfiler och Jarvis vet vilken fil som är aktiv. Detta blir viktigt för Offline Codex-läget.
