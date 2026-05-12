# UNIFICATION_PLAN PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


- [ ] **4.1** Skapa `F:\Jarvis-clean\dashboard\explorer.html`:
  - TvÃ¥-panel: tree (vÃ¤nster, expanderbar) + multi-tab editor (hÃ¶ger)
  - SÃ¶k-fÃ¤lt, filter, multi-root-stÃ¶d (F:\Jarvis-clean default; F:\New project read-only)
  - Ã…teranvÃ¤nd Project Explorer-koden frÃ¥n huvuddashboarden
- [ ] **4.2** Skapa `F:\Jarvis-clean\app\FileExplorerWindow.cs`:
  - Egen `Form` med `WebView2`
  - Delar samma WebMessage-protokoll som main fÃ¶r fil-lÃ¤sning/skrivning
  - All skrivning gÃ¥r genom samma `PendingApprovalV1`
- [ ] **4.3** LÃ¤gg till knapp `File Explorer` + slash-kommando `/explorer`
- [ ] **4.4** Multi-root: `F:\Jarvis-clean` (read-write via approval), `F:\New project` (read-only)
- [ ] **4.5** Skapa `tests\file-explorer-window.test.js`

**Verifiering**:
- File Explorer Ã¶ppnas separat
- Pending approval triggas fÃ¶r skrivning Ã¤ven frÃ¥n Explorer-fÃ¶nstret
- Read-only F:\New project blockerar skrivfÃ¶rsÃ¶k
- Build 0 errors

**Publish/restart**: ja

### Fas 5 â€” Python NeuroLinked-server (always-on) (~6-8h)

Nu kopplar vi in den riktiga Python-servern sÃ¥ brain alltid Ã¤r tillgÃ¤nglig.

- [ ] **5.1** Kopiera `F:\New project\neurolinked\` â†’ `F:\Jarvis-clean\neurolinked\` (inte F:\New project)
- [ ] **5.2** Kopiera `F:\New project\python\` â†’ `F:\Jarvis-clean\python\`
- [ ] **5.3** Skapa `F:\Jarvis-clean\app\Bridges\NeuroLinkedBridge.cs`:
  - Probar Python (samma logik som gamla, sÃ¤ker discovery: JARVIS_PYTHON env, py -3, lokala installs, PATH)
  - `StartAsync()` â†’ kÃ¶r `neurolinked\run.py` som child-process vid app-start
  - `StopAsync()` â†’ killar child vid app-shutdown
  - `IsAlive()` â†’ HTTP-GET mot localhost:8000/api/state med 800ms timeout
  - **Auto-start** vid main app OnLoad (efter dashboard ready)
- [ ] **5.4** BrainWindow.cs uppdateras:
  - Brain-fÃ¶nstret kan Ã¶ppnas nÃ¤r som helst â€” Python Ã¤r redan redo
  - Navigera WebView till `http://127.0.0.1:8000`
  - Watchdog: om server inte ready inom 8s â†’ visa fallback statisk brain.html (Fas 3)
- [ ] **5.5** **Offline-graceful**: NeuroLinkedBridge respekterar InternetProbe (Fas 1.3)
  - Lokal brain (Ollama + memory) fortsÃ¤tter funka utan internet
  - Web-tools (vÃ¤der, news, sÃ¶kning) svarar "Internet saknas, hoppar Ã¶ver"
- [ ] **5.6** Sluten port-policy: NeuroLinked binder bara till 127.0.0.1, ingen extern access
- [ ] **5.7** Status-indikator i main: chip som visar "Brain: redo" / "Brain: startar..." / "Brain: ej tillgÃ¤nglig"
- [ ] **5.8** Test: `tests\neurolinked-bridge.test.js` (start/stop/timeout/fallback/offline)

**Verifiering**:
- Main startar â†’ Python auto-startar i bakgrunden, status-chip "Brain: redo" inom 10s
- Brain-fÃ¶nster Ã¶ppnas â†’ dashboard visas direkt (Python redan redo)
- StÃ¤ng main â†’ Python stoppas inom 5s, inga rest-processer
- StÃ¤ng av WiFi â†’ Ollama + memory fortsÃ¤tter fungera, web-tools faller tillbaka
- Om Python saknas â†’ main fortsÃ¤tter, Brain-fÃ¶nster visar "krÃ¤ver Python"
- `Get-Process python` efter app-stop visar inga rest

