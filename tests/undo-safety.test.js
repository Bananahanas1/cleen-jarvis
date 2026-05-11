const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const pendingPath = path.join(__dirname, "..", "app", "PendingApprovalV1.cs");
const dashboardPath = path.join(__dirname, "..", "dashboard", "index.html");

const program = fs.readFileSync(programPath, "utf8");
const pending = fs.readFileSync(pendingPath, "utf8");
const dashboard = fs.readFileSync(dashboardPath, "utf8");

const requiredMarkers = [
  [pending, "FileUndo", "pending approval type for undo"],
  [program, "LatestFileUndoV1", "latest undo snapshot storage"],
  [program, "jarvis_undo_last_change_v1", "dashboard undo message handler"],
  [program, "UndoLastFileChangeRequestTool", "undo request creates pending preview"],
  [program, "ApprovePendingFileUndoTool", "undo approval applies change"],
  [dashboard, "undoLastChangeBtn", "dashboard undo button"]
];

let failures = 0;

for (const [source, marker, label] of requiredMarkers) {
  if (!source.includes(marker)) {
    failures += 1;
    console.log(`FAIL missing ${label}: ${marker}`);
  } else {
    console.log(`PASS ${label}`);
  }
}

const requestStart = program.indexOf("private static string UndoLastFileChangeRequestTool()");
const requestEnd = program.indexOf("private static string ApprovePendingFileUndoTool()");

if (requestStart < 0 || requestEnd < 0 || requestEnd <= requestStart) {
  failures += 1;
  console.log("FAIL could not locate undo request block");
} else {
  const requestBlock = program.slice(requestStart, requestEnd);
  const forbiddenInRequest = ["File.WriteAllText", "File.AppendAllText", "File.Delete"];

  for (const marker of forbiddenInRequest) {
    if (requestBlock.includes(marker)) {
      failures += 1;
      console.log(`FAIL undo request must not touch disk directly: ${marker}`);
    } else {
      console.log(`PASS undo request does not call ${marker}`);
    }
  }

  if (!requestBlock.includes("PendingApprovalStoreV1.Set")) {
    failures += 1;
    console.log("FAIL undo request does not create pending approval");
  } else {
    console.log("PASS undo request creates pending approval");
  }
}

if (failures > 0) {
  process.exit(1);
}
