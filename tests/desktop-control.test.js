const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const validator = fs.readFileSync(path.join(root, "app", "CommandValidatorV1.cs"), "utf8");
const pending = fs.readFileSync(path.join(root, "app", "PendingApprovalV1.cs"), "utf8");
const bridge = fs.readFileSync(path.join(root, "app", "Bridges", "UiTarsBridge.cs"), "utf8");
const action = fs.readFileSync(path.join(root, "app", "Desktop", "DesktopActionRequestV1.cs"), "utf8");
const gate = fs.readFileSync(path.join(root, "app", "Desktop", "DesktopActionGate.cs"), "utf8");
const executor = fs.readFileSync(path.join(root, "app", "Desktop", "DesktopActionExecutor.cs"), "utf8");
const capture = fs.readFileSync(path.join(root, "app", "Desktop", "ScreenCapture.cs"), "utf8");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

function blockBetween(text, startMarker, endMarker) {
  const start = text.indexOf(startMarker);
  const end = start < 0 ? -1 : text.indexOf(endMarker, start + startMarker.length);
  if (start < 0) return "";
  if (end < 0 || end <= start) return text.slice(start);
  return text.slice(start, end);
}

let failures = 0;

const filesAndMarkers = [
  [bridge, ["UiTarsBridge", "JARVIS_UITARS_BASE_URL", "JARVIS_UITARS_API_KEY", "JARVIS_UITARS_MODEL", "click(start_box", "ProposeActionAsync"]],
  [action, ["DesktopActionRequestV1", "TryParsePrediction", "TryCreateManual", "Click", "DoubleClick", "RightClick", "Drag", "TypeText", "Hotkey", "Scroll"]],
  [gate, ["DesktopActionGate", "Enabled", "Rate limit", "GetForegroundWindow", "taskmgr", "regedit", "powershell", "desktop_actions.log"]],
  [executor, ["DesktopActionExecutor", "SetCursorPos", "mouse_event", "SendKeys.SendWait", "DesktopActionGate.CanExecute"]],
  [capture, ["ScreenCapture", "CapturePrimaryToFile", "data", "screenshots", "CopyFromScreen"]]
];

for (const [text, markers] of filesAndMarkers) {
  for (const marker of markers) {
    if (!text.includes(marker)) {
      failures += 1;
      console.log("FAIL missing marker: " + marker);
    } else {
      console.log("PASS marker: " + marker);
    }
  }
}

for (const marker of [
  "DesktopAction",
  "DesktopStatus",
  "DesktopEnable",
  "DesktopDisable",
  "DesktopScreenshot",
  "DesktopBridgeStart",
  "DesktopBridgeStop",
  "DesktopVisionRequest",
  "DesktopActionRequest",
  "ParseDesktopSlashCommand",
  "desktop.action.request",
  "desktop.vision.request",
  "desktop.bridge.start",
  "RequiresApproval = true"
]) {
  if (!router.includes(marker) && !pending.includes(marker)) {
    failures += 1;
    console.log("FAIL routing/pending missing: " + marker);
  } else {
    console.log("PASS routing/pending has: " + marker);
  }
}

if (!validator.includes("case CommandIntent.DesktopActionRequest") ||
    !validator.includes("Desktop-action måste kräva pending/godkännande.") ||
    !validator.includes("case CommandIntent.DesktopVisionRequest")) {
  failures += 1;
  console.log("FAIL validator must require approval for desktop actions");
} else {
  console.log("PASS validator requires approval for desktop actions");
}

for (const marker of [
  "DesktopStatusTool",
  "DesktopEnableTool",
  "DesktopDisableTool",
  "DesktopScreenshotTool",
  "StartAsync",
  "StopAsync",
  "DesktopActionRequestTool",
  "DesktopVisionRequestToolAsync",
  "ApprovePendingDesktopActionTool",
  "CancelPendingDesktopActionTool",
  "PendingApprovalTypeV1.DesktopAction",
  "RegisterHotKey",
  "Ctrl+Shift+Alt+J"
]) {
  if (!program.includes(marker)) {
    failures += 1;
    console.log("FAIL Program missing: " + marker);
  } else {
    console.log("PASS Program has: " + marker);
  }
}

const requestBlock = blockBetween(
  program,
  "private static string DesktopActionRequestTool",
  "private static async Task<string> DesktopVisionRequestToolAsync"
);

for (const forbidden of ["DesktopActionExecutor.Execute", "SetCursorPos", "mouse_event", "SendKeys.SendWait"]) {
  if (requestBlock.includes(forbidden)) {
    failures += 1;
    console.log("FAIL request tool must not execute directly: " + forbidden);
  } else {
    console.log("PASS request tool avoids direct execution: " + forbidden);
  }
}

for (const marker of [
  '"/desktop status"',
  '"/desktop på"',
  '"/desktop av"',
  '"/desktop tars start"',
  '"/desktop klick "',
  '"/desktop fråga "',
  '"/skärm"',
  "visualDesktopState",
  "isRouterOnlyCommandV15"
]) {
  if (!dashboard.includes(marker)) {
    failures += 1;
    console.log("FAIL dashboard missing: " + marker);
  } else {
    console.log("PASS dashboard has: " + marker);
  }
}

if (!fs.existsSync(path.join(root, "config", "uitars.example.json"))) {
  failures += 1;
  console.log("FAIL missing config/uitars.example.json");
} else {
  console.log("PASS config/uitars.example.json exists");
}

if (failures > 0) process.exit(1);
