# JARVIS_NEXT_LEVEL_SUPERPLAN.md — From assistent till AI-medbyggare

Skapad: 2026-05-10
Driver: användarens vision om att Jarvis ska kunna konversera fritt, koda projekt från idéer, söka på nätet, öppna program och styra skärmen.

## Vad användaren vill ha

### Småfix (A) — direkt
- **A1.** Klick på fil-nod i Brain → öppna filen direkt i editor (utan extra inspector-popup)
- **A2.** Ta bort recently-modified-ringarna (störande)
- **A3.** "Bygg om"-knappen känns inert — visa tydligt vad som händer
- **A4.** Auto-promote: när användaren skriver `kom ihåg X` ska minnet sparas i både `data/memory.md` OCH `vault/auto/<datum>-<topic>.md`

### Conversational Jarvis (B) — multi-model + context + naturligt språk
- **B1.** Multi-model orchestration: enkla frågor → fast (qwen3:1.7b), komplext → smart (qwen3:8b), kod → coder (qwen2.5-coder:7b), djupt → reason (deepseek-r1:7b). Auto-routing baserat på query-analys.
- **B2.** Multi-turn context: Jarvis kommer ihåg samtalshistoriken (sliding window 10-20 meddelanden + memory + vault).
- **B3.** Naturligt språk → kod-edit: "gå in i filen X och uppdatera Y till Z" → Jarvis läser, föreslår diff, väntar på godkännande.
- **B4.** "Builder-läge": "bygg en webbsida som visar väder för Stockholm" → Jarvis bollar idéer, frågar vidare, skapar filerna stegvis med approval.

### Internet-sökning (C)
- **C1.** Web-search via DuckDuckGo eller liknande (offline-graceful)
- **C2.** Web-fetch: hämta innehåll från en URL och sammanfatta
- **C3.** Slash `/sök <query>` + naturligt "googla X"

