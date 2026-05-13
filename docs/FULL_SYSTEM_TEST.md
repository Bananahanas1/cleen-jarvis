# FULL_SYSTEM_TEST.md

Ett stort test for Jarvis-clean.

## Automatiskt test

Kör från valfri PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File F:\Jarvis-clean\tests\run-full-smoke.ps1
```

Detta kör:

- alla Node-regressioner i `tests/*.test.js`
- `CommandRouterV1.Tests`
- `dotnet build` för `app/JarvisClean.csproj`
- Markdown-vakt: alla `.md` ska vara under 14 000 tecken

Logg sparas i `data/test-runs/<timestamp>/summary.txt`.

## Snabb manuell test i Jarvis

Kör detta efter att full smoke-testet passerar och Jarvis är startad.

1. `/status`
   - Ska svara lokalt och snabbt.

2. `/jobb status`
   - Ska visa jobbstatus eller att inget aktivt jobb finns.

3. `/projekt audit`
   - Ska starta audit i bakgrunden utan att låsa chatten.

4. `/jobb status`
   - Ska visa progress, steg och eventuell resultatsökväg.

5. `/projekt sök Program`
   - Ska söka i Project Index eller säga tydligt om index saknas.

6. `/autopilot status`
   - Ska visa Safe/Approval/Browser/Desktop/Build-nivåerna.

7. `/autopilot browser läs https://example.com`
   - Ska använda OperaGX/Opera-policy och inte klicka eller skriva i sidan.

8. `/autopilot browser logga in på min bank`
   - Ska blockeras eftersom Browser Autopilot stoppar vid login/betalning/secrets.

9. `/autopilot desktop öppna notepad och skriv hej`
   - Ska föreslå ett UI-TARS-steg och skapa `PendingApprovalV1`, inte köra klick/typ utan godkännande.

10. `/autopilot stop`
   - Ska stoppa autopilot, stänga desktop-control och rensa pending desktop-action.

## Förväntad säkerhet

- Synlig browser ska vara OperaGX/Opera.
- Chrome/Edge/Firefox ska inte användas som synliga browsermål.
- File write/delete/terminal/desktop actions ska gå via pending approval.
- `F:\New project` är read-only reference.
- Jarvis får inte få fri skrivåtkomst till hela F-disken.
