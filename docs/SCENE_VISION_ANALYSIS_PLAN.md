# Scene Vision Analysis Plan

Senast uppdaterad: 2026-05-15

## Vad som finns nu

Jarvis har ingen first-class visionanalys för screenshots eller inspirationsbilder i Cinematic Workspace. Användaren kan beskriva bilder, men UI:t kan inte ta emot bildinput och skapa strukturerad analys.

## Problemet

För att förbättra UI/UX från inspirationsbilder behöver Jarvis kunna analysera layout, färg, hierarki, komponenter och känsla. Det ska göras utan att ge modellen fri datoråtkomst eller rå filskrivning.

## Vad vi ska bygga

V1 som plan och senare implementation:

- Upload/screenshot input i Scene.
- Lokal fil hamnar inom `F:\Jarvis-clean\data\vision-inputs`.
- Pending approval krävs innan filen sparas om input kommer från runtime.
- Visionmodell tolkar bilden och returnerar structured scene:
  - design observations
  - reusable patterns
  - risks
  - concrete UI suggestions
  - implementation checklist

Framtida modellval:

- Lokal kandidat: Qwen2.5-VL mindre variant om den fungerar på 8GB VRAM.
- Fallback: extern/cloud visionmodell via env-opt-in.
- Ingen cloud fallback utan tydlig konfiguration.

## Filer som påverkas

- `app/Vision/VisionAnalysisServiceV1.cs`
- `app/Scene/SceneComposerV1.cs`
- `dashboard/scene-renderer-v1.js`
- `dashboard/scene-pro.css`
- `dashboard/index.html`
- `tests/scene-vision-analysis.test.js`

## Risker

- Bilder kan innehålla secrets.
- Screenshots kan visa privata data.
- Visionmodell kan hallucinerar designmotiv.
- Lokal modell kan vara för tung för GPU/VRAM.

## Testplan

- Testa att vision V1 inte aktiveras utan explicit input.
- Testa att bildsparning går via pending approval när runtime gör write.
- Testa att scene type blir `vision_analysis`.
- Testa att cloud provider inte används utan env-konfig.

## Stegvis implementation

1. Dokumentera schema och säkerhetsregler.
2. Lägg UI-knapp som disabled/planerad först.
3. Lägg upload-flow bakom pending approval.
4. Lägg mockad local vision adapter för test.
5. Lägg riktig modelladapter senare.
6. Koppla resultat till Scene Composer.
7. Lägg manual check med användarens inspirationsbilder.
