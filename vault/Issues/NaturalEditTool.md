---
type: issue
created: 2026-05-10
tags: [natural-edit, pending-approval, b3, jarvis]
source_file: app/Brain/NaturalEditTool.cs
---

# NaturalEditTool

B3 första pass är implementerat.

Jarvis kan nu tolka:

- `/edit <fil> = <beskrivning>`
- `gå in i <fil> och <beskrivning>`

Flödet är säkert:

1. Läs målfilen lokalt inom `F:\Jarvis-clean`.
2. Be `qwen2.5-coder:7b` returnera komplett nytt filinnehåll.
3. Skapa `PendingApprovalV1.FileWrite`.
4. Vänta på användarens godkännande innan filen skrivs.

Viktigt beslut: NaturalEditTool får aldrig skriva direkt till disk. Det ska alltid gå via pending preview, precis som övrig filskrivning.

Begränsningar första passet:

- Max 24000 tecken per fil.
- Endast säkra textfiltyper: `.md`, `.txt`, `.json`, `.cs`, `.html`, `.css`, `.js`, `.ps1`.
- Ingen diff-apply ännu; modellen returnerar fullständigt nytt filinnehåll och befintlig approval/review visar ändringen.
