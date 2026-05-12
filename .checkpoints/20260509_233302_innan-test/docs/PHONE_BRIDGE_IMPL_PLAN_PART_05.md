# PHONE_BRIDGE_IMPL_PLAN PART 05

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


const requiredMarkers = [
  "JarvisBridge",
  "EventSource",
  "X-Bridge-Token",
  "X-Device-Fingerprint",
  "/api/message",
  "/api/events"
];

let failures = 0;
for (const m of requiredMarkers) {
  if (!bridgeSource.includes(m)) {
    failures += 1;
    console.log(`FAIL bridge.js missing: ${m}`);
  } else {
    console.log(`PASS bridge.js has: ${m}`);
  }
}

// Auto-detect: WebView2 mode → polyfill is no-op
const ctxWebView = {
  console,
  setTimeout, clearTimeout,
  window: { chrome: { webview: { postMessage(){}, addEventListener(){} } } },
  fetch() {},
  EventSource: function(){},
  localStorage: { getItem(){return null;}, setItem(){} },
  navigator: { userAgent: "Test UA" },
  location: { search: "?t=tok" }
};
ctxWebView.window.window = ctxWebView.window;
ctxWebView.window.localStorage = ctxWebView.localStorage;
ctxWebView.window.navigator = ctxWebView.navigator;
ctxWebView.window.location = ctxWebView.location;
ctxWebView.window.fetch = ctxWebView.fetch;
ctxWebView.window.EventSource = ctxWebView.EventSource;
vm.createContext(ctxWebView);
vm.runInContext(bridgeSource, ctxWebView, { filename: "bridge.js" });

if (!ctxWebView.window.JarvisBridge || !ctxWebView.window.JarvisBridge.mode) {
  failures += 1;
  console.log("FAIL JarvisBridge.mode missing in WebView2 context");
} else if (ctxWebView.window.JarvisBridge.mode !== "webview2") {
  failures += 1;
  console.log(`FAIL expected mode webview2, got ${ctxWebView.window.JarvisBridge.mode}`);
} else {
  console.log("PASS WebView2 mode detected");
}

// HTTP mode: no chrome.webview present → polyfill is active, mode=http
const ctxHttp = {
  console,
  setTimeout, clearTimeout,
  window: { chrome: undefined },
  localStorage: { _s: {}, getItem(k){return this._s[k]||null;}, setItem(k,v){this._s[k]=String(v);} },
  navigator: { userAgent: "Test Phone UA" },
  location: { search: "?t=ttt" },
  fetch: async () => ({ json: async () => ({ ok: true }) }),
  EventSource: function(url){ this.url = url; this.addEventListener=()=>{}; this.close=()=>{}; }
};
ctxHttp.window.window = ctxHttp.window;
ctxHttp.window.localStorage = ctxHttp.localStorage;
ctxHttp.window.navigator = ctxHttp.navigator;
ctxHttp.window.location = ctxHttp.location;
ctxHttp.window.fetch = ctxHttp.fetch;
ctxHttp.window.EventSource = ctxHttp.EventSource;
vm.createContext(ctxHttp);
vm.runInContext(bridgeSource, ctxHttp, { filename: "bridge.js" });

if (ctxHttp.window.JarvisBridge.mode !== "http") {
  failures += 1;
  console.log(`FAIL expected http mode, got ${ctxHttp.window.JarvisBridge.mode}`);
} else {
  console.log("PASS HTTP mode detected when no WebView2");
}

if (typeof ctxHttp.window.chrome !== "object" || typeof ctxHttp.window.chrome.webview?.postMessage !== "function") {
  failures += 1;
  console.log("FAIL chrome.webview.postMessage polyfill missing in HTTP mode");
} else {
  console.log("PASS chrome.webview.postMessage polyfilled in HTTP mode");
}

