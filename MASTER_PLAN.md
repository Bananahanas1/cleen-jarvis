# MASTER_PLAN.md — den enda aktiva källan

Senast uppdaterad: 2026-05-16

Detta är **den enda** plan/master-filen som ska läsas av AI-agenter och
användaren för att förstå vad Jarvis-clean är, vad som gäller nu, och vad
nästa konkreta steg är. Allt annat är arkiv eller specifika delplaner.

> Om du läser denna fil: läs bara filerna under "Aktiva filer" nedan. Ignorera
> äldre `JARVIS_MASTER_PLAN.md`, `JARVIS_MEGA_MASTER_PROMPT.md`,
> `PLANNING_INDEX.md`, `NEXT_AI_AGENT_TODO.md` osv — de är arkiverade i
> `archive/2026-05-12-planning-sprawl/` och säger samma sak fast med
> motsägelser.

## Produktroll

- `cleen-jarvis` är huvudprodukten.
- `F:\Jarvis-clean` är lokal arbetsmapp.
- `F:\New project` är **read-only referens** och får aldrig ändras.
- GitHub-repot är källan externa AI-agenter kan läsa från.

## Säker kontroll-loop

```text
Observe → Think → Plan → Ask if risky → Act → Verify → Report → Remember
```

Riskabla actions går alltid via routing, validation, pending preview,
approval, verification och report. Workers får läsa och föreslå men aldrig
skriva direkt.

## Aktiva filer (läs bara dessa)

| Fil | Vad |
|---|---|
| `README.md` | Projektingång + start-instruktion |
| `AGENTS.md` | Säkerhetsregler och publish/restart-regler för AI-agenter |
| `MASTER_PLAN.md` | Denna fil — single source of truth |
| `CURRENT_STATE.md` | Vad som fungerar nu (live) |
| `TODO_NEXT.md` | Aktiv nästa-lista (vad som är öppet) |
| `BUILD_PLAN.md` | Långsiktig fasplan (Fas 0–20) |
| `JARVIS_BACKGROUND_JOBS_PLAN.md` | Detaljplan för bakgrundsjobb/projektindex |
| `KARTAN_INDEX.md` | Kartan-feature (nedprioriterad) |
| `RELEASE_STATUS.md` | Senast verifierad release |
| `docs/SESSION_LOG.md` | Historiken över vad som hänt |
| `docs/SCENE_*_PLAN.md` | Pågående Scene/Cinematic-arbete |
| `docs/UNIFICATION_PLAN.md` | 3D + Brain + Explorer-fönster-arkitektur |
| `docs/AMY_WINDOWS_AUTOPILOT_PLAN.md` | Amy-style autopilot |
| `docs/SMART_MEMORY_PLAN.md` | Smart memory-plan |

## Aktuell huvudprioritet

Project Index + Background Jobs MVP är **klar** (se CURRENT_STATE.md).

Nästa fokus enligt TODO_NEXT.md:

1. **Cinematic Workspace Pro Fas 3** — SceneComposerV1 + scene-renderer-v1.js
2. **Cinematic Workspace Pro Fas 2** — SystemHealthPanelV1
3. **Background jobs** — pause/resume, map-reduce summary
4. **Program.cs-refaktor** — flytta terminal/memory/file-tool-logik till services

## GitHub-sync

Efter större lyckad ändring:

1. `git status`
2. Relevant build/test (Node-tester + `dotnet build`)
3. Stage:a bara avsedda filer (ej `.env`, tokens, runtime-cache)
4. Commit med tydligt meddelande
5. Push till GitHub

## Markdown-regel

Alla `.md`-filer ska vara under ~14 000 tecken. Långa dokument delas i
`*_PART_01.md` osv — men då med en kort indexfil och utan att skapa
parallella "master" eller "mega"-filer. Vi har redan 14 sådana som blev
arkiverade just för att de motsade varandra.

## Vad som finns att arbeta med (snabbreferens)

Jarvis har redan: WinForms+WebView2-dashboard, Project Explorer, filpanel,
terminal-panel, lokal Ollama-chat (qwen3:1.7b default), markdown-minne,
CommandRouter V1, CommandValidator V1, ToolRegistry V1, PendingApproval V1,
safe write/delete/undo, ModelRouter, ConversationHistory, WebSearcher via
browser (OperaGX/Opera-policy), SafeAppLauncher, BuilderMode, NaturalEditTool,
desktop-control via approval, vault/AI-kontext, brain-vy, tester, Project
Index + Background Jobs, BrowserPolicyV1, Hybrid AI router (lokal + auto
gratis/online), Agent Autopilot Modes V1 (Safe/Approval/Browser/Desktop/Build).

Detaljerade fas-beskrivningar i `BUILD_PLAN.md`.
