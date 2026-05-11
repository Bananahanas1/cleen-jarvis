# PHONE_BRIDGE_PLAN.md — Fas 1: PWA Phone Bridge MVP

Skapad: 2026-05-07
Status: Spec klar för implementation-plan
Owner: Azu

## Megaplan-kontext

Det här är **fas 1 av 6** för "Jarvis i telefonen". Den övergripande riktningen är att telefonens app **ÄR** Jarvis dashboard (samma `dashboard/index.html`) serverad över LAN, så att uppdateringar av Jarvis automatiskt ger uppdaterad mobilversion utan separat APK-pipeline.

| Fas | Vad | Storlek | Läge |
|---|---|---|---|
| **1** | **PWA bridge MVP (LAN, text, approval)** | medel | **DEN HÄR SPECEN** |
| 2 | TLS + PWA-install + mobile UX-polish | medel | senare |
| 3 | Voice via Web Speech API | liten | senare |
| 4 | Per-device certs + ev. mTLS | medel | senare |
| 5 | Internet-relay (cloudflared/Tailscale) | liten | senare |
| 6 | Push-notiser + offline-cache | medel | senare |

Varje fas ska brainstormas → speccas → planeras → implementeras separat. Det här dokumentet handlar bara om fas 1.

## Mål för fas 1

Leverera *smallest thing that works* så att Azu kan:

1. Öppna en URL i Chrome på telefonen (samma WiFi som datorn).
2. Pareras som "trusted device" via popup på datorn.
3. Skriva i chat-rutan på telefonen → samma routing som chat-rutan i WebView2 dashboard på datorn.
4. Få push av pending approvals till telefonen.
5. Godkänna riskabla actions från telefonen (när enheten är trusted).
6. Layout är **läsbar** på smal skärm — inte polerat.

Funktionellt feature-paritet med dashboard-chat; visuellt: minimal responsive pass.

## Out of scope för fas 1

| Feature | Fas |
|---|---|
| Voice / Web Speech API | 3 |
| TLS / HTTPS | 2 |
| PWA install (manifest + service worker) | 2 |
| Push-notiser (Web Push) | 6 |
| Mobile UX-redesign (bottom nav, swipe-gestures, polerade modaler) | 2 |
| File-edit polerat för phone-keyboard | 2 |
| Internet-relay | 5 |
| Multi-user / shared accounts | aldrig (single-user by design) |
| Offline-cache av state på phone | senare |
| Native Android-app (Kotlin/Flutter) | aldrig (PWA-vägen valdes uttryckligen i brainstorming) |

## Arkitektur-översikt

```
[Telefon Browser]            [LAN HTTP]            [Jarvis-clean PC]
                                                ┌────────────────────────────┐
                                                │  Jarvis.exe (WinForms)     │
                                                │  ┌──────────────────────┐  │
                                                │  │  WebView2 (lokal)    │  │ ← desktop UI (oförändrat)
                                                │  │  dashboard/index.html│  │
                                                │  └──────────┬───────────┘  │
                                                │             │              │
                                                │  ┌──────────▼───────────┐  │
                                                │  │  Bridge dispatcher   │  │ ← samma routing
                                                │  │  CommandRouterV1     │  │
                                                │  │  PendingApprovalV1   │  │
                                                │  │  ToolRegistryV1      │  │
                                                │  └──────────▲───────────┘  │
                                                │             │              │
┌──────────────┐  HTTP+SSE       ┌───────────────┴──────────────┴───────────┐ │
│ Chrome       │ ──POST /api───▶ │  BridgeServerV1 (HttpListener, :7777)    │ │
│ dashboard.html│ ◀─SSE events── │  Auth-middleware + endpoint-routing      │ │
│ (HTTP-mode)  │                │                                          │ │
└──────────────┘                └──────────────────────────────────────────┘ │
                                                │                            │
                                                └────────────────────────────┘
```

**Designprincip: ENA kodbas.** `dashboard/index.html` är samma fil som serveras lokalt till WebView2 (oförändrat) OCH över HTTP till telefonen. Ny tunn JS-modul `bridge.js` upptäcker transport och anpassar sig.

