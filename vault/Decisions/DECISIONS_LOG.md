---
type: decisions
created: 2026-05-10
tags: [decision, log]
---

# Beslutslogg

Arkitektur-beslut med datum och motivering.

## 2026-05-10 — ETT program, inga lösa fönster
- **Beslut**: Brain och allt annat är paneler i main-fönstret, inte separata Forms.
- **Motivering**: Användaren vill att Jarvis känns sammanhängande.
- **Konsekvens**: BrainWindow.cs och FileExplorerWindow.cs borttagna.
- Se [[Project/MULTI_WINDOW_DESIGN]]

## 2026-05-10 — Project Explorer = the explorer
- **Beslut**: Inga separata Explorer-vyer. Vänsterpanelen är _the_ explorer.
- **Motivering**: Förenkling, mindre dubbletter.
- Se [[Project/UNIFICATION_PLAN]]

## 2026-05-10 — Vault är ETT valv på F:\Jarvis-clean\vault\
- **Beslut**: All långtids-info som MD-filer i clean-vaulten. F:\New project\obsidian-vault\ är arkiverad referens.
- **Motivering**: Ren start, snabbare graf, klarare källa.
- **Konsekvens**: FileGraphBuilder scannar bara clean-vaulten.

## 2026-05-10 — Auto-läsning av vault default ON
- **Beslut**: Jarvis läser topp 3-5 vault-noter innan varje normal chat-svar.
- **Motivering**: Användaren vill att Jarvis "snabbt läser den varje gång innan den svarar".
- **Konsekvens**: VaultSearcher kommer i Fas BR2/BR6.

## 2026-05-10 — Sci-fi-stil, inte Obsidian
- **Beslut**: Brain-vyn ska likna `tmp-jarvis-2.0.16-check.png`.
- **Motivering**: Användaren explicit valde det.
- **Konsekvens**: BR1 fokuserar på UnrealBloomPass + glas-paneler + pulsing emissive.

## 2026-05-10 — NaturalEditTool skriver aldrig direkt
- **Beslut**: B3 NaturalEditTool genererar komplett nytt filinnehåll, men skapar bara `PendingApprovalV1.FileWrite`.
- **Motivering**: Naturligt språk kan missförstå användaren; diff/preview måste godkännas först.
- **Konsekvens**: `/edit <fil> = <instruktion>` och `gå in i <fil> och ändra...` är lokala kommandon, men filen ändras först efter approval-popupen.

## 2026-05-10 — BuilderMode planerar först, skriver bara via approval
- **Beslut**: B4 BuilderMode får starta session, ställa frågor och generera plan, men `/bygg plan` får bara skapa pending `FileCreate` för `vault/builds/<slug>/PLAN.md`.
- **Motivering**: Builder-läge kan annars skapa många filer baserat på otydlig intent. Planen måste granskas innan något skrivs.
- **Konsekvens**: Första passet skriver inga app-filer. Nästa fas får skapa filer stegvis, en fil i taget, via `PendingApprovalV1`.

## 2026-05-10 — Desktop-control är default OFF och action-baserad
- **Beslut**: UI-TARS/D1/D3/D4 byggs som Jarvis-native action layer. UI-TARS/VLM får föreslå actions, men Jarvis kör bara efter pending approval.
- **Motivering**: Desktop-control kan klicka/skriva fel och måste därför ha tydlig mänsklig kontroll.
- **Konsekvens**: `/desktop på` krävs först, varje action blir `PendingApprovalV1.DesktopAction`, Ctrl+Shift+Alt+J stänger av, och blacklist/rate-limit gäller alltid.
