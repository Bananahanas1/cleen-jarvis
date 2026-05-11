const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const dashboardPath = path.join(__dirname, "..", "dashboard", "index.html");
const source = fs.readFileSync(programPath, "utf8");
const dashboard = fs.readFileSync(dashboardPath, "utf8");

function blockBetween(startMarker, endMarker) {
  const start = source.indexOf(startMarker);
  const end = start < 0 ? -1 : source.indexOf(endMarker, start + startMarker.length);

  if (start < 0) return "";
  if (end < 0 || end <= start) return source.slice(start);

  return source.slice(start, end);
}

const previewBlock = blockBetween(
  "private static string TerminalPreviewTool(",
  "private static string TerminalConfirmRun()"
);

const confirmBlock = blockBetween(
  "private static string TerminalConfirmRun()",
  "private static string TerminalCancelRun()"
);

const cancelBlock = blockBetween(
  "private static string TerminalCancelRun()",
  "private static string TryBuildSafeTerminalCommand("
);

const genericCancelBlock = blockBetween(
  "private static string CancelCurrentPendingApprovalTool()",
  "private static bool IsGenericCancelCommand("
);

let failures = 0;

const requiredMarkers = [
  [previewBlock, "PendingApprovalStoreV1.Set", "terminal preview creates pending approval"],
  [previewBlock, "PendingApprovalTypeV1.TerminalRun", "terminal preview uses TerminalRun approval type"],
  [source, "ApprovePendingTerminalRunTool", "terminal run approval method exists"],
  [source, "CancelPendingTerminalRunTool", "terminal run cancel method exists"],
  [source, "CancelCurrentPendingApprovalTool", "context-aware generic cancel method exists"],
  [source, "pending.Type == PendingApprovalTypeV1.TerminalRun", "central approval handles terminal run"],
  [source, "ShowLatestTerminalTranscriptTool", "latest terminal transcript method exists"],
  [dashboard, "/terminal preview ", "slash terminal autocomplete suggestion exists"],
  [dashboard, "/terminal visa", "slash terminal show autocomplete suggestion exists"],
  [dashboard, "/terminal godkänn", "slash terminal approve autocomplete suggestion exists"],
  [dashboard, "/terminal avbryt", "slash terminal cancel autocomplete suggestion exists"]
];

for (const [haystack, marker, label] of requiredMarkers) {
  if (!haystack.includes(marker)) {
    failures += 1;
    console.log(`FAIL missing ${label}: ${marker}`);
  } else {
    console.log(`PASS ${label}`);
  }
}

const forbiddenInPreview = [
  "Process.Start",
  "PendingTerminalCommand",
  "PendingTerminalWorkingDirectory"
];

for (const marker of forbiddenInPreview) {
  if (previewBlock.includes(marker)) {
    failures += 1;
    console.log(`FAIL terminal preview must not directly run or use legacy pending storage: ${marker}`);
  } else {
    console.log(`PASS terminal preview avoids ${marker}`);
  }
}

if (!confirmBlock.includes("ApprovePendingTerminalRunTool()")) {
  failures += 1;
  console.log("FAIL terminal confirm does not delegate to pending approval run");
} else {
  console.log("PASS terminal confirm delegates to pending approval run");
}

if (!source.includes("WaitForExit(TerminalRunTimeoutMs)") ||
    !source.includes("BeginOutputReadLine()") ||
    !source.includes("BeginErrorReadLine()") ||
    !source.includes("TerminalRunTimeoutMs = 120000")) {
  failures += 1;
  console.log("FAIL terminal runner should stream output and use 120s timeout");
} else {
  console.log("PASS terminal runner streams output with 120s timeout");
}

if (!source.includes("BuildTerminalRunResult") ||
    !source.includes("Terminalpanelen har uppdaterats med full output.") ||
    !source.includes("BuildTerminalRunSummary")) {
  failures += 1;
  console.log("FAIL terminal results should use compact summary output");
} else {
  console.log("PASS terminal results use compact summary output");
}

if (!source.includes("TerminalPanelV1") ||
    !source.includes("LatestTerminalPanelV1") ||
    !source.includes("SendLatestTerminalPanelV1Async") ||
    !source.includes("ShowLatestTerminalTranscriptTool") ||
    !dashboard.includes("terminalPanel") ||
    !dashboard.includes("window.jarvisShowTerminalPanelV1")) {
  failures += 1;
  console.log("FAIL terminal output should update a dashboard terminal panel and remain visible to Jarvis");
} else {
  console.log("PASS terminal output updates dashboard panel and stays visible to Jarvis");
}

const showTranscriptBlock = blockBetween(
  "private static string ShowLatestTerminalTranscriptTool()",
  "private static string TrimTerminalOutput("
);

if (showTranscriptBlock.includes("Output preview:") ||
    showTranscriptBlock.includes("TrimTerminalOutput(") ||
    !showTranscriptBlock.includes("Full output finns i Terminal-panelen.")) {
  failures += 1;
  console.log("FAIL terminal transcript chat response should stay compact and point to Terminal panel");
} else {
  console.log("PASS terminal transcript chat response stays compact");
}

if (source.includes("[\"avbryt\"] = \"avbryt andring\"") ||
    !source.includes("IsGenericCancelCommand") ||
    !source.includes("Det finns inget pending att avbryta.") ||
    !source.includes("CancelCurrentPendingApprovalTool()")) {
  failures += 1;
  console.log("FAIL generic cancel should be context-aware and must not alias to change/delete cancellation");
} else {
  console.log("PASS generic cancel is context-aware");
}

if (!genericCancelBlock.includes("if (pending is null)") ||
    !genericCancelBlock.includes("Det finns inget pending att avbryta.") ||
    !genericCancelBlock.includes("pending.Type == PendingApprovalTypeV1.TerminalRun") ||
    !genericCancelBlock.includes("CancelPendingTerminalRunTool()")) {
  failures += 1;
  console.log("FAIL generic cancel should be neutral with no pending and cancel terminal when terminal is pending");
} else {
  console.log("PASS generic cancel is neutral without pending and handles pending terminal");
}

if (!cancelBlock.includes("CancelPendingTerminalRunTool()")) {
  failures += 1;
  console.log("FAIL terminal cancel does not cancel pending terminal approval");
} else {
  console.log("PASS terminal cancel cancels pending terminal approval");
}

if (failures > 0) {
  process.exit(1);
}