**Data flow när telefonen skickar `/hjälp`:**

1. Phone JS: `JarvisBridge.send({type:"jarvis_chat_user_v1", text:"/hjälp"})`
2. JS upptäcker `!window.chrome?.webview` → POST `/api/message` med token i header
3. C# `BridgeServerV1` validerar token + känner igen device → routar till **samma** handler som befintliga `WebView_WebMessageReceived`
4. CommandRouterV1 → CommandValidatorV1 → tool execution → svar
5. C# postar svar till SSE event-bussen → broadcast till alla anslutna klienter (telefon + desktop dashboard)
6. Phone JS får event via `EventSource` → renderar i chat

## Komponenter

| Plats | Fil | Status | Vad |
|---|---|---|---|
| C# | `app\BridgeServerV1.cs` | NY | `HttpListener` på `0.0.0.0:7777`, token-auth-middleware, endpoint-routing, SSE broadcaster |
| C# | `app\BridgeAuthV1.cs` | NY | install-token + per-device trust check + device fingerprint hashing |
| C# | `app\TrustedDevicesStoreV1.cs` | NY | Läs/skriv `config\trusted_devices.json` (atomic write via temp + rename) |
| C# | `app\PendingApprovalV1.cs` | ÄNDRAD | Ny `PendingApprovalTypeV1.DeviceTrustRequest` |
| C# | `app\Program.cs` | ÄNDRAD | Starta `BridgeServerV1` vid app-start; broadcasta alla `PostWebMessageAsJson`-meddelanden via bridgen också |
| C# | `app\CommandRouterV1.cs` | ÄNDRAD | Ny `CommandIntent.BridgeAdmin` med subcommands (`devices.list`, `devices.remove`, `token.rotate`) |
| Config | `config\bridge_token.txt` | NY | install-token (32 byte base64), genereras vid första start om saknas |
| Config | `config\bridge_port.txt` | NY | port (default 7777, kan ändras) |
| Config | `config\bridge_lan_ip.txt` | NY (valfri) | LAN-IP override för QR/URL i setup-vyn; om saknas → auto-detect |
| Config | `config\trusted_devices.json` | NY | lista med device fingerprints + metadata |
| Data | `data\bridge.log` | NY | enkel append-logg för bridge events (auth fail, pair, token rotation) |
| JS | `dashboard\bridge.js` | NY | `JarvisBridge`-modul: auto-detect WebView2 vs HTTP+SSE, exponerar `send` + `onMessage` |
| JS | `dashboard\index.html` | ÄNDRAD | importerar `bridge.js`; alla `chrome.webview.postMessage` / `addEventListener("message",...)` ersätts av `JarvisBridge.send` / `JarvisBridge.onMessage` |
| CSS | `dashboard\index.html` | ÄNDRAD | nytt `@media (max-width: 800px)`-block med vertikal stack |
| UI | `dashboard\index.html` | ÄNDRAD | ny "Anslut"-knapp/panel med QR + URL + trusted-devices-lista |
| Tests | `tests\bridge-server.test.js` | NY | smoke-test mot HttpListener (kör server i task, gör HTTP-anrop) |
| Tests | `tests\bridge-auth.test.js` | NY | token-validation + trusted/untrusted approval-beslut |
| Tests | `tests\bridge-jsclient.test.js` | NY | `JarvisBridge` auto-detect, HTTP+SSE fallback, `bootstrap_v1` |
| Tests | `tests\mobile-css.test.js` | NY | media-query-block finns + panel-stack-regler |
| Tests | `tests\setup-view.test.js` | NY | QR-rendering + URL display + trusted-device-list |
| Tests | `tests\CommandRouterV1.Tests\Program.cs` | UTÖKAD | DeviceTrustRequest, `/enheter`-kommandon, `/bridge nytoken` |

**Inga ändringar i:** `ToolRegistryV1.cs` (förutom ev. ny tool entry för bridge-admin), `CommandValidatorV1.cs`. Alla befintliga 19 node-tester ska fortsätta vara gröna eftersom WebView2-pathen är oförändrad — bridge är ett **additivt** lager.