**Publish/restart**: ja, med uppdatering i CURRENT_STATE.md om always-on policy

### Fas 6 â€” OllamaAgentHarness (17 verktyg) (~4-5h)

Bring in tool-calling-agenten frÃ¥n gamla sÃ¥ Ollama kan lÃ¤sa/Ã¤ndra filer.

- [ ] **6.1** Kopiera `F:\New project\app\Agents\OllamaAgentHarness.cs` â†’ `F:\Jarvis-clean\app\Agents\OllamaAgentHarness.cs`
- [ ] **6.2** Anpassa till clean's safety-regler:
  - `write_file`, `replace_in_file`, `run_command` MÃ…STE gÃ¥ genom `PendingApprovalV1`
  - `read_file` fÃ¥r lÃ¤sa F:\Jarvis-clean och F:\New project (read-only)
  - INGEN fri F-disk-skrivning
- [ ] **6.3** `run_command` blockas helt utanfÃ¶r whitelistade kommandon (`dotnet build`, `dotnet test`, `dotnet publish`)
- [ ] **6.4** LÃ¤gg till slash-kommando `/agent <task>` som triggar agentlÃ¤ge
- [ ] **6.5** Test: `tests\ollama-agent-safety.test.js` â€” verifiera att alla 17 verktyg respekterar PendingApproval

**Verifiering**:
- Agent kan lÃ¤sa filer fritt
- Agent fÃ¶rsÃ¶ker skriva â†’ pending approval popup
- Agent fÃ¶rsÃ¶ker `rm -rf` â†’ blockas
- Build 0 errors

### Fas 7 â€” Multi-model (ModelCatalog) (~2h)

Multi-model switching frÃ¥n gamla.

- [ ] **7.1** Kopiera `F:\New project\app\Core\ModelCatalog.cs`
- [ ] **7.2** LÃ¤gg till `_activeModel` field i JarvisForm
- [ ] **7.3** Slash-commands: `/modell visa`, `/modell byt <namn>`, `/modell snabb`, `/modell kod`
- [ ] **7.4** Auto-upgrade fastâ†’coder vid agent-lÃ¤ge
- [ ] **7.5** Pull-script som verifierar att alla 5 modeller finns lokalt

**Verifiering**: byter modell mid-conversation â†’ kontext bevaras

### Fas 8 â€” Cleanup, dokumentation, slutverifiering (~2-3h)

- [ ] **8.1** Uppdatera alla MD-filer i F:\Jarvis-clean med slutgiltig arkitektur
- [ ] **8.2** Skriv `MIGRATION_FROM_NEW_PROJECT.md` med exakt vad som porterats
- [ ] **8.3** Skriv ny `RELEASE_STATUS.md` med v1.0 unified
- [ ] **8.4** Markera `F:\New project` som **arkiverad referens** (skapa `F:\New project\ARCHIVED.md`)
- [ ] **8.5** Full regressionstest: alla node-tester + alla C#-tester + manuell UI-rundtur
- [ ] **8.6** Final publish + restart, screenshot fÃ¶r dokumentation

**Verifiering**: hela suiten grÃ¶n, manuell rundtur OK.

## Tidsuppskattning totalt

| Fas | Tid | Kumulativt |
|-----|-----|-----------|
| 0 â€” MD | 30 min | 0:30 |
| 1 â€” Baseline | 3-4h | 4:30 |
| 2 â€” Vendor | 30 min | 5:00 |
| 3 â€” Brain-fÃ¶nster | 4-6h | 11:00 |
| 4 â€” Explorer-fÃ¶nster | 3-4h | 15:00 |
| 5 â€” Python server | 6-8h | 23:00 |
| 6 â€” OllamaAgent | 4-5h | 28:00 |
| 7 â€” Multi-model | 2h | 30:00 |
| 8 â€” Cleanup | 2-3h | 33:00 |

**Totalt: ~30-33 timmar effektivt arbete**, fÃ¶rdelade Ã¶ver flera sessioner.

## Risker och mitigeringar

