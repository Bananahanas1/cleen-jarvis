# Tests

Stort lokalt smoke/regression-test:

```powershell
powershell -ExecutionPolicy Bypass -File F:\Jarvis-clean\tests\run-full-smoke.ps1
```

Det kör alla Node-tester, `CommandRouterV1.Tests`, `dotnet build` och MD-längdkoll.
Se även `docs/FULL_SYSTEM_TEST.md` för manuell Jarvis-checklista.