## API-yta (HTTP)

| Method | Path | Auth | Syfte |
|---|---|---|---|
| `GET` | `/` | install-token via query eller header | dashboard.html (statisk) |
| `GET` | `/bridge.js`, statiska assets | install-token | resurser |
| `GET` | `/api/health` | install-token | ping; returnerar `{ok: true, version: "..."}` |
| `POST` | `/api/message` | install-token | telefonens user-input → routar till samma handler som `WebView_WebMessageReceived` |
| `GET` | `/api/events` | install-token | Server-Sent Events stream — broadcast av allt som idag går via `PostWebMessageAsJson` + `bootstrap_v1` vid connect |
| `POST` | `/api/approval` | install-token + **trusted-device** | godkänn/avbryt en pending approval |
| `POST` | `/api/pair/request` | install-token | skapa `PendingApprovalTypeV1.DeviceTrustRequest` |

**Payload-form för `POST /api/message`:** exakt samma JSON-shape som `chrome.webview.postMessage` skickar idag. Återanvänd C# message handler utan ändring.

**SSE event-format:** `data: <json>\n\n` där json = exakt samma shape som `PostWebMessageAsJson` skickar idag. Plus ett initialt `bootstrap_v1`-event vid connect som innehåller: active editor file, current pending approval, latest terminal payload, latest file change review.

## Pairing-flow (DeviceTrustRequest)

```
Phone                              Jarvis-clean PC
  │                                       │
  │  1. öppnar http://192.168.1.42:7777   │
  │     med ?t=<install-token>            │
  ├──────────────────────────────────────▶│ ansluter, SSE öppnas
  │                                       │ device fingerprint = SHA256(UA + saltedRandom from phone localStorage)
  │                                       │ → marked as untrusted, sparas inte än
  │                                       │
  │  2. klicka "Be om trust" i app-menyn  │
  ├──── POST /api/pair/request ──────────▶│
  │                                       │ skapa PendingApprovalV1 med Type = DeviceTrustRequest
  │                                       │ broadcast popup-event till alla SSE-klienter
  │  ◀── popup på BÅDA klienter ─────────│
  │                                       │
  │                                       │ User klickar "Godkänn" på desktop
  │                                       │ TrustedDevicesStoreV1.Add(fingerprint, name, addedAt)
  │                                       │ broadcast: device-trusted event
  │  ◀── trust granted ──────────────────│
  │                                       │
  │  3. nu kan telefonen approva pending  │
  │     actions: knappen syns i UI lokalt │
```

**`config\trusted_devices.json`-shape:**

```json
{
  "devices": [
    {
      "id": "a3b2f1...",
      "name": "Pixel-7 Chrome",
      "addedAt": "2026-05-07T12:34:00Z",
      "lastSeen": "2026-05-07T18:00:00Z"
    }
  ]
}
```

**Hantering på datorn:** nya CommandRouter-intents:
- `/enheter` → lista trusted devices
- `/enheter ta bort 1` → ta bort device på position 1
- `/bridge nytoken` → rotera install-token (alla devices avrustas, måste re-paras)

## Auth + säkerhet

**install-token:** 32 byte base64, sparas i `config\bridge_token.txt` (file ACL = current user). Krävs för att alls ansluta. Skickas som query-param `?t=...` (initialt) eller header `X-Bridge-Token` (efter setup, sparas i phone localStorage). Rotate via `/bridge nytoken`.

**device fingerprint:** SHA256(User-Agent || installSalt) där `installSalt` är random 32-byte sparat i phone localStorage vid första anslutning. Persistent men ej hard-spoof-resistant — fas 1-säkerhet räcker.

**trust-status:** kollas i C# innan **alla** approval-endpoints. Otrustad device som POSTar `/api/approval` → 403 + log entry.

**`DeviceTrustRequest`-undantag:** Den enda pending approval som otrusted devices **inte** kan approva är ironiskt nog `DeviceTrustRequest` själv (chicken-and-egg). En `DeviceTrustRequest` som triggers från phone X kan bara godkännas från en redan trusted device — i fas 1 betyder det desktop-WebView2 (som alltid räknas som trusted via lokal IPC, inte HTTP). Phone X får se popup:en men knappen `Godkänn` är dold där.

