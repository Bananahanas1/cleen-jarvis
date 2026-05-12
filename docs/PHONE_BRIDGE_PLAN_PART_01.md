# PHONE_BRIDGE_PLAN PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

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
