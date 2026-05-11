# MASTER_PLAN.md — Jarvis långsiktig plan

Senast uppdaterad: 2026-05-09 (unifieringsplan godkänd)

## Huvudmål

Jarvis ska bli en lokal/offline-first AI-assistent som kan hjälpa med dator- och utvecklingsarbete utan att bli ett okontrollerat "gör vad som helst på datorn"-verktyg.

Den detaljerade långsiktiga visionen finns i:

```text
docs\JARVIS_LONG_TERM_VISION.md
```

## Prioritetsordning

1. Jarvis ska först bli expert på `F:\Jarvis-clean`.
2. Sedan ska Jarvis bli expert på användarens andra kodprojekt.
3. Därefter kan Jarvis bli en bredare datorassistent.
4. Desktop/browser/screen control kommer mycket senare och måste vara extra säkert.

## Säker kontroll-loop

```text
Observe -> Think -> Plan -> Ask if risky -> Act -> Verify -> Report -> Remember
```

Riskabla handlingar ska alltid gå via routing, validation, pending preview, approval, verification och report.

## Aktuell fasstatus

Redan implementerat eller delvis stabilt:

- Safe dashboard.
- Local Ollama chat.
- Local markdown memory.
- Project Explorer och filpanel.
- CommandRouter V1.
- CommandValidator V1.
- ToolRegistry V1.
- PendingApproval V1.
- Slash commands.
- File write/append/delete/undo approval safety.
- Terminal preview/approval.
- Terminal-panel V1.
- Dashboard smart-open guardrails.
- One canonical smart-open path efter cleanup av gamla V3/V4/V5/V6/V7-varianter.

Kvar i närtid:

- Manual UI verification.
- Project Explorer tree polish.
- File panel edit mode med pending save.
- Terminal transcript formatting.
- `/fil skapa` med pending approval.
- Named checkpoints/history.

## Senare faser

- Task workspace i `.jarvis/tasks`.
- Worker delegation där workers bara får läsa, sammanfatta och föreslå.
- Multi-root Project Explorer med read-only som default.
- Local Ollama/Claude Code setup docs/scripts.
- Visual Lab / 3D som valfritt lager, off by default.
- Voice Jarvis på samma säkra router/validator/approval-system.

## Viktiga regler

- Ändra aldrig `F:\New project` — den är read-only-referens. Vi kopierar därifrån, skriver aldrig dit.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.
- All filskrivning går via `PendingApprovalV1` — gäller även från Brain/Explorer-fönstren.
- Gör små steg och verifiera varje steg.

## Unifieringsplan (2026-05-09)

Den nya riktningen är dokumenterad i `docs\UNIFICATION_PLAN.md`. Sammanfattning:

- **Slutmål**: ETT projekt på `F:\Jarvis-clean\` med multi-window-arkitektur.
- **3 fönster**: Main (chat+explorer+editor) + Brain (3D NeuroLinked) + File Explorer (sekundär huvudskärm).
- **Brain är always-on**: NeuroLinked Python-server auto-startas med main-appen. Offline-graceful — Ollama + lokala verktyg fungerar utan internet.
- **Ordning**: Fas 0 (MD) → Fas 1 (slutför baseline) → Fas 2-3 (3D vendor + Brain) → Fas 4 (Explorer) → Fas 5 (Python server) → Fas 6-7 (OllamaAgent + ModelCatalog) → Fas 8 (cleanup).
- **Bästa-av-bägge**: Behåll clean's CommandRouter/PendingApproval/säkra defaults; portera in 3D, OllamaAgent (17 verktyg), ModelCatalog (5 modeller), Graphify, Obsidian från `F:\New project`.
