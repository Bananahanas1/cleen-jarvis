---
type: open-issue
created: 2026-05-10
tags: [open-issue, vault, ai]
---

# Vault som AI-kontext

## Problem
Jarvis svarar utan att veta projektets historia. Användaren vill att Jarvis "snabbt läser vaulten varje gång innan svar".

## Önskad lösning (BR2 + BR6 i [[Project/BRAIN_3D_SUPERPLAN]])
- `VaultSearcher.cs` med TF-IDF eller substring-match
- Topp 3-5 noter som system-prompt-prefix
- Max 4KB extra per request
- Status-chip "Senaste svar använde N vault-noter"

## Status
Öppen — väntar på implementation.
