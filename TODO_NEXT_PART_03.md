# TODO_NEXT PART 03

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


- [x] Old duplicated smart-open V3/V4/V5/V6/V7 methods removed.
- [x] WebView smart-open message compatibility routes through one canonical smart-open path.
- [x] `tests\smart-open-cleanup.test.js` guards against duplicate smart-open returning.

### Terminal safety

- [x] Terminal preview/confirm/cancel uses `PendingApprovalV1`.
- [x] Approval popup reused for terminal preview.
- [x] Approval popup focuses `Avbryt` first and briefly locks `Godkänn`.
- [x] Approved terminal timeout increased to 120 seconds.
- [x] Terminal output streams stdout/stderr asynchronously.
- [x] Terminal-panel V1 exists.
- [x] Chat receives compact terminal summaries.
- [x] Latest terminal transcript stays in runtime memory.
- [x] `visa terminal` and related phrases no longer open terminal test files.
- [x] Generic `avbryt` is context-aware.

## Later roadmap

Read `docs\JARVIS_LONG_TERM_VISION.md` before larger work.

Later phases:

- Smart natural-language routing to validated intents.
- `.jarvis/tasks` task workspace.
- Worker agents for read/summarize/propose only.
- Multi-root Project Explorer with read-only defaults.
- Local model/provider setup docs/scripts.
- Optional Visual Lab / 3D after safety and workspace are stable.
- Voice Jarvis on top of the same router/validator/approval path.
