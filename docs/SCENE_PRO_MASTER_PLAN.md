# Scene Pro Master Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

- Cinematic Workspace V3 finns i `dashboard/index.html`.
- `/scen` routas via `CommandRouterV1` till `HandleSceneShowAsync` i `app/Program.cs`.
- `app/Scene/SceneServiceV1.cs` bygger skeleton med hero, summary och video-slot.
- `app/Scene/SceneResearchV1.cs` hämtar best-effort-källor via Wikipedia och DuckDuckGo Instant Answer.
- Dashboarden har slots: `sceneStack`, `sceneHeroSlot`, `sceneSummarySlot`, `sceneVideoSlot`, `sceneSourcesSlot`.
- Voice, karta, settings, explorer, terminal och safe approval-flöden är separata och ska bevaras.

## Problemet

Scenen fungerar, men upplevelsen känns fortfarande som en dashboard med en stor bild och text. Den saknar tydlig Mission Control-identitet, idle-läge, strukturerad research-layout och separation mellan visuellt system, dataformat och rendering.

## Vad vi ska bygga

1. Fas 1: Pro idle screen och designsystem.
2. Fas 2: System Health Panel för repetitiva tekniska fel.
3. Fas 3: Scene Composer Engine med strukturerad scene JSON.
4. Fas 4: News Intelligence med tydligare research, timeline och osäkerhet.
5. Senare: Vision/Image Analysis för screenshots och inspirationsbilder.

## Filer som påverkas

- `dashboard/index.html`
- `dashboard/theme.css`
- `dashboard/scene-pro.css`
- `dashboard/scene-renderer-v1.js`
- `app/Program.cs`
- `app/Scene/SceneServiceV1.cs`
- `app/Scene/SceneComposerV1.cs`
- `app/SystemHealth/SystemHealthPanelV1.cs`
- `tests/scene-pro-phase1.test.js`
- `tests/scene-composer-v1.test.js`
- `tests/system-health-panel-v1.test.js`

## Risker

- `dashboard/index.html` är stor. Små patchar behövs.
- Scene CSS får inte påverka explorer, chat, karta eller terminal.
- News/web-fel får inte börja spamma chatten.
- Structured scene JSON får inte kringgå safe router eller approval.
- Externa webbkällor måste vara best-effort och fail-soft.

## Testplan

- Node-regressioner för scene markup, renderer API och health-panel.
- `dotnet build app/JarvisClean.csproj`.
- Befintliga scene- och voice-tester ska fortsatt passera.
- Manuell UI-check: öppna Scen, idle screen, `/scen Stockholm`, `/scen senaste nyheterna om AI`.

## Stegvis implementation

1. Skapa planfiler.
2. Lägg till `theme.css` och `scene-pro.css`.
3. Bygg pro idle screen i `scenePanel`.
4. Lägg regression för idle screen och existerande slots.
5. Lägg System Health Panel utan att ändra chat-beteende brett.
6. Lägg `SceneComposerV1` och renderer-fil.
7. Flytta news-läge till structured JSON.
8. Utöka research-källor och timeline.
9. Planera vision/upload utan att slå på tung modell som default.