| Risk | Sannolikhet | Mitigering |
|------|-------------|------------|
| Python-server hÃ¤nger main-appen | HÃ¶g | BrainWindow Ã¤r async, watchdog 8s, fallback statisk brain.html |
| 3D fryser pÃ¥ svaga datorer | Medel | safe-mode-flagga `?safe=1` som hoppar Ã¶ver WebGL |
| OllamaAgent kringgÃ¥r PendingApproval | HÃ¶g (kritisk om missas) | Whitelist + tester per verktyg, code review fÃ¶re merge |
| Build.ps1 third_party-bugg flyttas hit | Medel | Vi kÃ¶r `dotnet publish` direkt, ingen third_party-kopiering |
| Konflikter clean vs old i samma fil | Medel | Filerna kopieras med nya namn, jÃ¤mfÃ¶rs explicit |
| MD-filer hamnar ur synk | HÃ¶g | Fas 0 + 8 explicit MD-uppdatering |
| AnvÃ¤ndaren tappar kontext | Medel | Status uppdateras i CURRENT_STATE.md efter varje fas |

## Beslut som krÃ¤vs av anvÃ¤ndaren INNAN start

1. **BekrÃ¤fta multi-window-arkitekturen** (3 separata fÃ¶nster: Main, Brain, Explorer) â€” eller fÃ¶redrar du tabbar i ett fÃ¶nster?
2. **BekrÃ¤fta att F:\New project blir read-only-referens** efter portning â€” inga Ã¤ndringar dÃ¤r, vi bara lÃ¤ser/kopierar.
3. **BekrÃ¤fta sÃ¤kerhetsregeln**: Python NeuroLinked-server startar bara pÃ¥ explicit anvÃ¤ndarbegÃ¤ran, default OFF.
4. **BekrÃ¤fta tidsuppskattning** ~30h Ã¶ver flera sessioner â€” eller vill du ha snabbare/mindre version?
5. **BekrÃ¤fta att vi kÃ¶r Fas 0+1 fÃ¶rst** fÃ¶r att slutfÃ¶ra Jarvis-clean baseline innan vi rÃ¶r 3D.

## Verifieringskedja per fas

Varje fas slutar med samma checklista:

```
[ ] dotnet build â†’ 0 errors
[ ] dotnet run --project tests\CommandRouterV1.Tests â†’ grÃ¶n
[ ] node tests\*.test.js â†’ alla grÃ¶na
[ ] Manuell UI-rundtur (om relevant)
[ ] CURRENT_STATE.md uppdaterad
[ ] TODO_NEXT.md uppdaterad (markera klara, lÃ¤gg till nya)
[ ] Publish + restart om runtime-Ã¤ndring
[ ] Commit i git med tydligt budskap
```

## Ã…terhÃ¤mtning om nÃ¥got gÃ¥r fel

- Varje fas backas upp med `.checkpoints\<datum>\` (befintligt system)
- Build-fel â†’ kÃ¶r `dotnet clean` + `dotnet restore` + `dotnet build`
- Brain-fÃ¶nster fryser â†’ close, kÃ¶r `taskkill /F /IM Jarvis.exe` + omstart med `?safe=1`
- Python-server hÃ¤nger â†’ close Brain â†’ bridge dÃ¶dar process
- Vid total rÃ¶ra: `git reset --hard <senaste-grÃ¶na-commit>`

## Vad jag (Claude Code) Ã¤r sÃ¤ker pÃ¥ att jag kan utfÃ¶ra

âœ… Fas 0 (MD-uppdatering) â€” trivialt
âœ… Fas 1 (baseline) â€” bygger pÃ¥ existerande mÃ¶nster i clean
âœ… Fas 2 (vendor copy) â€” bara filkopiering
âœ… Fas 3 (Brain statisk) â€” ren JS+C# port av kÃ¤nda komponenter
âœ… Fas 4 (Explorer-fÃ¶nster) â€” bygger pÃ¥ Project Explorer som finns
âœ… Fas 6 (OllamaAgent) â€” koden finns, bara sÃ¤kra anpassningar
âœ… Fas 7 (ModelCatalog) â€” direkt port
âœ… Fas 8 (cleanup) â€” dokumentation
âš ï¸ Fas 5 (Python server) â€” krÃ¤ver att anvÃ¤ndaren har Python installerat och aktivt godkÃ¤nner. Tekniken Ã¤r vÃ¤lkÃ¤nd men beroenden Ã¤r komplexa.

**Slutsats**: Jag kan utfÃ¶ra hela planen. Fas 5 Ã¤r den enda med extern beroende-risk men har tydlig fallback (statisk brain.html frÃ¥n Fas 3).
