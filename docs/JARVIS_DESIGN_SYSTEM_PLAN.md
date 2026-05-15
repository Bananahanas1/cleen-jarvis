# Jarvis Design System Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

Dashboarden har mycket inline CSS i `dashboard/index.html`. Stilen är mörk, cyan och terminalnära. Scene V3 har egen inline-style nära markupen.

## Problemet

Utan design tokens och scoped CSS blir varje ny panel ett nytt specialfall. Det gör det svårt att få Jarvis att kännas som ett sammanhållet OS.

## Vad vi ska bygga

Ett litet designsystem:

- `dashboard/theme.css` för tokens:
  - färger
  - glows
  - border
  - radius
  - spacing
  - motion
- `dashboard/scene-pro.css` för Cinematic Workspace:
  - grid background
  - orb
  - widgets
  - mission cards
  - source board
  - action chips
  - responsive layout

## Filer som påverkas

- `dashboard/theme.css`
- `dashboard/scene-pro.css`
- `dashboard/index.html`
- `tests/scene-pro-phase1.test.js`

## Risker

- CSS kan läcka till explorer/chat om den inte scopeas.
- För aggressiv override kan bryta befintlig karta eller settings.
- Externa CSS-filer måste laddas via dashboardens virtual host.

## Testplan

- Testa att CSS-filerna finns och länkas från `index.html`.
- Testa att scene CSS är scoped till `#scenePanel` eller `.scene-*`.
- Testa att viktiga slots fortfarande finns.
- Build efter runtime dashboard-ändring.

## Stegvis implementation

1. Skapa `theme.css` med `:root` tokens.
2. Skapa `scene-pro.css` med bara scene-scoped klasser.
3. Länka CSS i `index.html`.
4. Flytta inte all befintlig CSS direkt.
5. Lägg pro idle screen som första konsument.
6. Flytta mer scene styling gradvis när tester finns.
7. Undvik stora rewrites av `index.html`.
