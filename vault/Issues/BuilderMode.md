---
type: issue
created: 2026-05-10
tags: [jarvis, builder, pending-approval, open-issue]
source_file: app/Brain/BuilderMode.cs
---

# BuilderMode

BuilderMode är första passet av B4: Jarvis kan ta en idé, ställa klargörande frågor, spara svar i runtime och skapa en plan i `vault/builds/<slug>/PLAN.md`.

## Klart

- `/bygg <idé>` startar en builder-session.
- `/bygg svar <svar>` sparar svar i aktiv session.
- `/bygg status` visar idé, slug, antal svar och plan-path.
- `/bygg avbryt` rensar sessionen.
- `/bygg plan` genererar en plan och skapar pending `FileCreate`.

## Säkerhetsregel

BuilderMode får aldrig skriva filer direkt. Planen sparas först efter approval-popup. Framtida filskapande från planen ska ske en fil i taget via `PendingApprovalV1`, inte som ett stort svep.

## Nästa fas

- Lägg till "bygg från godkänd plan" som läser planfilen.
- Skapa en tydlig filkö i UI/chat.
- Visa pending preview per fil.
- Stoppa direkt om en fil redan finns, pending finns, eller målpath är osäker.
