# REFERENCE_PROJECTS.md  External inspiration sources

Senast uppdaterad: 2026-05-05

## Syfte

Den här filen listar externa projekt, artiklar och idéer som Jarvis-clean kan använda som inspiration.

Regel:
- Dessa projekt ska inte integreras direkt i Jarvis-clean ännu.
- De ska läsas som inspiration/referens.
- All funktionalitet ska byggas Jarvis-native och säkert.
- Inga externa verktyg får skriva filer direkt innan Jarvis har CommandRouter V1, CommandValidator och pending/godkännande för filskrivning.

## 1. Octogent

GitHub:
https://github.com/hesamsheikh/octogent

Viktigt:
- Använd inte länkar med mcp_token.
- Om en gammal länk innehåller token ska den saneras till ren GitHub-länk ovan.

Inspiration för Jarvis:
- agent workspace
- task folders
- context per task
- todo per task
- notes per task
- terminal/status UI
- flera agents/workflows
- handoff mellan AI-agenter

Jarvis-native idé:
- .jarvis/tasks/<task-id>/CONTEXT.md
- .jarvis/tasks/<task-id>/TODO.md
- .jarvis/tasks/<task-id>/NOTES.md
- .jarvis/tasks/<task-id>/SESSION_LOG.md

Status:
- Reference only.
- Integrera inte direkt ännu.

## 2. Claude Coworker Model

GitHub:
https://github.com/imkunal007219/claude-coworker-model

Author article:
https://medium.com/@kunalbhardwaj598/i-was-burning-through-claude-codes-weekly-limit-in-3-days-here-s-how-i-fixed-it-0344c555abda

Inspiration för Jarvis:
- worker model delegation
- billig/lokal worker för tung filläsning
- worker sammanfattar stora filer
- huvud-Jarvis bestämmer plan, säkerhet och beslut
- worker får inte skriva direkt utan pending/godkännande

## 3. LLM Guy / future inspiration

Status:
- Exakt länk saknas ännu.
- När användaren skickar rätt länk ska den läggas här.

TODO: lägg in LLM Guy GitHub / artikel / video-länk här.

## 4. Free Jarvis / ProjectPixel local reference

Local path:

```text
F:\Free Jarvis
```

Research note:

- See `docs\FREE_JARVIS_RESEARCH.md`.
- `ProjectPixel.exe` was inspected only through static metadata and was not run.
- The executable is not digitally signed.
- The folder appears to contain a Python-bundled assistant/runtime with audio, speech, API and GUI dependencies.
- `.env` contains sensitive provider variable names and must remain redacted.

Inspiration för Jarvis:

- voice mode later
- speech-to-text and text-to-speech provider design
- TTS cache cleanup policy
- safe `.env` redaction rules
- weather/API provider pattern after InternetProbe

Status:
- Reference only.
- Do not copy code until license/ownership is clear.
- Do not run `ProjectPixel.exe` outside an explicit sandboxed test plan.

## Säkerhetsregel för alla externa inspirationer

Innan något externt projekt får kopplas till Jarvis-clean måste detta vara klart:
1. CommandRouter V1
2. CommandValidator
3. Safe path validation
4. Allowed file type validation
5. Pending preview for file writes
6. User approval before write
7. Backup/checkpoint before write
8. Terminal preview/confirm flow

Ingen extern agent, worker eller tool får:
- skriva filer direkt
- köra terminalkommandon direkt
- ändra projektstruktur utan pending/godkännande
- röra F:\New project
- kringgå CommandRouterV1, CommandValidatorV1, ToolRegistryV1 eller PendingApprovalV1
