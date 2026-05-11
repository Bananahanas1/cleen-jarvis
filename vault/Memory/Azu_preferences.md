---
type: memory
created: 2026-05-10
tags: [user, preferences, jarvis-must-know]
---

# Azu — preferenser och arbetsregler

Detta är användaren bakom Jarvis. Jarvis ska känna till och respektera dessa regler i varje svar.

## Profil

- Hobby-utvecklare, intresserad av AI-assistenter
- Driver Jarvis-clean-projektet
- Pratar **svenska** som huvudspråk

## Arbetsregler

- **Alltid på svenska** om inte annat begärs
- **Verifiera innan klart**: kör tester, kolla build, innan man säger "klart"
- **Säkra defaults**: filskrivning bakom PendingApproval, ingen fri F-disk-access
- **F:\New project är read-only-referens** — vi läser därifrån, skriver aldrig dit
- **F:\Jarvis-clean är hem** för all aktiv utveckling
- **Inga lösenord/API-nycklar** i logs, chat, eller markdown

## Stilpreferenser i UI

- Mörka teman med cyan/grön/gul accent (mörk-blå bg #03060c)
- Sci-fi-stil > Obsidian-stil för 3D-grafen — gillar `tmp-jarvis-2.0.16-check.png`
- ETT program = ETT fönster — inga lösa Windows Forms
- Chat ska vara synlig överallt
- Brain-mode döljer Project Explorer så fokuset är på 3D + chat

## Vad Jarvis ALLTID ska veta

- Vault är _the_ source-of-truth för långtidsminne — läs den innan svar
- Project Explorer (vänster) är _the_ explorer — inga andra fil-vyer
- All filskrivning från Brain/Explorer/Agent går via PendingApproval
- 3D Brain-vyn är opt-in (klick på Brain-knappen), inte default

## Vad Jarvis ALDRIG ska göra

- Skriva till `F:\New project\` (utom ARCHIVED.md som är godkänt)
- Skapa nya separata Windows-fönster
- Spara hemligheter i markdown
- Bypassa PendingApproval för "convenience"
- Auto-starta tunga simulationer utan användaren ber om det
