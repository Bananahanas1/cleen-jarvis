# Amy Windows Standalone

Amy Windows ar en separat app under `F:\Jarvis-clean\amy-windows`. Den paverkar
inte Jarvis runtime och har egen backend, frontend, databas och `.env`.

## Vad som finns i V1

- FastAPI-backend med setup-status, chat endpoint, task/memory/note-API och
  WebSocket-status.
- SQLite for memory, tasks, notes och run-logg.
- Vite/TypeScript/Three.js dashboard med orb, provider-status och lokala paneler.
- Provider-adapters for Claude, Groq Whisper, ElevenLabs, fal.ai och Playwright.
- Saker default: appen startar utan API-nycklar och visar vad som saknas.
- Playwright browser-agent ar torrkorning som default. Satt
  `AMY_BROWSER_AUTORUN=1` for att tillata riktig browser-korning.

## Installera

```powershell
cd F:\Jarvis-clean\amy-windows
.\Install-AmyWindows.ps1
```

For Playwright Chromium ocksa:

```powershell
.\Install-AmyWindows.ps1 -InstallBrowser
```

## Starta

```powershell
cd F:\Jarvis-clean\amy-windows
.\Start-AmyWindows.ps1
```

Frontend: `http://127.0.0.1:5177`
Backend: `http://127.0.0.1:8787`

## API-nycklar

Kopiera `.env.example` till `.env` och fyll bara i de nycklar du vill anvanda.
Nycklar loggas inte och skickas inte till frontend, bara boolean setup-status.

## Arkitektur

- `backend/app/main.py` - FastAPI app och routes.
- `backend/app/config.py` - `.env`/settings utan secret-lackage.
- `backend/app/database.py` - SQLite schema och store helpers.
- `backend/app/services/providers.py` - Claude/Groq/ElevenLabs/fal status och
  Claude chat-adapter.
- `backend/app/services/browser_agent.py` - Windows-sakrare Playwright adapter.
- `frontend/src/*` - Vite dashboard och Three.js orb.

## Sakerhetsmodell

Amy har egen arbetsyta och skriver bara i `amy-windows/data/` och
`amy-windows/logs/` om du inte bygger vidare pa adapters. Browser-agenten stoppar
vid login, password, payment och publiceringstermer i V1.