**TLS:** **HTTP only i fas 1**, plain text på LAN. Voice (Web Speech API) kräver HTTPS — flyttat till fas 2 där self-signed cert sätts upp.

**Bind address:** `0.0.0.0:7777` (alla interfaces inkl LAN). Skydd: token-auth räcker när du är på betrott LAN.

**Risker (för medvetet val):**

- **Plain HTTP på Wi-Fi:** någon på samma nät kan sniffa token. Mitigering: TLS i fas 2. Acceptans: scope = ditt eget hemnät.
- **Token i URL/query:** kan hamna i browser history, screenshot. Mitigering: efter första laddning sparas i localStorage; URL:en behöver inte token i query för subsequent requests. Token rotation manuellt.
- **Stulen telefon:** trust-bit kvar. Mitigering: `/enheter ta bort` på datorn återkallar trust.
- **localStorage-clear på phone:** trust nollas, måste re-pareras. Acceptabelt.
- **Trafficsiffror:** alla anslutna klienter får alla SSE-events. Privacy-känsligt om någon parad untrusted device hänger kvar. Mitigering: untrusted devices får också see-events (de ser ju ändå dashboard) men kan inte approva. Inga separata privacy-tier.

## Mobile CSS-omfattning (fas 1)

Single-pass under `@media (max-width: 800px)` i `dashboard/index.html`:

- 3-panel grid (`Project Explorer | Workspace | Chat`) → **vertikal stack**: Workspace överst, Chat under, Project Explorer i en hamburger-toggle bottom-sheet.
- Approval-popup: full-width, padding för thumb-target, knappar nederst (sticky).
- Terminal-panel: `overflow-x: auto` + minskat `min-height`.
- Suggestions/autocomplete: större font, mer tap-area-padding.
- Setup-panelen ("Anslut") får också mobil-vänlig layout direkt när den byggs.

Inte med: bottom-nav, swipe-gester, polerad mobil file-editor, polerad diff-viewer (= fas 2).

## Discovery + setup-vy

- Ny knapp **"Anslut"** i top-row (eller liten ikon i hörnet) → öppnar setup-panel i mitten-ytan.
- Setup-panelen innehåller:
  - **QR-kod** med `http://<lan-ip>:7777/?t=<install-token>` (renderas av tiny inline qrcode-funktion i `bridge.js`, ingen extern CDN).
  - **Klartextsversion** av URL + token för manuell inmatning.
  - **Knapp "Kopiera URL"**.
  - **Lista trusted devices** med last-seen + "Ta bort"-knapp.
  - **Knapp "Rotera token"** (varnar att alla devices avrustas).
- Setup-panelen visar bara **primär** LAN-IP via `Dns.GetHostAddresses` filtrerat på `AddressFamily.InterNetwork` och icke-loopback. På maskiner med flera NICs (VPN, Hyper-V, VirtualBox) kan fel IP hamna först — fas 1 hanterar det med en konfigurerbar override `config\bridge_lan_ip.txt` (om filen finns används det, annars auto).

## Error / edge cases

- **Phone offline mid-action:** SSE auto-reconnects med exponentiell backoff. Banner i UI: "Återansluter…".
- **Server restart:** klienter får 0-byte from SSE → reconnect → server skickar `bootstrap_v1`-event med initial state-snapshot.
- **Concurrent edits:** desktop + phone editar samma fil → existing editor-save-pending-flow gäller. Den som sparar sist får "filen ändrats sedan du öppnade"-notice (befintligt skydd).
- **Token rotated:** alla anslutna klienter får 401 → bridge.js rensar localStorage → setup-vy med "ny pairing krävs".
- **Approval race:** desktop & phone trycker Godkänn samtidigt → `PendingApprovalStoreV1.TryConsume(id)` returnerar true bara för första; andra får `Pending redan utfört`.
- **BridgeServer kraschar:** körs i egen `Task.Run` med try/catch + auto-restart. Loggar till `data\bridge.log`.
- **Skadad `trusted_devices.json`:** parse-error → tom lista, varning i log, **ingen** auto-overwrite (användaren måste re-para).
- **Port 7777 upptagen:** Jarvis loggar fel + fallback till random ledig port mellan 7777-7799 + visar i setup-vyn.

