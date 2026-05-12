# PHONE_BRIDGE_PLAN PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


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
