# PHONE_BRIDGE_IMPL_PLAN PART 06

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


      // Render QR
      try { renderQrV1(setupQrCanvas, url); } catch (e) { console.error(e); }

      setupTrustedDevicesList.innerHTML = "";
      const devices = setupPayloadV1.devices || [];
      devices.forEach(function(d, idx) {
        const li = document.createElement("li");
        li.style.padding = "6px 0";
        li.style.borderBottom = "1px solid #193247";
        const label = document.createElement("span");
        label.textContent = (d.name || "Unknown") + " (sedd " + (d.lastSeen || "?") + ")";
        const btn = document.createElement("button");
        btn.textContent = "Ta bort";
        btn.style.marginLeft = "8px";
        btn.addEventListener("click", function() {
          postMessage({ type: "jarvis_bridge_devices_remove_v1", index: idx });
        });
        li.appendChild(label);
        li.appendChild(btn);
        setupTrustedDevicesList.appendChild(li);
      });
    };

    showSetupBtn.addEventListener("click", function() {
      visualPanel.style.display = "none";
      editorArea.style.display = "none";
      diffViewer.style.display = "none";
      setupPanelV1.style.display = "flex";
      workspaceTitle.textContent = "Anslut";
      postMessage({ type: "jarvis_bridge_setup_request_v1" });
    });

    setupRotateTokenBtn.addEventListener("click", function() {
      if (confirm("Rotera token? Alla anslutna enheter måste paras om.")) {
        postMessage({ type: "jarvis_bridge_token_rotate_v1" });
      }
    });

    setupCopyUrlBtn.addEventListener("click", async function() {
      try { await navigator.clipboard.writeText(setupConnectUrl.textContent || ""); } catch {}
    });

    // QR-rendering läggs till i Task 14
    function renderQrV1(canvas, text) {
      // placeholder — implementeras i Task 14
      const ctx = canvas.getContext && canvas.getContext("2d");
      if (!ctx) return;
      ctx.fillStyle = "#fff";
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = "#000";
      ctx.font = "10px monospace";
      ctx.fillText("(QR placeholder)", 10, 20);
    }
```

- [ ] **Step 6: Run test, verify green**

```bash
cd /f/Jarvis-clean && node tests/setup-view.test.js
```

Expected: alla PASS.

- [ ] **Step 7: Commit**

```bash
git add dashboard/index.html tests/setup-view.test.js
git commit -m "feat(bridge): add setup view with URL/QR/trusted devices skeleton"
```

---

## Task 14: QR-kod-rendering (inline tiny lib)

Ersätt `renderQrV1`-placeholder med riktigt QR-kod via en minimal QR-lib inline. Använd biblioteket `qrcode-generator` (~10KB) inkluderat i `bridge.js` eller ny `qrcode.js`.

**Files:**
- Create: `F:\Jarvis-clean\dashboard\qrcode.js` (kopia av qrcode-generator MIT-licensad lib, eller minimal inline implementation)
- Modify: `F:\Jarvis-clean\dashboard\index.html` (importera qrcode.js, byt ut renderQrV1)
- Test: utöka `tests\setup-view.test.js`

- [ ] **Step 1: Lägg in qrcode.js**

Ladda ner `qrcode-generator/qrcode.js` (kazuhikoarase, MIT) eller använd `qr-creator` (small inline). För hobby-projekt OK att copy-paste hela. Spara till `dashboard\qrcode.js`. Kommentar i toppen som anger källa + licens.

(Som alternativ: skriv en mini-impl själv. För det här planen rekommenderas att importera ett välkänt bibliotek då fas 1 inte ska experimentera med kryptografisk QR-encoding.)

- [ ] **Step 2: Importera i index.html**

```html
<script src="qrcode.js"></script>
<script src="bridge.js"></script>
```

- [ ] **Step 3: Byt ut renderQrV1**

Ersätt placeholder-versionen:

```javascript
function renderQrV1(canvas, text) {
  if (!text || !window.qrcode) return;
  const qr = window.qrcode(0, "L");
  qr.addData(text);
  qr.make();
  const modules = qr.getModuleCount();
  const ctx = canvas.getContext("2d");
  const size = canvas.width;
  const cell = Math.floor(size / modules);
  ctx.fillStyle = "#fff";
  ctx.fillRect(0, 0, size, size);
  ctx.fillStyle = "#000";
  for (let r = 0; r < modules; r++) {
    for (let c = 0; c < modules; c++) {
      if (qr.isDark(r, c)) ctx.fillRect(c * cell, r * cell, cell, cell);
    }
  }
}
```

- [ ] **Step 4: Test QR markers**

I `tests\setup-view.test.js`, lägg till markers:

```javascript
markers.push("qrcode.js");
markers.push("qr.addData");
```

- [ ] **Step 5: Run + commit**

```bash
git add dashboard/qrcode.js dashboard/index.html tests/setup-view.test.js
git commit -m "feat(bridge): render QR code in setup view"
```

---

## Task 15: Mobile CSS @media block

**Files:**
- Modify: `F:\Jarvis-clean\dashboard\index.html`
- Test: `F:\Jarvis-clean\tests\mobile-css.test.js`

- [ ] **Step 1: Skriv marker-test (red)**

`tests\mobile-css.test.js`:

```javascript
const fs = require("fs");
const path = require("path");
const html = fs.readFileSync(path.join(__dirname, "..", "dashboard", "index.html"), "utf8");

