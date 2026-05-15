# Scene Pro UI UX Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

`scenePanel` visar en titel, en enkel tom-state, och efter `/scen` en vertikal layout med hero, summary, video och sources. Stilen är mörk/neonblå men ganska statisk.

## Problemet

Idle-läget säljer inte idén om Jarvis OS. Efter en fråga saknas tydlig Mission Brief-struktur med status, timeline, media, källor och action chips. Layouten behöver kännas som ett research-rum snarare än ett vanligt kortflöde.

## Vad vi ska bygga

- Idle screen:
  - Central Jarvis-orb med lätt glow/pulse.
  - Dark grid background.
  - Command feed med exempelkommandon.
  - System status widgets.
  - Ikonrad för chat, camera, map, media, files och terminal.
- Efter fråga:
  - Mission Brief View.
  - Hero header.
  - Executive summary.
  - Deep info cards.
  - Timeline.
  - Source board.
  - Media wall.
  - Jarvis conclusion.
  - Action chips: Fördjupa, Visa källor, Jämför, Öppna karta.

## Filer som påverkas

- `dashboard/index.html`
- `dashboard/theme.css`
- `dashboard/scene-pro.css`
- `tests/scene-pro-phase1.test.js`

## Risker

- För mycket animation kan göra WebView2 segt.
- En tung hero får inte trycka sönder layouten på små skärmar.
- Ikoner får inte bli nya kommandon innan de är kopplade säkert.
- CSS ska vara scoped till `#scenePanel`.

## Testplan

- Testa att `scenePanel`, `sceneStack`, `sceneHeroSlot`, `sceneSummarySlot` och idle screen finns.
- Testa att CSS-filerna länkas.
- Testa att action chips finns men bara skickar säkra textkommandon när de kopplas.
- Manuell visuell check i Jarvis efter publish.

## Stegvis implementation

1. Lägg `dashboard/theme.css` med design tokens.
2. Lägg `dashboard/scene-pro.css` med scoped scene-styling.
3. Ersätt enkel `sceneEmpty` med pro idle screen.
4. Behåll existerande slots oförändrade.
5. Lägg lätt CSS-animation: pulse, scanline, grid shimmer.
6. Lägg test som låser markup och CSS-länkar.
7. Kör scene-test, befintligt scene-test och build.
