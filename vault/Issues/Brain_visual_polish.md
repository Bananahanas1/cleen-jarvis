---
type: open-issue
created: 2026-05-10
tags: [open-issue, brain, ui]
---

# Brain visual polish — sci-fi-stil

## Problem
Brain-vyn renderar noder och edges men ser fortfarande "platt" ut jämfört med `tmp-jarvis-2.0.16-check.png`. Saknar glow, pulsing, glas-paneler.

## Önskad lösning (BR1 i [[Project/BRAIN_3D_SUPERPLAN]])
- UnrealBloomPass post-processing för glow
- Pulsing emissive intensity baserat på degree
- Sci-fi glas-paneler runt 3D-canvasen
- Subtilare lines (additive blending, opacity 0.12)

## Status
Öppen — väntar på BR1-implementation.
