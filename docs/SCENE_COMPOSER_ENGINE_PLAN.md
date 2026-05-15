# Scene Composer Engine Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

`SceneServiceV1` returnerar `ScenePayloadV1` med `Query`, `Title`, `Summary`, `Cards` och `CreatedAt`. Korttyperna är enkla: hero, summary, video, source och äldre fallback-typer.

## Problemet

UI:t vet för mycket om hur scenen byggs. Framtida lägen som news brief, map brief, comparison och vision analysis behöver ett stabilt JSON-format så renderer kan visa data utan specialfall i HTML.

## Vad vi ska bygga

`SceneComposerV1` ska skapa structured scene JSON:

- `type`
- `title`
- `subtitle`
- `hero`
- `summary`
- `timeline`
- `sourceCards`
- `media`
- `mapPins`
- `actions`

Layouttyper:

- `explainer`
- `news_brief`
- `map_brief`
- `vision_analysis`
- `github_changelog`
- `comparison`
- `project_plan`

## Filer som påverkas

- `app/Scene/SceneComposerV1.cs`
- `app/Scene/SceneServiceV1.cs`
- `app/Program.cs`
- `dashboard/scene-renderer-v1.js`
- `dashboard/index.html`
- `tests/scene-composer-v1.test.js`

## Risker

- Dubbla scene-format kan skapa förvirring.
- Renderer får inte öppna länkar, skriva filer eller köra terminal.
- JSON måste vara bakåtkompatibel med V3 under migration.

## Testplan

- Unit-liknande Node-test som läser C# och JS för schemafält.
- Testa att `/scen` fortfarande använder befintliga slots under migration.
- Build efter C#-ändringar.
- Manuell test av `explainer` och `news_brief`.

## Stegvis implementation

1. Definiera records i `SceneComposerV1.cs`.
2. Skapa adapter från gamla `ScenePayloadV1` till nytt schema.
3. Lägg `scene-renderer-v1.js` med render-funktioner per layouttyp.
4. Låt dashboard använda renderer när structured scene finns.
5. Behåll `jarvisApplyScenePayloadV1` som compatibility wrapper.
6. Flytta kort-rendering ur `index.html` i små steg.
7. Lägg tester per layouttyp.
