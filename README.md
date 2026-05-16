# Jarvis-clean / cleen-jarvis

`cleen-jarvis` är huvudprodukten. Lokal arbetsmapp:

```text
F:\Jarvis-clean
```

GitHub-repot är källan som externa AI-agenter (ChatGPT/Codex etc) läser från.
Lokala ändringar måste pushas för att vara synliga för dem.

## Starta

```text
F:\Jarvis-clean\Starta-Jarvis.vbs
```

Eller publicerad release: `F:\Jarvis-clean\dist\Jarvis.exe`

## Läs detta (i ordning)

1. **`MASTER_PLAN.md`** — den enda aktiva källan. Allt börjar här.
2. **`AGENTS.md`** — säkerhetsregler för AI-agenter.
3. **`CURRENT_STATE.md`** — vad som fungerar nu.
4. **`TODO_NEXT.md`** — vad som är öppet.

Det är allt. Andra .md-filer är delplaner — läs dem när `MASTER_PLAN.md`
pekar dit. Äldre planning-sprawl ligger i `archive/`.

## Produktroller

- `cleen-jarvis` — huvudprodukt. Stabil, builds, tester, push.
- `F:\New project` — **read-only referens**. Får aldrig ändras.
- `f-drive-projects` — referens, backup, inspiration.

## Viktiga regler

- Ändra aldrig `F:\New project`.
- Ge aldrig Jarvis fri skrivåtkomst till hela F-disken.
- Lokala kommandon hanteras före Ollama/LLM.
- Filskrivning/append/delete/terminal/desktop-control kräver `PendingApprovalV1`.
- Alla `.md`-filer ska vara under ~14 000 tecken.
