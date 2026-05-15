# System Health Panel Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

Tekniska fel kan dyka upp i chatten när webbtjänster eller live-data misslyckas. Exempel är DNS-fel för `airplanes.live`, OpenSky 429 och AISStream 1006.

## Problemet

Chatten blir brusig om samma tekniska fel upprepas. Användaren behöver se att systemet är vaket och vad som är degraderat, men utan att research- eller chatflödet fylls av samma fel.

## Vad vi ska bygga

`SystemHealthPanelV1` ska samla tekniska händelser:

- provider/service
- severity
- firstSeen
- lastSeen
- count
- shortMessage
- details
- suggestedAction

Chatten visar första felet kort och hänvisar sedan till systempanelen. Upprepningar aggregeras.

## Filer som påverkas

- `app/SystemHealth/SystemHealthPanelV1.cs`
- `app/Program.cs`
- `dashboard/index.html`
- `dashboard/theme.css`
- `dashboard/scene-pro.css`
- `tests/system-health-panel-v1.test.js`

## Risker

- Fel får inte döljas helt.
- Health-panel får inte logga secrets eller fulla requestar.
- Aggregering måste vara enkel och deterministisk.
- UI ska inte kräva ny tung dependency.

## Testplan

- Testa att upprepade fel ökar `count`.
- Testa att första felet kan returnera chat-hint.
- Testa att senare fel bara uppdaterar panel.
- Testa att secrets maskas.
- Manuell check med simulerat servicefel.

## Stegvis implementation

1. Skapa `SystemHealthEventV1` och `SystemHealthPanelV1`.
2. Lägg in-memory store med max antal events.
3. Koppla best-effort health snapshot till dashboard.
4. Lägg system panel i UI.
5. Koppla utvalda web/live-data-fel till health.
6. Lägg chat suppression för repetitiva fel.
7. Lägg test för airplanes/OpenSky/AISStream-exempel.
