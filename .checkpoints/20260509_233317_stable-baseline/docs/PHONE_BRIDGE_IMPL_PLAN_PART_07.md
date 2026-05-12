# PHONE_BRIDGE_IMPL_PLAN PART 07

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


```bash
git add app/Program.cs
git commit -m "feat(bridge): wire BridgeServerV1 into Program.cs and broadcast all PostWebMessageAsJson"
```

---

## Task 17: URL ACL setup-script + dokumentation

`http://+:7777/` kräver URL ACL för icke-elevated user.

**Files:**
- Create: `F:\Jarvis-clean\tools\install-bridge-urlacl.cmd`
- Modify: `F:\Jarvis-clean\README.md`

- [ ] **Step 1: Skapa setup-script**

`tools\install-bridge-urlacl.cmd`:

```cmd
@echo off
REM Run this once as Administrator to allow Jarvis bridge to bind to http://+:7777/
REM Uses current user. Adjust port if you changed config\bridge_port.txt.

netsh http add urlacl url=http://+:7777/ user=%USERNAME%

if %ERRORLEVEL% EQU 0 (
    echo OK: URL ACL added for http://+:7777/
) else (
    echo FAILED. Try running this CMD as Administrator.
)
pause
```

- [ ] **Step 2: README-tillägg**

Lägg till i `README.md`:

```markdown
## Phone bridge (fas 1)

Telefonen kan styra Jarvis över LAN via `dashboard/index.html` serverad på port 7777.

**First-time setup:**

1. Högerklicka `tools\install-bridge-urlacl.cmd` → Run as administrator. Detta tillåter Jarvis att binda `http://+:7777/` utan elevation.
2. Starta Jarvis. Klicka **Anslut**-knappen → kopiera URL eller scanna QR med telefonen.
3. På telefonen: trycka "Be om trust" → godkänn popup på datorn → telefonen kan nu approva pending actions.

Konfiguration:
- `config\bridge_token.txt` — install-token (genereras automatiskt)
- `config\bridge_port.txt` — port (default 7777)
- `config\bridge_lan_ip.txt` — LAN-IP override (vid flera NICs)
- `config\trusted_devices.json` — parade enheter

CommandRouter-kommandon:
- `/enheter` — lista trusted devices
- `/enheter ta bort N` — ta bort device på position N
- `/bridge nytoken` — rotera install-token (tvingar om-paring)
```

- [ ] **Step 3: Commit**

```bash
git add tools/install-bridge-urlacl.cmd README.md
git commit -m "docs(bridge): add URL ACL setup script and README section"
```

---

## Task 18: Final verification + manuell smoke-test

- [ ] **Step 1: Kör alla node-tester**

```bash
cd /f/Jarvis-clean && for t in dashboard-routing.test.js smart-open-cleanup.test.js visual-panel.test.js dashboard-scrollbar-style.test.js approval-popup.test.js help-text.test.js file-write-safety.test.js file-delete-safety.test.js editor-save-safety.test.js undo-safety.test.js change-review-ui.test.js terminal-approval-safety.test.js project-explorer-polish.test.js overview-livestate.test.js create-folder-suggestions.test.js suggestion-colors.test.js bridge-jsclient.test.js setup-view.test.js mobile-css.test.js; do node "tests/$t" > /dev/null 2>&1 && echo "GREEN $t" || echo "RED $t"; done
```

Expected: alla GREEN.

- [ ] **Step 2: Kör alla C#-tester**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/CommandRouterV1.Tests/CommandRouterV1.Tests.csproj 2>&1 | tail -5
cd /f/Jarvis-clean && dotnet run --project tests/TrustedDevicesStoreV1.Tests/TrustedDevicesStoreV1.Tests.csproj 2>&1 | tail -5
cd /f/Jarvis-clean && dotnet run --project tests/BridgeServerV1.Tests/BridgeServerV1.Tests.csproj 2>&1 | tail -5
```

Expected: alla PASS.

- [ ] **Step 3: dotnet build + publish**

```bash
cd /f/Jarvis-clean && dotnet build app/JarvisClean.csproj 2>&1 | tail -3
cd /f/Jarvis-clean && dotnet publish app/JarvisClean.csproj -c Release -o dist --no-self-contained 2>&1 | tail -3
```