const markers = [
  "@media (max-width: 800px)",
  "flex-direction: column",
  "id=\"projectExplorerHamburgerV1\""
];

let fail = 0;
for (const m of markers) {
  if (!html.includes(m)) { fail++; console.log("FAIL:", m); }
  else console.log("PASS:", m);
}
if (fail) process.exit(1);
```

- [ ] **Step 2: Run test, verify red**

- [ ] **Step 3: Lägg till @media i index.html**

I `<style>`-blocket nära slutet, lägg till:

```css
    @media (max-width: 800px) {
      body {
        grid-template-columns: 1fr;
        grid-template-rows: auto auto auto;
      }
      .nav, .editor, .chat {
        flex-direction: column;
      }
      #projectExplorerHamburgerV1 {
        display: block;
      }
      aside.nav {
        display: none;
      }
      aside.nav.mobile-open {
        display: block;
      }
      .approval-dialog {
        max-width: 100%;
        width: 100%;
        margin: 0;
        border-radius: 0;
      }
      #suggestions {
        font-size: 14px;
      }
      .tree-row {
        padding: 8px 6px;
      }
      .panel-button, button {
        min-height: 40px;
      }
    }
```

- [ ] **Step 4: Lägg till hamburger-knapp ovanför mid-area**

I top-row eller editor-headern:

```html
<button id="projectExplorerHamburgerV1" type="button" style="display:none;">≡</button>
```

JS:

```javascript
const projectExplorerHamburgerV1 = document.getElementById("projectExplorerHamburgerV1");
projectExplorerHamburgerV1.addEventListener("click", function() {
  const aside = document.querySelector("aside.nav");
  if (aside) aside.classList.toggle("mobile-open");
});
```

- [ ] **Step 5: Run + commit**

```bash
git add dashboard/index.html tests/mobile-css.test.js
git commit -m "feat(bridge): mobile CSS @media block with vertical stack and hamburger"
```

---

## Task 16: Wire-up i `Program.cs` — starta BridgeServer + broadcast PostWebMessageAsJson

**Files:**
- Modify: `F:\Jarvis-clean\app\Program.cs`

- [ ] **Step 1: Hitta WebView2-init-platsen**

Leta `EnsureCoreWebView2Async`-anropet (omkring rad 75-90).

- [ ] **Step 2: Lägg till BridgeServer-start efter WebView2-init**

I main form / startup, efter att WebView2 är ready men före Navigate, lägg till:

```csharp
        var installToken = BridgeConfigV1.GetOrCreateInstallToken();
        var bridgeAuth = new BridgeAuthV1(installToken);
        var bridgeTrusted = new TrustedDevicesStoreV1(BridgeConfigV1.TrustedDevicesPath());
        var bridgePort = BridgeConfigV1.GetPort();
        var dashboardDir = @"F:\Jarvis-clean\dashboard";

        _bridgeServer = new BridgeServerV1(
            port: bridgePort,
            auth: bridgeAuth,
            trusted: bridgeTrusted,
            onMessage: (json, fingerprint) =>
            {
                // Routa till samma pipeline som WebView2
                try
                {
                    return HandleWebMessageFromBridge(json, fingerprint);
                }
                catch (Exception ex) { return "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}"; }
            },
            bootstrapState: BuildBootstrapStateV1,
            staticRoot: dashboardDir
        );
        _bridgeServer.Start();