### Desktop-kontroll (D) — UI-TARS-integration
- **D1.** Öppna program: "öppna VS Code", "starta Spotify"
- **D2.** Skärm-capture: Jarvis ser vad som finns på skärmen
- **D3.** Klick/typ-kontroll: Jarvis kan interagera med GUI:n
- **D4.** Bygg på `F:\UI-TARS-desktop-main\` (ByteDances Multimodal AI Agent stack med GUI Agent, Vision, MCP-tools)

---

## UI-TARS — vad det är, hur det integreras

`F:\UI-TARS-desktop-main\` är ByteDance's open-source-stack:
- **Agent TARS** (`multimodal/agent-tars/`): CLI + Web UI, multimodal LLM-agent, MCP-tools
- **UI-TARS Desktop** (`apps/ui-tars/`): native desktop GUI agent (Electron-app)
- **GUI Agent SDK** (`multimodal/gui-agent/agent-sdk/`): bibliotek för GUI-automation
- **Operators** (`operator-browser`, `operator-nutjs`, `operator-adb`): konkreta klick/typ-implementationer

Kärnmodellen är **UI-TARS** (https://github.com/bytedance/UI-TARS) — en vision-LLM som tar screenshots och genererar GUI-actions (click x,y / type text / scroll).

**Två integrationsmodeller**:

**Model 1 — Subprocess** (lättare, ~3 dagar)
- Jarvis kör UI-TARS Desktop som extern process
- IPC via stdin/stdout eller HTTP-bridge
- Ingen kompilering av TypeScript-stacken — bara installera och kalla
- Bra för: "öppna VS Code", "klicka skicka-knappen"
- Säkerhet: vi kontrollerar bara command, UI-TARS gör jobbet

**Model 2 — Eget mini-bibliotek** (tyngre, ~2 veckor)
- Plocka ut bara `operator-nutjs` (cross-platform input via Node.js NUT.js)
- Skriv egen vision-loop med Ollama eller OpenAI vision
- Mer kontroll men kräver mycket TypeScript→C# bridge
- Bra om vi inte vill bero på hela Electron-appen

**Rekommendation**: börja med Model 1 (subprocess) eftersom det är snabbast till värde. Migrera till Model 2 senare om det blir mycket använt.

---

## Säkerhetsregler för desktop-kontroll

UI-TARS kan klicka, skriva, läsa skärmen — det är **mycket farligare** än vanlig filskrivning. Hårda regler:

1. **Defaultkonfiguration: AV** — ingen desktop-control förrän användaren explicit aktiverar via `/desktop på`
2. **Approval per action** — varje klick/typ visar pending popup (visa thumbnail + action), default-knapp är "Avbryt"
3. **Whitelist/blacklist**:
   - Whitelist: Notepad, VS Code, browser, Spotify
   - Blacklist: Task Manager (kan döda processer), Registry Editor, PowerShell, cmd, Settings
4. **Skärm-capture**: bara på explicit `/skärm` eller "ta skärmdump nu", aldrig kontinuerligt
5. **No-secrets-recording**: skip OCR i fönster med titel som matchar `password|api|key|token|secret|wallet`
6. **Hard kill switch**: Ctrl+Shift+Alt+J avbryter ALL desktop-control omedelbart
7. **Audit log**: varje action loggas i `data/desktop_actions.log` med screenshot-thumbnail
8. **Rate limit**: max 30 actions/minut, måste pausa 200ms mellan actions
9. **Sandboxning**: UI-TARS-process körs som child av Jarvis och kan dödas

---

## Faser

### Spår A — Småfix (~45 min)

**A1. Klick på projektfil → öppna direkt**
- Modifiera click-handler i brain-vyn: om `n.source === "project"` → posta `jarvis_open_file_smart` direkt + visa Files-läget
- Behåll inspector för vault-noder (de har inte direkt-fil-koppling)
- Skip dubbel-klick-zoom för projektfiler — klick är klick

**A2. Ta bort recently-modified-ringar**
- Radera `ringMeshes`-koden i Brain
- Behåll `mtimeMin` i Node-payload för framtida bruk (i inspector "● Nyligen ändrad")

**A3. "Bygg om" — bättre feedback**
- När knapp klickas: visa "Bygger om..." + scrambla nodernas positioner så force-layout kör visuellt på nytt
- Räknare i status-cellen "Senaste rebuild: HH:MM, antal noder: X"

**A4. Auto-promote `kom ihåg`**
- I `SaveSmartMemory`: efter `File.AppendAllText(memoryPath)` → även skapa `vault/auto/<yyyyMMdd-HHmmss>-<sanitizedSummary>.md` med frontmatter
- VaultSearcher.InvalidateIndex() så det syns direkt i Brain

**Verifiering A**: alla node-tester gröna, manuell test av varje punkt.

---

### Spår B — Conversational Jarvis (~6h)

#### B1. Multi-model orchestration (~2h)

Skapa `app/Brain/ModelRouter.cs`:
```csharp
public static string PickModelForQuery(string query, int turnDepth)
{
    var len = query.Length;
    var hasCode = Regex.IsMatch(query, @"\b(klass|funktion|metod|fil|kod|fix|bug|implementera|refaktorera)\b", RegexOptions.IgnoreCase);
    var hasPlan = Regex.IsMatch(query, @"\b(planera|design|arkitektur|tänk|borde|hur ska)\b", RegexOptions.IgnoreCase);

    if (hasCode) return ModelCatalog.Coder.Name;        // qwen2.5-coder:7b
    if (hasPlan || len > 200) return ModelCatalog.Smart.Name;  // qwen3:8b
    if (turnDepth > 5) return ModelCatalog.Smart.Name;  // dialog → smart
    return ModelCatalog.Fast.Name;                      // qwen3:1.7b
}
```

Override via `_activeModel` (om användaren bytt manuellt med `/modell byt X`).

UI-feedback: chat-meddelande visar liten badge `[fast]`/`[smart]`/`[code]` så användaren ser val.

#### B2. Multi-turn context (~1h)

Idag skickar `AskOllamaAsync` bara `system + userMessage`. Bygg in:
- Sliding window 10 senaste turn (5 user + 5 assistant)
- Lagra i `_conversationHistory` static field
- Trimma om totalt > 8000 tecken (behåll först + senaste)
- Slash `/historik` visar
- Slash `/glöm samtal` rensar

#### B3. Naturligt språk → kod-edit (~2h)

Användaren skriver: *"gå in i app/Program.cs och ändra OllamaUrl till https://example"*

Pipeline:
1. **Intent-classifier** (regex): känn igen "gå in i / öppna / fixa / ändra / uppdatera / refaktorera" + ".cs/.md/.js"-fil
2. **Extract**: filnamn + beskrivning av ändring
3. **Read** filen (read_file via OllamaAgentHarness)
4. **Generate diff** (Ollama coder-modell prompts: "här är filen, här är önskad ändring, returnera unified diff")
5. **Show pending approval popup** med diffen (existerande PendingApprovalV1 men för file write)
6. **På godkänn**: tillämpa diff, kör test om finns

Konkret: ny intent `NaturalCodeEdit`, ny tool `NaturalEditTool`.

#### B4. Builder-läge (~1h)

Slash `/bygg <beskrivning>` eller naturligt "bygg en webbsida för X":
- Steg 1: Jarvis ställer 3-5 klargörande frågor ("vad ska det heta?", "vilka sidor?", "responsive?")
- Steg 2: Genererar projekt-skiss som `vault/builds/<slug>/PLAN.md`
- Steg 3: Användaren godkänner planen
- Steg 4: Jarvis skapar filerna en åt gången via PendingApproval

**Verifiering B**: kan be Jarvis "förklara vad bin gör i Program.cs", svaret kommer från coder-modellen, inkluderar fil-content. Skriv "bygg ett TODO-app" → Jarvis ställer frågor och börjar.

---

### Spår C — Internet-sökning (~2h)

Använd `python/jarvis_web_agent.py` som redan finns (porterad från gamla):

**C1. Web-search**
- Slash `/sök <query>` → Python kallar DuckDuckGo, returnerar topp 5 träffar
- Naturligt "googla X" / "sök på X"
- I AskOllamaAsync: om query innehåller `?` och pekar utåt ("vad är", "vem är", "när hände"), auto-trigga web-search och inkludera topp-3 i system-prompt

**C2. Web-fetch + summary**
- Slash `/läs <url>` → hämta sida, extrahera text, sammanfatta med smart-modellen
- Cache i `data/web_cache/<hash>.txt` (24h TTL)

**C3. Offline-graceful**
- InternetProbe redan finns — använd `IsInternetOnlineCachedAsync()`
- Om offline: returnera "Internet saknas, kan inte söka. Kör vanlig chat istället."

---

### Spår D — Desktop-kontroll via UI-TARS (~10h, faseras)

#### D1. Process-bridge mot UI-TARS Desktop (~3h)

`app/Bridges/UiTarsBridge.cs`:
- Detect: är `F:\UI-TARS-desktop-main\apps\ui-tars\` byggd? Annars visa instruktion.
- Start subprocess vid `/desktop på` (inte default!)
- IPC via Node.js HTTP-server (port 9999) som UI-TARS exposes
- Stop vid main close

#### D2. Öppna program (~1h)

Slash `/öppna program <namn>`:
- Whitelist-lookup: `notepad`, `vscode`, `chrome`, `spotify`, `explorer`
- Använd `Process.Start` för whitelistade
- Allt annat → blockerat med felmeddelande

#### D3. Skärm-capture (~2h)

Slash `/skärm` → screen capture via `System.Drawing.Bitmap` + `Graphics.CopyFromScreen`
- Spara till `data/screenshots/<timestamp>.png`
- Ingen automatisk OCR — bara fil-pekare + thumbnail
- Skicka till multimodal LLM (UI-TARS-modellen) på explicit begäran "tolka skärmen"

#### D4. Klick/typ via UI-TARS (~4h)

Naturligt: "klicka på Skicka-knappen i Outlook":
1. Skärm-capture
2. Skicka till UI-TARS-vision: "find Send button, give me click coordinates"
3. Visa **pending approval popup** med thumbnail + förslagen action ("klick @ x=812, y=445")
4. På godkänn: använd UI-TARS operator för klicket
5. Loggla i `data/desktop_actions.log`

**Verifiering D**: aktivera `/desktop på`, säg "öppna Notepad", popup visas, godkänn, Notepad öppnas. `Ctrl+Shift+Alt+J` stänger ner allt.

---

## Filer som ska skapas/ändras

| Fil | Spår | Ny/ändrad |
|------|------|-----------|
| `dashboard/index.html` | A1, A2, A3 | ändra (brain-click + ringar + bygg-om-knapp) |
| `app/Program.cs` | A4, B1, B2, B3, B4, C, D | ändra (många ställen) |
| `app/Brain/ModelRouter.cs` | B1 | NY |
| `app/Brain/ConversationHistory.cs` | B2 | NY |
| `app/Brain/NaturalEditTool.cs` | B3 | NY |
| `app/Brain/BuilderMode.cs` | B4 | NY |
| `app/Brain/WebSearcher.cs` | C1, C2 | NY |
| `app/Bridges/UiTarsBridge.cs` | D1 | NY |
| `app/Desktop/DesktopController.cs` | D2, D3, D4 | NY |
| `app/Desktop/SafeAppLauncher.cs` | D2 | NY (whitelist) |
| `app/Desktop/ScreenCapture.cs` | D3 | NY |
| `app/CommandRouterV1.cs` | A, B, C, D | ändra (nya intents) |
| `vault/Decisions/DECISIONS_LOG.md` | alla | ändra (logga beslut) |
| `vault/Issues/Desktop_safety.md` | D | NY |

---

## Tidsuppskattning + ordning

| Spår | Tid | Värde | Risk |
|------|-----|-------|------|
| A — Småfix | 45 min | Direkt UX-förbättring | Låg |
| B1 — Multi-model | 2h | Smart auto-routing | Låg |
| B2 — Multi-turn context | 1h | Jarvis "minns" | Låg |
| C — Internet-sök | 2h | Jarvis kan söka | Låg-medel |
| B3 — NL kod-edit | 2h | "gå in i filen och fixa" | Medel |
| B4 — Builder-läge | 1h | Bygg från idé | Medel |
| D2 — Öppna program | 1h | Lätt, säker subset | Låg |
| D3 — Skärm-capture | 2h | Vision-grund | Medel |
| D1 — UI-TARS-bridge | 3h | Komplex extern | Hög |
| D4 — Klick/typ via UI-TARS | 4h | Full GUI-kontroll | **Hög** |

**Totalt: ~18h** (kan delas i 5-6 sessions)

**Rekommenderad ordning**:
1. Spår A (45 min) — direkt värde
2. Spår B1 + B2 (3h) — bättre conversational AI
3. Spår C (2h) — internet
4. Spår B3 + B4 (3h) — Jarvis kan koda
5. Spår D2 + D3 (3h) — säker desktop-grund
6. Spår D1 + D4 (7h) — full UI-TARS-integration sist

---

## Beslut som krävs

1. **UI-TARS subprocess vs eget mini-bibliotek**: rekommendation = subprocess (snabbare). OK?
2. **Desktop-control default OFF eller ON?**: rekommendation = OFF, opt-in via `/desktop på`. OK?
3. **Multi-turn-context-storlek**: 10 turns / 8000 tecken — räcker?
4. **Builder-läge: spara projekt under `vault/builds/<slug>/` eller egen mapp**?
5. **Web-search: DuckDuckGo (gratis, ingen nyckel) eller Google (kräver API-nyckel)?** Rekommendation = DuckDuckGo.

---

## Risker och mitigeringar

| Risk | Mitigering |
|------|------------|
| UI-TARS klickar fel ställe → förstör data | Pending approval per klick, default = Avbryt, hard-kill Ctrl+Shift+Alt+J |
| Desktop-control används med admin-fönster aktivt | Detect admin-process via `IsUserAnAdmin()`, blockera i såna fall |
| Multi-turn context exploderar token-budget | Hård cap 8000 tecken, summary-fallback efter 20 turns |
| Auto-routing väljer fel modell → konstigt svar | Override via `/modell byt X`, badge visar vald modell |
| Web-search returnerar skräp → AI hallucinerar | Visa källor i svaret, låt användaren se vilka URL:er som lästes |
| Builder-läge skapar 50 filer i ett svep | Steg-för-steg approval per fil, summary efter varje 5 filer |
| Naturligt-språk-edit missförstår → fel rad ändras | Diff-popup måste godkännas, "ångra senaste"-knapp efter |

---

## När vi börjar — beslutsfråga

**Tre möjliga första-passes**:

**Path X — Bara småfixar** (~45 min): A1+A2+A3+A4. Säkert, direkt värde.

**Path Y — Conversational baseline** (~4h): A + B1 + B2 + B3 (multi-model + context + naturligt-språk-edit). Jarvis blir mycket smartare.

**Path Z — Allt** (~18h över flera sessioner): hela superplanen.

Säg vilken path du vill köra (X / Y / Z) eller om du vill ändra ordningen, så börjar jag bygga.