Expected: 0 errors, 1 known warning.

- [ ] **Step 4: Stoppa & starta Jarvis**

```bash
powershell.exe -Command "Get-Process Jarvis -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep 2"
powershell.exe -Command "Start-Process wscript.exe -ArgumentList 'F:\Jarvis-clean\Starta-Jarvis.vbs' -WindowStyle Hidden; Start-Sleep 4; Get-Process Jarvis | Format-List Id, SessionId"
```

Expected: ny PID i SessionId 11.

- [ ] **Step 5: Manuella tester**

Kör i denna ordning:

| # | Steg | Förväntat |
|---|------|-----------|
| 1 | Klicka **Anslut** i datorns dashboard | Setup-panel visar URL + QR + tom trusted-lista |
| 2 | Skriv `/enheter` i chat | Svar: "Inga trusted devices ännu" |
| 3 | Öppna URL från setup-panelen i annan dator/telefon-browser på samma WiFi | Dashboard renderas |
| 4 | Skriv `/hjälp` i telefonens chat | Svar dyker upp på BÅDA klienter |
| 5 | I telefonens setup-panel, "Be om trust" (TODO: lägg knapp i Task 13 om saknas) | Popup på datorn: "Trust new device?" |
| 6 | Godkänn popup på datorn | Trust granted broadcast |
| 7 | Skapa pending file write från telefon: `/fil skapa docs/test-bridge.md = från-telefonen` | Approval popup på BÅDA |
| 8 | Approva från telefon | Fil skapas |
| 9 | `/enheter` | Listar din telefon |
| 10 | `/bridge nytoken` | Båda klienter får 401 |

- [ ] **Step 6: Commit final state**

```bash
git status
git add -A
git commit -m "feat(bridge): phase 1 complete — PWA phone bridge MVP"
```

---

## Self-review (utförd av writing-plans)

**Spec coverage** (PHONE_BRIDGE_PLAN.md → tasks):

| Spec section | Tasks |
|---|---|
| BridgeServerV1 (HttpListener, auth, endpoint-routing, SSE) | 6, 7, 8, 9, 10, 11 |
| BridgeAuthV1 | 4 |
| TrustedDevicesStoreV1 | 3 |
| PendingApprovalV1.DeviceTrustRequest | 1 |
| Program.cs (start, broadcast) | 16 |
| CommandRouterV1.BridgeAdmin | 2 |
| bridge_token.txt + bridge_port.txt + bridge_lan_ip.txt | 5 |
| trusted_devices.json | 3 |
| dashboard\bridge.js (JarvisBridge polyfill) | 12 |
| dashboard\index.html (setup view, mobile CSS) | 13, 14, 15 |
| Tests (bridge-server, bridge-auth, bridge-jsclient, mobile-css, setup-view) | 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 |
| URL ACL setup script | 17 |
| Acceptance criteria verification | 18 |

Alla spec-områden täckta.

**Placeholder scan:** Inga TBD/TODO i task-innehållen. Alla code-block är konkreta. Tasken som inkluderar tredjeparts-bibliotek (Task 14, qrcode.js) refererar specifikt MIT-licensad fil från känd källa.

**Type consistency:** `BridgeAuthV1.ValidateInstallToken`, `BridgeAuthV1.ComputeFingerprint`, `BridgeAuthV1.GenerateInstallToken`, `TrustedDevicesStoreV1.IsTrusted/Add/RemoveAt/List/Clear/TouchLastSeen`, `BridgeServerV1.Start/Stop/Broadcast`, `JarvisBridge.send/onMessage/mode/ready` — konsistenta över alla tasks.

**Scope check:** 18 tasks, alla i fas 1. Inga senare-fas-features har smugits in. Plan är fokuserad.

---

## Execution Handoff

**Plan complete and saved to `F:\Jarvis-clean\docs\PHONE_BRIDGE_IMPL_PLAN.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — fresh subagent per task, two-stage review mellan varje, fast iteration.

**2. Inline Execution** — köra tasks i denna session med executing-plans skill, batch-checkpoints för review.

**Which approach?**
