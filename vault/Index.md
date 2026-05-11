---
type: index
created: 2026-05-10
tags: [jarvis, vault, index]
---

# Jarvis Vault — Index

Detta är **valvet** — Jarvis långtidsminne som MD-filer enligt Obsidian-konvention.
All info Jarvis behöver veta innan svar finns här. AI auto-läser de mest relevanta noterna inför varje chat.

## Struktur

- [[Project/UNIFICATION_PLAN]] — den stora planen för att unifiera Jarvis-clean med New project
- [[Project/MULTI_WINDOW_DESIGN]] — 3-fönster-arkitekturen som blev till en enda dashboard
- [[Project/MIGRATION_FROM_NEW_PROJECT]] — vad som porterats från gamla
- [[Project/BRAIN_3D_SUPERPLAN]] — superplan för 3D Brain View
- [[Project/CURRENT_STATE]] — nuvarande tillstånd, faser klara
- [[Decisions/DECISIONS_LOG]] — arkitekturbeslut, en-till-en med datum
- [[Memory/Azu_preferences]] — användarens preferenser och arbetsregler
- [[Sessions/SESSIONS_LOG]] — session-för-session vad som hänt

## Tekniska byggblocks

- [[Project/CommandRouterV1]] — slash-routing och naturligt språk
- [[Project/PendingApprovalV1]] — säkert godkännande-flöde för risky writes
- [[Project/FileGraphBuilder]] — bygger 3D-grafens nod/edge-data
- [[Project/OllamaAgentHarness]] — read-only agent med 5 verktyg
- [[Project/NeuroLinkedBridge]] — Python brain-server livscykel
- [[Project/ModelCatalog]] — 5 modellprofiler

## Aktuella öppna trådar

- [[Issues/Brain_visual_polish]] — UnrealBloomPass + sci-fi-stil enligt tmp-jarvis-2.0.16
- [[Issues/Vault_AI_context]] — Jarvis ska auto-läsa vault innan varje svar (BR6)
- [[Issues/NaturalEditTool]] — B3 NL→kod-edit via PendingApproval
- [[Issues/BuilderMode]] — B4 idé→frågor→plan via PendingApproval
- [[Issues/DesktopControl]] — D1/D3/D4 UI-TARS desktop-control via pending actions

## Länksregler

- Använd `[[Note Name]]` för wiki-länkar mellan noter (Obsidian-stil)
- Frontmatter `source_file:` pekar tillbaka till projekt-fil för cross-koppling i Brain-vyn
- Tags `#jarvis`, `#decision`, `#open-issue` används för kategorisering

## Brain-vyn

I Brain-läget visas alla dessa noter som **violetta noder** kopplade till **projekt-fil-noder** (cyan/grön/gul beroende på filtyp). Klicka för att navigera.
