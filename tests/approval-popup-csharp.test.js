const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const source = fs.readFileSync(programPath, "utf8");

const requiredMarkers = [
  "jarvis_pending_approval_v1",
  "HandlePendingApprovalDecisionV1",
  "ShowOrHidePendingApprovalPopupV1Async",
  "SendPendingApprovalPopupV1Async",
  "window.jarvisShowPendingApprovalV1",
  "window.jarvisHidePendingApprovalV1"
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!source.includes(marker)) {
    failures += 1;
    console.log(`FAIL missing approval bridge marker: ${marker}`);
  } else {
    console.log(`PASS approval bridge marker present: ${marker}`);
  }
}

const messageHandler = source.slice(
  source.indexOf("private async void OnWebMessageReceived"),
  source.indexOf("private async Task<bool> TryHandleCommandRouterV1UiAsync")
);

if (!messageHandler.includes("jarvis_pending_approval_v1")) {
  failures += 1;
  console.log("FAIL WebView message handler does not route popup approval decisions");
} else {
  console.log("PASS WebView message handler routes popup approval decisions");
}

if (!messageHandler.includes("ShowOrHidePendingApprovalPopupV1Async")) {
  failures += 1;
  console.log("FAIL chat flow does not refresh approval popup state after local handling");
} else {
  console.log("PASS chat flow refreshes approval popup state");
}

if (failures > 0) {
  process.exit(1);
}
