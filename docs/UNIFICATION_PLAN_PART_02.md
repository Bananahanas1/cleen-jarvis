# UNIFICATION_PLAN PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


- [ ] **5.1** Kopiera `F:\New project\neurolinked\` → `F:\Jarvis-clean\neurolinked\` (inte F:\New project)
- [ ] **5.2** Kopiera `F:\New project\python\` → `F:\Jarvis-clean\python\`
- [ ] **5.3** Skapa `F:\Jarvis-clean\app\Bridges\NeuroLinkedBridge.cs`:
  - Probar Python (samma logik som gamla, säker discovery: JARVIS_PYTHON env, py -3, lokala installs, PATH)
  - `StartAsync()` → kör `neurolinked\run.py` som child-process vid app-start
  - `StopAsync()` → killar child vid app-shutdown
  - `IsAlive()` → HTTP-GET mot localhost:8000/api/state med 800ms timeout
  - **Auto-start** vid main app OnLoad (efter dashboard ready)
- [ ] **5.4** BrainWindow.cs uppdateras:
  - Brain-fönstret kan öppnas när som helst — Python är redan redo
  - Navigera WebView till `http://127.0.0.1:8000`
  - Watchdog: om server inte ready inom 8s → visa fallback statisk brain.html (Fas 3)
- [ ] **5.5** **Offline-graceful**: NeuroLinkedBridge respekterar InternetProbe (Fas 1.3)
  - Lokal brain (Ollama + memory) fortsätter funka utan internet
  - Web-tools (väder, news, sökning) svarar "Internet saknas, hoppar över"
- [ ] **5.6** Sluten port-policy: NeuroLinked binder bara till 127.0.0.1, ingen extern access
- [ ] **5.7** Status-indikator i main: chip som visar "Brain: redo" / "Brain: startar..." / "Brain: ej tillgänglig"
- [ ] **5.8** Test: `tests\neurolinked-bridge.test.js` (start/stop/timeout/fallback/offline)

**Verifiering**:
- Main startar → Python auto-startar i bakgrunden, status-chip "Brain: redo" inom 10s
- Brain-fönster öppnas → dashboard visas direkt (Python redan redo)
- Stäng main → Python stoppas inom 5s, inga rest-processer
- Stäng av WiFi → Ollama + memory fortsätter fungera, web-tools faller tillbaka
- Om Python saknas → main fortsätter, Brain-fönster visar "kräver Python"
- `Get-Process python` efter app-stop visar inga rest

**Publish/restart**: ja, med uppdatering i CURRENT_STATE.md om always-on policy

### Fas 6 — OllamaAgentHarness (17 verktyg) (~4-5h)

Bring in tool-calling-agenten från gamla så Ollama kan läsa/ändra filer.

- [ ] **6.1** Kopiera `F:\New project\app\Agents\OllamaAgentHarness.cs` → `F:\Jarvis-clean\app\Agents\OllamaAgentHarness.cs`
- [ ] **6.2** Anpassa till clean's safety-regler:
  - `write_file`, `replace_in_file`, `run_command` MÅSTE gå genom `PendingApprovalV1`
  - `read_file` får läsa F:\Jarvis-clean och F:\New project (read-only)
  - INGEN fri F-disk-skrivning
- [ ] **6.3** `run_command` blockas helt utanför whitelistade kommandon (`dotnet build`, `dotnet test`, `dotnet publish`)
- [ ] **6.4** Lägg till slash-kommando `/agent <task>` som triggar agentläge
- [ ] **6.5** Test: `tests\ollama-agent-safety.test.js` — verifiera att alla 17 verktyg respekterar PendingApproval

**Verifiering**:
- Agent kan läsa filer fritt
- Agent försöker skriva → pending approval popup
- Agent försöker `rm -rf` → blockas
- Build 0 errors

### Fas 7 — Multi-model (ModelCatalog) (~2h)

Multi-model switching från gamla.

- [ ] **7.1** Kopiera `F:\New project\app\Core\ModelCatalog.cs`
- [ ] **7.2** Lägg till `_activeModel` field i JarvisForm
- [ ] **7.3** Slash-commands: `/modell visa`, `/modell byt <namn>`, `/modell snabb`, `/modell kod`
- [ ] **7.4** Auto-upgrade fast→coder vid agent-läge
- [ ] **7.5** Pull-script som verifierar att alla 5 modeller finns lokalt

**Verifiering**: byter modell mid-conversation → kontext bevaras

### Fas 8 — Cleanup, dokumentation, slutverifiering (~2-3h)

