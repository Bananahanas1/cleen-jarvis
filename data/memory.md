# Jarvis lokalt minne

## Importerat från gammal memory.txt

2026-05-04 05:26:12 | mitt Project Jarvis

## 2026-05-04 05:28:13
mitt minne sparas nu som markdown

## 2026-05-04 05:30:00
Jarvis sparar nu minne i markdown

## 2026-05-04 06:15:12

Type: note
Importance: 3/5
Tags: smart-memory

Text:
Jarvis har nu Smart Memory format

## 2026-05-04 06:17:13

Type: important
Importance: 5/5
Tags: important

Text:
Jarvis ska aldrig spara hemliga uppgifter

## 2026-05-04 06:17:34

Type: project_fact
Importance: 4/5
Tags: project

Text:
Jarvis-clean ska byggas steg för steg

## 2026-05-04 06:29:09

Type: note
Importance: 3/5
Tags: smart-memory

Text:
text

## 2026-05-05 00:51:40

Type: project_fact
Importance: 4/5
Tags: project

Text:
Offline Codex Fas 3 fungerar: Jarvis kan skapa pending changes, godkänna rubrikändringar, skapa backup och arkivera förslaget.

## 2026-05-05 01:04:15

Type: project_fact
Importance: 4/5
Tags: project

Text:
Checkpoint-systemet fungerar. Jarvis kan skapa och lista checkpoints innan större ändringar.

## 2026-05-05 02:49:51

Type: project_fact
Importance: 4/5
Tags: project

Text:
Jarvis filverktyg har nu read-only och writable filtyper. .sln blockeras vid skrivning och .csproj kan läsas.

## 2026-05-05 05:07:17

Type: important
Importance: 5/5
Tags: important

Text:
minne:

## 2026-05-05 05:07:23

Type: important
Importance: 5/5
Tags: important

Text:
minne

## 2026-05-05 05:39:33

Type: project_fact
Importance: 4/5
Tags: project

Text:
Jarvis-clean har nu 3-panelslayout, filpanel, Project Explorer, autocomplete/TAB-förslag och spärr mot tomma minnes-/sökkommandon. Nästa stora steg är CommandRouter V1 och hård säkerhetsvalidering för alla kommandon innan Ollama får meddelanden.

## 2026-05-05 06:37:59

Type: project_fact
Importance: 4/5
Tags: project

Text:
Octogent och claude-coworker-model ska användas som inspiration men inte integreras direkt ännu. Först ska Jarvis-clean få CommandRouter V1, CommandValidator och säker pending/godkännande för filskrivning. Senare ska vi bygga Jarvis Task Workspace och Worker Delegation inspirerat av dessa projekt.

## 2026-05-05 06:54:37

Type: project_fact
Importance: 4/5
Tags: project

Text:
Externa inspirationslänkar sparas i docs/REFERENCE_PROJECTS.md. Octogent och claude-coworker-model är reference only. Octogent-länk ska vara utan mcp_token. LLM Guy-länk saknas tills användaren skickar exakt URL.

## 2026-05-05 07:01:39

Type: project_fact
Importance: 4/5
Tags: project

Text:
CommandRouter-research finns i docs/COMMAND_ROUTER_RESEARCH.md. Jarvis ska bygga CommandRouter V1 med CommandIntent, CommandRisk, CommandResult, CommandValidator, ToolRegistry och PendingApproval innan fler stora features.

## 2026-05-05 07:04:23

Type: project_fact
Importance: 4/5
Tags: project

Text:
CommandRouter V1 skeleton är skapad i app/CommandRouterV1.cs med CommandIntent, CommandRisk, CommandResult och CommandRouterV1.Parse. Ingen runtime-logik har ändrats ännu.

## 2026-05-05 07:12:05

Type: project_fact
Importance: 4/5
Tags: project

Text:
Framtida Project Explorer ska kunna ha flera roots på F-disken. Jarvis-clean är huvudprojektet, F:\New project ska vara read-only reference, och andra F-mappar ska vara read-only tills säker permission/write approval finns.

## 2026-05-05 07:12:25

Type: project_fact
Importance: 4/5
Tags: project

Text:
fram

## 2026-05-05 07:15:28

Type: project_fact
Importance: 4/5
Tags: project

Text:
CommandValidator V1 skeleton är skapad i app/CommandValidatorV1.cs. Den ska senare kontrollera saknade argument och approval-regler för minne, sök, filskrivning, terminal och modellbyte.

## 2026-05-05 07:20:10

Type: project_fact
Importance: 4/5
Tags: project

Text:
PendingApproval V1 skeleton är skapad i app/PendingApprovalV1.cs. Den ska senare användas för säker godkännandeprocess innan filskrivning, terminalkörning och projektminnesförslag.

## 2026-05-05 07:36:27

Type: project_fact
Importance: 4/5
Tags: project

Text:
Codex-handoff är skapad i docs/CODEX_HANDOFF.md och docs/CODEX_START_PROMPT.md. Jarvis ska få slash-kommandon som exakt säkert läge och naturligt språk som LLM-förståelse som översätts till säkra intents.

## 2026-05-09 23:31:52

Type: note
Importance: 3/5
Tags: smart-memory

Text:
jag testar fas 1

## 2026-05-10 08:14:02

Type: note
Importance: 3/5
Tags: general

Text:
att en av dina slut mål är att kunna bygga projekt och appar samt websidor och bilder och kunna styra min dator som en assistent

