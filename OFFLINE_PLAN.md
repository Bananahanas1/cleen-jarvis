# Offline Plan — Jarvis utan internet

Senast uppdaterad: 2026-05-04
Ägare: nästa AI-agent som tar offline-arbetet.

## Mål

Jarvis ska kunna starta, chatta, köra agentverktyg och visa dashboarden
**helt utan internetanslutning**. Allt som faktiskt kräver internet
(webbsök, väder, nyheter, OpenAI) ska antingen vara avstängt eller
falla tillbaka till tydliga felmeddelanden — aldrig hänga UI:t.

Acceptanstest (kör med Wi-Fi avstängd):

1. `Starta-Jarvis.vbs` ger ett fönster utan timeout-fel.
2. NeuroLinked-dashboarden öppnas (eller safe-mode-läget visas).
3. Chat → "hej" → Ollama svarar på svenska inom 10 s.
4. Agentläge → `kod: lista filer i app/` → fungerar.
5. Webbverktyg svarar artigt: "Internet saknas, hoppar över."
6. Inga 30+ s blockerande väntningar, inga DNS-timeouter i loggen.

## Vad som redan fungerar offline

| Område | Status | Anledning |
|--------|--------|-----------|
| Ollama qwen3:1.7b chat | OK | localhost:11434, modell finns lokalt |
| Ollama agent-tool-calling | OK | samma sak |
| Lokala plugins | OK | `plugins/*.json` läses från disk |
| SQLite-minne | OK | `app/Core/ChatDatabase.cs` |
| Graphify-graf | OK | `graphify-out/graph.json` är förinläst |
| Obsidian-läsning | OK | filsystem `F:\New project\obsidian-vault\` |
| WinForms-UI | OK | renderas direkt av Windows |

## Vad som idag bryter offline-läget

### B1 — Dashboard-vendor (Three.js m.m.)

`neurolinked/dashboard/index.html` har en importmap mot `/vendor/three.module.js`
och `/vendor/`. Verifiera:

- Servar `neurolinked/server.py` `/vendor/*` från lokal disk?
- Finns `three.module.js` faktiskt nedladdat någonstans i repot?
- Om JS-importerna 404:ar går dashboarden in i frys-loop tidigare beskriven.

**Åtgärd:** committa nödvändiga vendor-JS-filer (Three.js + addons) till
`neurolinked/dashboard/vendor/` och se till att `server.py` mountar mappen.
Verifiera med `curl http://127.0.0.1:8000/vendor/three.module.js` offline.

### B2 — TTS/STT laddar ner modeller vid första användning

- `python/local_tts.py` (Piper) hämtar röstfiler från GitHub.
- `python/local_stt.py` (faster-whisper) laddar ner modellvikter via
  Hugging Face vid första anropet.

**Åtgärd:** kör en *online-warmup* en gång (skript i `python/offline_prepare.py`)
som tvingar ned alla modellfiler till `%LOCALAPPDATA%\jarvis\models`.
Vid offline-start ska Piper/whisper hitta cachen och inte göra någon nätverksrequest.
Lägg en check i `local_tts.py` / `local_stt.py` som returnerar tydligt fel
om cachen saknas, istället för att hänga på socket-timeout.

### B3 — Webbverktyg blockerar agenten när nät saknas

Verktyg som idag försöker nätverkskall:
- `web_search`, `web_fetch`, `download_file` (`AgentTools.cs`)
- `jarvis_weather` (Open-Meteo), `jarvis_news` (DuckDuckGo)

**Åtgärd:** lägg en gemensam offline-probe i `AgentTools.cs`
(t.ex. en cachad TCP-koll mot `1.1.1.1:443` med 800 ms timeout).
När den misslyckas:
- markera tools som temporärt otillgängliga,
- returnera direkt `"Internet saknas just nu, hoppar över sökningen."`,
- agenten får svaret i sin tool-loop och kan fortsätta utan att blockera.

### B4 — OpenAI/Codex opt-in-paths

Idag är OpenAI bara opt-in via prefix (`openai:`, `openai agent:`).
Men `OpenAiResponsesClient` kan fortfarande triggas om en användare
skriver `openai:` i offline-läget.

**Åtgärd:** låt `JarvisRuntimePolicy` exponera `RequireOnline()` som
kontrolleras innan OpenAI/Codex-kall. Om offline → svara
`"OpenAI/Codex kräver internet. Skickar till Ollama istället."` och routa
om till `RunOllamaAsync`.

### B5 — Dashboard-bootstrap väntar på WebSocket som inte kommer

`completeDashboardStartup` har redan en REST-fallback-timer på 8 s,
men om hela NeuroLinked-servern är nere fortsätter loading-skärmen.
Safe-mode (`JARVIS_SAFE_DASHBOARD = true` i `index.html`) löser detta
tillfälligt genom att hoppa över 3D helt.

**Åtgärd:** behåll safe-mode som permanent fallback för svaga datorer
**och** offline-läget. Lägg till en knapp i WinForms `JarvisForm`:
"Öppna dashboard i safe mode" → laddar `index.html?safe=1` och tvingar
flaggan via URL-parameter istället för const.

### B6 — Pip-paket vid första uppstart

Om en användare klonar repot och kör `run.py` på en dator där pip-paketen
inte är installerade krävs internet för att hämta dem.

**Åtgärd:** publicera `requirements.lock` och en `wheels/`-mapp med förbyggda
hjul, så `pip install --no-index --find-links wheels/ -r requirements.lock`
fungerar utan PyPI. Hjul behövs för Windows-x64, Python 3.11.

### B7 — WebView2 Evergreen-runtime

WebView2 kontrollerar uppdateringsservrar i bakgrunden. Det är inte
blockerande för UI:t men det är trafik. Ofarligt offline (failar tyst),
men dokumentera så framtida agenter inte tror det är en bugg.

## Implementeringsfaser

### Fas 1 — Diagnos (ca 30 min)

- [ ] Kör `nslookup` → bekräfta att inget domännamn slås upp i offline-test.
- [ ] `netstat -ano` medan Jarvis startar offline → identifiera alla
      utgående anslutningar som faktiskt försöker.
- [ ] Logga varje sådan anslutning i `MEMORY/SESSION_LOG.md`.

### Fas 2 — Vendor + cache (B1, B2, B6)

- [ ] Lägg `neurolinked/dashboard/vendor/three.module.js` (+ addons).
- [ ] Lägg `python/offline_prepare.py` som warmup-skript.
- [ ] Lägg `wheels/` + `requirements.lock`.
- [ ] Verifiera offline-uppstart från ren Windows-användarprofil.

### Fas 3 — Graceful degradation (B3, B4, B5)

- [ ] `AgentTools.OfflineProbe()` med 800 ms timeout, cachad i 30 s.
- [ ] Alla web-tools kollar probe före nätverksanrop.
- [ ] OpenAI-prefix routar om till Ollama när offline.
- [ ] WinForms-knapp för safe-mode-dashboard.

### Fas 4 — Smoke-test offline

- [ ] Stäng av Wi-Fi.
- [ ] Kör `Starta-Jarvis.vbs`.
- [ ] Verifiera alla 6 acceptanspunkter i toppen av filen.
- [ ] Uppdatera `CURRENT_STATE.md` med slutligt offline-status.

### Fas 5 — Dokumentera (en pass)

- [ ] Lägg `OFFLINE.md` på repo-roten med användarinstruktion ("kör
      detta en gång med internet, sedan kan du köra utan").
- [ ] Uppdatera `MEMORY/DECISIONS.md` med offline-arkitekturbeslut.
- [ ] Stryk denna fil eller markera som RESOLVED när allt är klart.

## Beröringsregler

- **Ändra inte `build.ps1`** utan att uppdatera P2-bug-noten i
  `MEMORY/OPEN_ISSUES.md` (third_party-kopiering är redan trasig).
- **Ändra inte C# `JarvisForm` runtime-läge** utan att verifiera
  `dotnet run --project tests\JarvisApp.Tests` passerar grönt.
- **Rör inte hjärnminnet** (`brain_state`, `neurolinked/data/`) i offline-fixen.
- All offline-logik ska vara *tydligt* avstängningsbar: en flagga,
  inte spridda if-satser.

## Risker

- TTS/STT-modeller är stora (Piper-röst ≈ 60 MB, whisper-small ≈ 460 MB).
  Om de inte cachas tar offline-läget bort röstfunktionerna helt.
  Acceptabelt om text-chatten fungerar.
- Three.js + addons är ~1 MB extra i repot. Acceptabelt jämfört med
  dagens frys-bugg.
- Offline-probe kan ge falskt positiv (lokal nätverk men ingen DNS).
  Lägg en sekundär probe mot Ollama (`localhost:11434`) som primär
  hälsoindikator — Jarvis kräver ändå Ollama oavsett internet.
