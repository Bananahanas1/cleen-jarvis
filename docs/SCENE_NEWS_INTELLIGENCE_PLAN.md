# Scene News Intelligence Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

Nyhetsfrågor går genom samma `/scen`-flöde som alla andra frågor. `SceneResearchV1` hämtar generella källor, men UI:t visar dem bara som källkort. Det finns ingen tydlig news brief-struktur.

## Problemet

Nyheter kräver mer precision än vanlig presentation. Användaren behöver veta vad som hänt, vem som är inblandad, tidslinje, bekräftade delar, osäkerheter och möjliga konsekvenser. Annars ser det snyggt ut men hjälper inte beslutsmässigt.

## Vad vi ska bygga

News mode ska rendera:

- Vad har hänt?
- Vem är inblandad?
- När hände det?
- Varför spelar det roll?
- Timeline.
- Flera källor.
- Vad är bekräftat?
- Vad är osäkert?
- Möjliga konsekvenser.
- Nästa smarta frågor.

Framtida källor:

- RSS-lista i lokal config.
- GDELT som best-effort global nyhetskälla.
- Optional search APIs via env vars.
- Befintlig Wikipedia/DDG fallback för allmän kontext.

## Filer som påverkas

- `app/Scene/SceneResearchV1.cs`
- `app/Scene/SceneComposerV1.cs`
- `app/Program.cs`
- `dashboard/scene-renderer-v1.js`
- `dashboard/scene-pro.css`
- `tests/scene-news-intelligence.test.js`

## Risker

- Aktuella nyheter kan vara fel eller sakna bekräftelse.
- Rate limits och nätfel får inte spamma chatten.
- Jarvis får inte presentera osäkra uppgifter som fakta.
- RSS/API-nycklar får aldrig loggas.

## Testplan

- Testa att news-intent väljer `news_brief`.
- Testa schemafält för confirmed, uncertain, impact och next questions.
- Testa att source count begränsas.
- Testa fail-soft vid nätfel.
- Manuell test med en svensk nyhetsfråga.

## Stegvis implementation

1. Lägg enkel news-detektor i composer.
2. Skapa `news_brief` schema.
3. Be Ollama strukturera summary i rubriker, men låt UI bära layouten.
4. Lägg RSS/GDELT-plan utan att slå på externa beroenden direkt.
5. Visa confirmed/uncertain/impact som egna kort.
6. Lägg next questions som action chips.
7. Lägg system health-event vid rate limit eller DNS-fel.
