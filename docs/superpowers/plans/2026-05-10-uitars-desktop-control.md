# UI-TARS Desktop Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build Jarvis-native UI-TARS desktop control with screenshot, action parsing and pending-approved click/type/scroll execution.

**Architecture:** Jarvis owns the safety loop. UI-TARS or a compatible VLM may propose actions, but Jarvis converts them into `DesktopActionRequestV1` and stores them as `PendingApprovalV1.DesktopAction`; execution only happens after approval.

**Tech Stack:** C# WinForms/WebView2, user32 mouse APIs, `SendKeys`, `System.Drawing` screenshots, OpenAI-compatible vision chat completions.

---

### Task 1: Safe Desktop Action Core

**Files:**
- Create: `app/Desktop/DesktopActionRequestV1.cs`
- Create: `app/Desktop/DesktopActionGate.cs`
- Create: `app/Desktop/DesktopActionExecutor.cs`
- Modify: `app/PendingApprovalV1.cs`

- [x] Add desktop action kinds: click, double click, right click, hover, drag, type, hotkey, scroll, wait, finished.
- [x] Add parser for manual commands and UI-TARS predictions such as `click(start_box='(30,40)')`.
- [x] Add safety gate with default OFF, foreground blacklist, rate limit and audit log.
- [x] Add executor that runs only after `PendingApprovalV1.DesktopAction`.

### Task 2: Screen Capture and UI-TARS Bridge

**Files:**
- Create: `app/Desktop/ScreenCapture.cs`
- Create: `app/Bridges/UiTarsBridge.cs`
- Create: `config/uitars.example.json`

- [x] Capture primary screen to `data/screenshots`.
- [x] Detect local `F:\UI-TARS-desktop-main`.
- [x] Support OpenAI-compatible UI-TARS vision config through env vars or `config/uitars.json`.
- [x] Convert UI-TARS/VLM response into a pending desktop action.
- [x] Add subprocess start/stop for UI-TARS Desktop through `pnpm --filter ui-tars-desktop start`.

### Task 3: Router, UI and Approval

**Files:**
- Modify: `app/CommandRouterV1.cs`
- Modify: `app/CommandValidatorV1.cs`
- Modify: `app/Program.cs`
- Modify: `dashboard/index.html`

- [x] Add `/desktop status`, `/desktop på`, `/desktop av`.
- [x] Add `/skärm`.
- [x] Add `/desktop klick`, `/desktop dubbelklick`, `/desktop högerklick`, `/desktop drag`, `/desktop skriv`, `/desktop hotkey`, `/desktop scroll`.
- [x] Add `/desktop fråga <instruktion>` for UI-TARS vision proposals.
- [x] Add `/desktop tars start` and `/desktop tars stop`.
- [x] Add Ctrl+Shift+Alt+J hard-kill.
- [x] Add dashboard autocomplete and overview desktop state.

### Task 4: Verification and Documentation

**Files:**
- Create: `tests/desktop-control.test.js`
- Modify: `tests/CommandRouterV1.Tests/Program.cs`
- Modify: `tests/CommandRouterV1.Tests/CommandRouterV1.Tests.csproj`
- Modify: docs/vault handoff files

- [x] Add Node safety/marker test.
- [x] Add C# router/parser tests.
- [x] Run targeted tests and build.
- [x] Run full regression, publish and restart.
