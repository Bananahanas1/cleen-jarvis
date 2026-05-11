# MVP Backlog (Fas A)

Senast uppdaterad: 2026-05-06

Målet med MVP (Fas A) är att få en stabil bas av Jarvis-clean som kan byggas vidare i säkra, små steg. Nedan följer en konkret backlog som du kan kopiera in i ditt arbetsflöde och följa direkt.

Planens fokus: stabil baseline, tydlig testbank, dokumentation och en tydlig checklista för nästa fas.

## Mål för Fas A (MVP)
- Leverera en körbar baseline som bygger och laddar utan fel.
- Implementera en första uppsättning tester (unit/integration) som täcker kärnflödena: routing, filverktyg och terminal-API.
- Införa en första dokumentations-skylt: hur man kör lokalt, hur man testar och hur man bidrar.
- Etablera en enkel back-/verify-kedja så att verifiering innan commit/PR känns tydlig.
- Upprätta en enkel backlog-kanban (lokal eller i vault) för fortsatt arbete.

## Konkreta uppgifter
1) Baseline byggbarhet
   - Se till att dotnet build av app-projektet lyckas utan fel.
   - Rensa upp eventuella varningar som inte blockerar runtime.
   - Bekräfta att 3-panel layout laddas utan krascher.

2) Grundläggande tester
   - Skapa/säkerställ test-mallar: unit-test för CommandRouterV1 och CommandValidatorV1.
   - Skapa enkla integrationstester för: läs/skriv via PendingApprovalV1 (mockad disk).
   - Lägg till testkörning i lokal utvecklingsmiljö (CI-skeletton kan beskrivas i planen).

3) Dokumentation
   - Uppdatera CURRENT_STATE.md och MASTER_PLAN.md med MVP-status.
   - Lägg till en kort onboarding-guide i docs/README eller docs/developer-guide.
   - Lägg till plan för nästa fas (Fas B) i PLAN/MVP-backlog.md.

4) Verifierings- och release-checklista
   - Skapa en enkel “verification-before-completion” checklista för MVP-delar.
   - Beskriv vilka kommandon/byggsteg som måste lyckas innan PR kan merges.

5) Habits: läs alla md-filer före planering
   - Aktiv vana: läs igenom alla md-filer i projektet innan nästa planläggning och uppdatera sammanfattning i backlog om relevanta nya insikter finns.

## Acceptance-kriterier
- Baseline byggbarhet: bygg utan fel och app startar.
- Minsta testuppsättning implementerad och passerad lokalt.
- Dokumentation uppdaterad och konsistent med MVP-syfte.
- Verifieringskriterier definerade och användas innan commit/PR.
- En enkel habit-dokumentation finns för att läsa md-filer som rutin.

Nuvarande status: MVP-planen är definierad och redo att startas. Användaren kan ge klartecken för att börja med Fas A.