if (failures > 0) process.exit(1);
```

- [ ] **Step 2: Run test (red)**

```bash
cd /f/Jarvis-clean && node tests/bridge-jsclient.test.js
```

Expected: FAIL — `bridge.js` saknar markers + JarvisBridge.

- [ ] **Step 3: Implement bridge.js**

Ersätt innehållet i `dashboard\bridge.js`:

```javascript
(function() {
  "use strict";

  function readQueryToken() {
    try {
      const m = String(window.location.search || "").match(/[?&]t=([^&]+)/);
      return m ? decodeURIComponent(m[1]) : "";
    } catch { return ""; }
  }

  function getOrCreateClientSalt() {
    let salt = "";
    try {
      salt = window.localStorage.getItem("jarvis_bridge_salt") || "";
      if (!salt) {
        const buf = new Uint8Array(16);
        if (window.crypto && window.crypto.getRandomValues) window.crypto.getRandomValues(buf);
        else for (let i = 0; i < buf.length; i++) buf[i] = Math.floor(Math.random() * 256);
        salt = Array.from(buf).map(b => b.toString(16).padStart(2, "0")).join("");
        window.localStorage.setItem("jarvis_bridge_salt", salt);
      }
    } catch { salt = "fallback"; }
    return salt;
  }

  async function sha256Hex(s) {
    if (window.crypto && window.crypto.subtle) {
      const buf = await window.crypto.subtle.digest("SHA-256", new TextEncoder().encode(s));
      return Array.from(new Uint8Array(buf)).map(b => b.toString(16).padStart(2, "0")).join("");
    }
    // Test environment fallback (no crypto.subtle)
    return "fallback-" + s.length;
  }

  const isWebView2 = !!(window.chrome && window.chrome.webview);
  const messageHandlers = [];

  if (isWebView2) {
    window.JarvisBridge = {
      mode: "webview2",
      send(msg) { window.chrome.webview.postMessage(msg); },
      onMessage(cb) { window.chrome.webview.addEventListener("message", e => cb(e.data)); },
      ready: true
    };
    return;
  }

  // HTTP mode
  const token = readQueryToken() || (window.localStorage.getItem("jarvis_bridge_token") || "");
  if (token) {
    try { window.localStorage.setItem("jarvis_bridge_token", token); } catch {}
  }

  let fingerprintCache = "";

  async function ensureFingerprint() {
    if (fingerprintCache) return fingerprintCache;
    const ua = (window.navigator && window.navigator.userAgent) || "";
    const salt = getOrCreateClientSalt();
    fingerprintCache = await sha256Hex(ua + "||" + salt);
    return fingerprintCache;
  }

  async function send(msg) {
    const fp = await ensureFingerprint();
    const path = msg && msg.type === "jarvis_pending_approval_v1" ? "/api/approval"
               : msg && msg.type === "jarvis_bridge_pair_request_v1" ? "/api/pair/request"
               : "/api/message";

    const resp = await window.fetch(path, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Bridge-Token": token,
        "X-Device-Fingerprint": fp
      },
      body: JSON.stringify(msg)
    });
    if (!resp.ok && resp.status === 401) {
      try { window.localStorage.removeItem("jarvis_bridge_token"); } catch {}
    }
    return resp;
  }

  function startSse() {
    if (!window.EventSource) return;
    const url = "/api/events?t=" + encodeURIComponent(token);
    const es = new window.EventSource(url);
    es.addEventListener("message", e => {
      try {
        const data = JSON.parse(e.data);
        for (const cb of messageHandlers) {
          try { cb(data); } catch {}
        }
      } catch {}
    });
    es.addEventListener("error", () => {
      // browser auto-reconnects
    });
  }

  // Polyfill chrome.webview so existing dashboard code works unchanged
  window.chrome = window.chrome || {};
  window.chrome.webview = window.chrome.webview || {
    postMessage(msg) { send(msg).catch(err => console.error("bridge send failed:", err)); },
    addEventListener(type, cb) {
      if (type === "message") messageHandlers.push(payload => cb({ data: payload }));
    }
  };

  window.JarvisBridge = {
    mode: "http",
    send: msg => send(msg),
    onMessage(cb) { messageHandlers.push(cb); },
    ready: true
  };

  startSse();
})();
```

- [ ] **Step 4: Run test (green)**

```bash
cd /f/Jarvis-clean && node tests/bridge-jsclient.test.js
```

Expected: alla PASS.

- [ ] **Step 5: Inkludera bridge.js i dashboard/index.html**

I `dashboard\index.html`, hitta `<script>`-blocket (rad ~718) och lägg till EXAKT före:

```html
<script src="bridge.js"></script>
<script>
```

- [ ] **Step 6: Commit**

```bash
git add dashboard/bridge.js dashboard/index.html tests/bridge-jsclient.test.js
git commit -m "feat(bridge): add JarvisBridge JS polyfill for chrome.webview"
```

---

## Task 13: Setup-vy i `dashboard/index.html`

Ny "Anslut"-knapp + panel med URL/token + trusted devices-lista.

**Files:**
- Modify: `F:\Jarvis-clean\dashboard\index.html`
- Test: `F:\Jarvis-clean\tests\setup-view.test.js`

- [ ] **Step 1: Skriv marker-test (red)**

`tests\setup-view.test.js`:

```javascript
const fs = require("fs");
const path = require("path");
const html = fs.readFileSync(path.join(__dirname, "..", "dashboard", "index.html"), "utf8");