## Testing strategy

**Befintliga tester (19):** alla ska fortsätta vara gröna eftersom WebView2-pathen är oförändrad. `bridge.js` läses in i alla node-tester men är no-op när `window.chrome.webview` finns (vilket testen mockar idag).

**Nya C#-tester (CommandRouterV1.Tests utökad):**
- `DeviceTrustRequest` skapas korrekt vid `/api/pair/request`-handler
- otrusted device → 403 på `/api/approval`
- `/enheter`, `/enheter ta bort 1`, `/bridge nytoken` route lokalt
- token rotation invalidatar trusted-lista

**Nya node-tester:**
- `tests\bridge-server.test.js` — HttpListener: status codes, token-validation, 404, 405
- `tests\bridge-auth.test.js` — trusted vs untrusted device beslut
- `tests\bridge-jsclient.test.js` — `JarvisBridge` auto-detect (mocka `window.chrome.webview`), HTTP+SSE fallback, bootstrap_v1
- `tests\mobile-css.test.js` — media-query-block finns + panel-stack-regler
- `tests\setup-view.test.js` — QR-rendering finns + URL display + trusted-device-list

**Manuella tester (efter fas 1-leverans):**
- Setup-vy visar QR + URL på datorn
- Annan device på samma WiFi öppnar URL → dashboard renders → token validerat
- Phone skickar `/hjälp` → svar dyker upp på BÅDA klienter
- Phone trycker "Be om trust" → popup på datorn → godkänn → trusted
- Skapa pending file write från phone → popup på BÅDA → godkänn på phone → fil skrivs
- Rotera token → båda devices kastas ut → re-pairing krävs

## Acceptance criteria för fas 1

1. `dotnet build` 0 errors med BridgeServerV1 + ändringar.
2. Alla 19 befintliga node-tester gröna + 5 nya gröna + utökade C#-router-tester gröna.
3. `dotnet publish` lyckas; ny `Jarvis.exe` startar BridgeServer på `:7777`.
4. Setup-vy renderar QR + URL korrekt.
5. Phone på samma WiFi kan öppna URL → se dashboard → skicka chat → få svar.
6. Phone kan be om trust → popup på desktop → godkänn → phone kan approva.
7. Mobile layout renderar utan horizontal scroll på 360px-bred viewport.
8. Token rotation kastar ut alla devices.
9. Inga regressioner i WebView2 desktop-pathen (alla manuella scenarios från `TODO_NEXT.md` fungerar oförändrat).

## Öppna frågor (för senare faser)

- TLS-cert-distribution till phone (fas 2): self-signed → måste accepteras manuellt en gång, eller mkcert-rotad lokal CA?
- PWA install-prompt UX (fas 2): automatisk vs manuell `beforeinstallprompt`-hantering.
- Voice språkmodell (fas 3): Web Speech API är cloud (Google), Whisper.wasm är offline men 30+MB. Tradeoff senare.
- Push-notiser via VAPID-keys (fas 6): VAPID-key-management.
- Internet-relay (fas 5): Tailscale (kräver konto) vs cloudflared (publik tunnel) vs eget WireGuard.

Dessa är **inte** en del av fas 1-implementation och ska inte påverka fas 1-kod.

## Referenser

- `docs\JARVIS_LONG_TERM_VISION.md` — Capability Layer 1-7
- `docs\VISUAL_PANEL_PLAN.md` — panel-baserad UI-arkitektur
- `app\PocketBridge\` — tidigare experimentellt försök, exkluderat från compile-scope; behåll som referens men bygg om Jarvis-native enligt denna spec
- `webview2-dotnet-reference.md` (vault) — WebView2 .NET API-patterns
- `ollama-api-reference.md` (vault) — Ollama HTTP API (oförändrat i fas 1)