```

(`_bridgeServer` deklareras som privat field i Form-klassen; stoppa i `OnFormClosing`.)

- [ ] **Step 3: Implement HandleWebMessageFromBridge**

Lägg till metod som tar JSON, parsar samma sätt som WebView_WebMessageReceived, och returnerar svar:

```csharp
private string HandleWebMessageFromBridge(string json, string fingerprint)
{
    // Återanvänd existerande dispatch-logik. Den synkrona returvärdet är tunnt;
    // de flesta state-uppdateringar sker via PostWebMessageAsJson + Broadcast.
    using var doc = JsonDocument.Parse(json);
    var type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

    // Pair-request
    if (type == "jarvis_bridge_pair_request_v1")
    {
        var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.DeviceTrustRequest,
            Title = "Trust new device?",
            Target = fingerprint,
            Preview = name + " (" + fingerprint.Substring(0, 8) + "...)",
            RequiresUserApproval = true
        });
        BroadcastPendingApprovalV1();
        return "{\"pending\":true}";
    }

    // Övriga: dispatcha synkront via samma path som WebView2 hade
    DispatchWebMessageSync(json);
    return "{\"ok\":true}";
}
```

(`DispatchWebMessageSync` är en ny privat metod som extraherar dispatch-koden ur befintliga `WebView_WebMessageReceived` så den kan anropas från båda hållen.)

- [ ] **Step 4: Bygga om PostWebMessageAsJson så det också broadcastar**

Hitta wrapper-metoden för `webView.CoreWebView2.PostWebMessageAsJson(...)` (eller skapa en):

```csharp
private void SendToAllClientsV1(string json)
{
    try { webView.CoreWebView2.PostWebMessageAsJson(json); } catch { }
    try { _bridgeServer?.Broadcast(json); } catch { }
}
```

Ersätt alla direkta `webView.CoreWebView2.PostWebMessageAsJson(...)`-anrop i Program.cs med `SendToAllClientsV1(...)`.

- [ ] **Step 5: BuildBootstrapStateV1**

```csharp
private string BuildBootstrapStateV1()
{
    var state = new
    {
        type = "jarvis_bootstrap_v1",
        activeFile = LatestActiveFilePathV1 ?? "",
        latestTerminal = LatestTerminalPayloadV1Json ?? "{}",
        pendingApproval = PendingApprovalStoreV1.Get() is null ? null : new {
            type = PendingApprovalStoreV1.Get()!.Type.ToString(),
            title = PendingApprovalStoreV1.Get()!.Title,
            target = PendingApprovalStoreV1.Get()!.Target
        }
    };
    return JsonSerializer.Serialize(state);
}
```

- [ ] **Step 6: OnFormClosing → stop bridge**

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    try { _bridgeServer?.Stop(); } catch { }
    base.OnFormClosing(e);
}
```

- [ ] **Step 7: Build + verify 0 errors**

```bash
cd /f/Jarvis-clean && dotnet build app/JarvisClean.csproj 2>&1 | tail -3
```

Expected: 0 errors. Rätta refactor-kompileringsfel iterativt.

- [ ] **Step 8: Commit**