- [ ] **8.1** Uppdatera alla MD-filer i F:\Jarvis-clean med slutgiltig arkitektur
- [ ] **8.2** Skriv `MIGRATION_FROM_NEW_PROJECT.md` med exakt vad som porterats
- [ ] **8.3** Skriv ny `RELEASE_STATUS.md` med v1.0 unified
- [ ] **8.4** Markera `F:\New project` som **arkiverad referens** (skapa `F:\New project\ARCHIVED.md`)
- [ ] **8.5** Full regressionstest: alla node-tester + alla C#-tester + manuell UI-rundtur
- [ ] **8.6** Final publish + restart, screenshot för dokumentation

**Verifiering**: hela suiten grön, manuell rundtur OK.

## Tidsuppskattning totalt

| Fas | Tid | Kumulativt |
|-----|-----|-----------|
| 0 — MD | 30 min | 0:30 |
| 1 — Baseline | 3-4h | 4:30 |
| 2 — Vendor | 30 min | 5:00 |
| 3 — Brain-fönster | 4-6h | 11:00 |
| 4 — Explorer-fönster | 3-4h | 15:00 |
| 5 — Python server | 6-8h | 23:00 |
| 6 — OllamaAgent | 4-5h | 28:00 |
| 7 — Multi-model | 2h | 30:00 |
| 8 — Cleanup | 2-3h | 33:00 |

**Totalt: ~30-33 timmar effektivt arbete**, fördelade över flera sessioner.

## Risker och mitigeringar

| Risk | Sannolikhet | Mitigering |
|------|-------------|------------|
| Python-server hänger main-appen | Hög | BrainWindow är async, watchdog 8s, fallback statisk brain.html |
| 3D fryser på svaga datorer | Medel | safe-mode-flagga `?safe=1` som hoppar över WebGL |
| OllamaAgent kringgår PendingApproval | Hög (kritisk om missas) | Whitelist + tester per verktyg, code review före merge |
| Build.ps1 third_party-bugg flyttas hit | Medel | Vi kör `dotnet publish` direkt, ingen third_party-kopiering |
| Konflikter clean vs old i samma fil | Medel | Filerna kopieras med nya namn, jämförs explicit |
| MD-filer hamnar ur synk | Hög | Fas 0 + 8 explicit MD-uppdatering |
| Användaren tappar kontext | Medel | Status uppdateras i CURRENT_STATE.md efter varje fas |

## Beslut som krävs av användaren INNAN start

1. **Bekräfta multi-window-arkitekturen** (3 separata fönster: Main, Brain, Explorer) — eller föredrar du tabbar i ett fönster?
2. **Bekräfta att F:\New project blir read-only-referens** efter portning — inga ändringar där, vi bara läser/kopierar.
3. **Bekräfta säkerhetsregeln**: Python NeuroLinked-server startar bara på explicit användarbegäran, default OFF.
4. **Bekräfta tidsuppskattning** ~30h över flera sessioner — eller vill du ha snabbare/mindre version?
5. **Bekräfta att vi kör Fas 0+1 först** för att slutföra Jarvis-clean baseline innan vi rör 3D.

## Verifieringskedja per fas

Varje fas slutar med samma checklista:

```
[ ] dotnet build → 0 errors
[ ] dotnet run --project tests\CommandRouterV1.Tests → grön
[ ] node tests\*.test.js → alla gröna
[ ] Manuell UI-rundtur (om relevant)
[ ] CURRENT_STATE.md uppdaterad
[ ] TODO_NEXT.md uppdaterad (markera klara, lägg till nya)
[ ] Publish + restart om runtime-ändring
[ ] Commit i git med tydligt budskap
```

## Återhämtning om något går fel

- Varje fas backas upp med `.checkpoints\<datum>\` (befintligt system)
- Build-fel → kör `dotnet clean` + `dotnet restore` + `dotnet build`
- Brain-fönster fryser → close, kör `taskkill /F /IM Jarvis.exe` + omstart med `?safe=1`
- Python-server hänger → close Brain → bridge dödar process
- Vid total röra: `git reset --hard <senaste-gröna-commit>`

## Vad jag (Claude Code) är säker på att jag kan utföra

✅ Fas 0 (MD-uppdatering) — trivialt
✅ Fas 1 (baseline) — bygger på existerande mönster i clean
✅ Fas 2 (vendor copy) — bara filkopiering
✅ Fas 3 (Brain statisk) — ren JS+C# port av kända komponenter
✅ Fas 4 (Explorer-fönster) — bygger på Project Explorer som finns
✅ Fas 6 (OllamaAgent) — koden finns, bara säkra anpassningar
✅ Fas 7 (ModelCatalog) — direkt port
✅ Fas 8 (cleanup) — dokumentation
⚠️ Fas 5 (Python server) — kräver att användaren har Python installerat och aktivt godkänner. Tekniken är välkänd men beroenden är komplexa.

**Slutsats**: Jag kan utföra hela planen. Fas 5 är den enda med extern beroende-risk men har tydlig fallback (statisk brain.html från Fas 3).
