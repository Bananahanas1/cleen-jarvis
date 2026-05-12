# CODEX_HANDOFF_NEXT_LEVEL PART 03

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


**Säkerhetsregler från SUPERPLAN**:
- Hard kill: Ctrl+Shift+Alt+J avbryter ALL desktop-control
- Audit: alla actions till `data/desktop_actions.log` med thumbnail
- Rate limit: 30 actions/min, 200ms mellan
- Blacklist windows: Task Manager, Registry, cmd, PowerShell

## Test-mönster för Codex att följa

Varje ny tool ska ha:
1. **C#-router-test** i `tests/CommandRouterV1.Tests/Program.cs` — slash routing
2. **Node-test** i `tests/<feature>.test.js` — säkerhetschecker (path-traversal, whitelist, etc.)
3. **Build-verifiering**: `dotnet build app/JarvisClean.csproj -c Release` → 0 errors

Mönster:
```javascript
const fs = require("fs"); const path = require("path");
const program = fs.readFileSync(path.join(__dirname, "..", "app", "Program.cs"), "utf8");
let failures = 0;
const markers = ["MyToolName", "MyToolMethod"];
for (const m of markers) { if (!program.includes(m)) { failures++; console.log("FAIL: " + m); } else console.log("PASS: " + m); }
if (failures > 0) process.exit(1);
```

## MD-filer att uppdatera efter varje fas

1. `TODO_NEXT.md` — markera fas KLAR
2. `CURRENT_STATE.md` — lägg till sektion med datum
3. `vault/Decisions/DECISIONS_LOG.md` — logga viktiga beslut
4. `RELEASE_STATUS.md` — uppdaterad funktionsslista
5. `docs/SESSION_LOG.md` — sessionssammanfattning

## Verifierings-checklista per spår

### Efter B
- `dotnet build` → 0 errors
- Alla node + C# tester gröna
- Manuell: chatta 5 turns, se att Jarvis kommer ihåg tidigare frågor
- Manuell: skriv "fixa X i Program.cs" → diff-popup
- Modell-badge syns i chat-svar

### Efter C
- `/sök hello world` → 5 träffar i chat
- `/läs https://example.com` → sammanfattning
- Med wifi av: tydligt fel-meddelande

### Efter D
- `/öppna program notepad` → notepad öppnas
- Försök öppna ej-whitelistat program → blockerat
- `/skärm` → screenshot sparas
- `/desktop på` → bridge startar
- Ctrl+Shift+Alt+J → bridge dödas
- Försök klicka i blacklistad fönster → blockerat

## Beslut Codex behöver fatta (eller fråga användaren)

1. **WebSearcher: DuckDuckGo HTML scraping eller annan metod?** Rekommendation = DuckDuckGo (gratis, ingen API-nyckel).
2. **Vart sparas builds från Builder-mode?** Rekommendation = `vault/builds/<slug>/`
3. **UI-TARS: bygga från source eller använda npm-paket?** Rekommendation = subprocess via `pnpm start` om byggt, annars instruktion till användaren att bygga först.
4. **Multi-turn context: ska VaultSearcher kallas varje turn eller bara första?** Rekommendation = varje turn (minst overhead, mest context).
5. **Modell-routing: visa badge per svar eller dölja?** Rekommendation = visa.

## Kontakt-punkter med befintlig kod

- `Program.cs` `AskOllamaAsync` (rad ~4380) — multi-model + multi-turn + vault-context (vault klart, multi-model + multi-turn behövs)
- `Program.cs` `HandleMessageAsync` — naturligt-språk-edit hookas in före Ollama
- `Program.cs` `_activeModel` field — Override för ModelRouter
- `app/CommandRouterV1.cs` — alla nya slash-kommandon (B/C/D)
- `app/Brain/VaultSearcher.cs` — pattern för cached lookup
- `app/Bridges/NeuroLinkedBridge.cs` — pattern för subprocess-bridge

## Säkerhetsregler från användarens preferenser (`vault/Memory/Azu_preferences.md`)

- Inga lösenord i logs/chat/markdown
- F:\New project är read-only-referens
- All filskrivning via PendingApprovalV1
- Ingen fri F-disk-write-access
- Allt på svenska (default)

## Tidsestimat för Codex

- Spår B: 6 timmar (fördelat på 2-3 sessions)
- Spår C: 2 timmar
- Spår D: 10 timmar (fördelat på 3-4 sessions, sist eftersom mest komplext)

**Totalt**: ~18 timmar för Codex att slutföra Spår B+C+D.

## Vad som INTE ska göras

- Ändra `F:\New project\*` (utom ARCHIVED.md som finns)
- Auto-aktivera desktop-control (kräver explicit `/desktop på`)
- Skapa nya separata Windows-fönster (ETT program-regeln)
- Ta bort PendingApproval-skydd för någon write-path
- Bypass UI-TARS säkerhetsblacklists
- Lägga in API-nycklar i kod

## Slut

Användaren får läsa BÄDA dokumenten:
- `JARVIS_NEXT_LEVEL_SUPERPLAN.md` — översikt + säkerhet + beslut
- `CODEX_HANDOFF_NEXT_LEVEL.md` (denna) — exakt kod-skiss + nästa steg

Codex kan börja med Spår B1 (ModelRouter — minst risk, mest värde direkt).
