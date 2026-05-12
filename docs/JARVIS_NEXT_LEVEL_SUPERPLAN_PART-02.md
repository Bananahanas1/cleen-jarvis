# JARVIS_NEXT_LEVEL_SUPERPLAN_PART-02.md

Del 2 till `docs/JARVIS_NEXT_LEVEL_SUPERPLAN.md`.

## Risker och mitigeringar

| Risk | Mitigering |
|------|------------|
| UI-TARS klickar fel ställe -> förstör data | Pending approval per klick, default = Avbryt, hard-kill Ctrl+Shift+Alt+J |
| Desktop-control används med admin-fönster aktivt | Detect admin-process via `IsUserAnAdmin()`, blockera i såna fall |
| Multi-turn context exploderar token-budget | Hård cap 8000 tecken, summary-fallback efter 20 turns |
| Auto-routing väljer fel modell -> konstigt svar | Override via `/modell byt X`, badge visar vald modell |
| Web-search returnerar skräp -> AI hallucinerar | Visa källor i svaret, låt användaren se vilka URL:er som lästes |
| Builder-läge skapar 50 filer i ett svep | Steg-för-steg approval per fil, summary efter varje 5 filer |
| Naturligt-språk-edit missförstår -> fel rad ändras | Diff-popup måste godkännas, "ångra senaste"-knapp efter |