const markers = [
  'id="setupPanelV1"',
  'id="setupConnectUrl"',
  'id="setupQrCanvas"',
  'id="setupTrustedDevicesList"',
  'id="setupRotateTokenBtn"',
  'id="showSetupBtn"',
  'window.jarvisRenderSetupV1'
];

let fail = 0;
for (const m of markers) {
  if (!html.includes(m)) { fail++; console.log("FAIL missing:", m); }
  else console.log("PASS:", m);
}
if (fail) process.exit(1);
```

- [ ] **Step 2: Run test, verify red**

- [ ] **Step 3: Lägg till knapp i top-row**

I `dashboard\index.html`, hitta `<button id="showVisualBtn"` och lägg till efter den:

```html
        <button id="showSetupBtn" class="panel-button">Anslut</button>
```

- [ ] **Step 4: Lägg till setup-panel HTML**

Hitta `<div id="visualPanel">` och lägg till EFTER hela `</div>`-blocket (omedelbart före `<textarea id="editorArea"`):

```html
      <div id="setupPanelV1" style="display:none; flex:1; min-height:0; flex-direction:column; gap:12px; overflow:auto; background:#03070d; color:#dff7ff; border:1px solid #193247; border-radius:8px; padding:14px;">
        <div>
          <div class="visual-kicker">Anslut telefon</div>
          <h3 class="visual-title">Bridge setup</h3>
        </div>
        <div class="visual-cell">
          <div class="visual-label">Connection URL</div>
          <div id="setupConnectUrl" class="visual-value">Hämtar...</div>
          <button id="setupCopyUrlBtn" type="button" style="margin-top:8px;">Kopiera URL</button>
        </div>
        <div class="visual-cell">
          <div class="visual-label">QR-kod</div>
          <canvas id="setupQrCanvas" width="220" height="220" style="background:#fff;"></canvas>
        </div>
        <div class="visual-cell">
          <div class="visual-label">Trusted devices</div>
          <ul id="setupTrustedDevicesList" style="list-style:none; padding:0; margin:0;"></ul>
        </div>
        <div class="visual-cell">
          <div class="visual-label">Token</div>
          <button id="setupRotateTokenBtn" type="button">Rotera token (kastar ut alla devices)</button>
        </div>
      </div>
```

- [ ] **Step 5: Hooka in showSetupBtn + render-funktion**

I `<script>`-blocket nära andra panel-knappar, lägg till:

```javascript
    const showSetupBtn = document.getElementById("showSetupBtn");
    const setupPanelV1 = document.getElementById("setupPanelV1");
    const setupConnectUrl = document.getElementById("setupConnectUrl");
    const setupQrCanvas = document.getElementById("setupQrCanvas");
    const setupTrustedDevicesList = document.getElementById("setupTrustedDevicesList");
    const setupRotateTokenBtn = document.getElementById("setupRotateTokenBtn");
    const setupCopyUrlBtn = document.getElementById("setupCopyUrlBtn");

    let setupPayloadV1 = null;

    window.jarvisRenderSetupV1 = function(payload) {
      setupPayloadV1 = payload || {};
      const url = String(setupPayloadV1.url || "");
      setupConnectUrl.textContent = url || "(ej tillgänglig)";
