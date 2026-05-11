const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const pendingPath = path.join(__dirname, "..", "app", "PendingApprovalV1.cs");
const program = fs.readFileSync(programPath, "utf8");
const pending = fs.readFileSync(pendingPath, "utf8");

const requiredMarkers = [
  "PendingApprovalTypeV1.FileDelete",
  "TryExtractDeleteProjectFileRequest",
  "DeleteProjectFileRequestTool",
  "ApprovePendingFileDeleteTool",
  "CancelPendingFileDeleteTool",
  "Godkänn filradering"
];

let failures = 0;

for (const marker of requiredMarkers) {
  const source = marker === "PendingApprovalTypeV1.FileDelete" ? pending + program : program;
  if (!source.includes(marker)) {
    failures += 1;
    console.log(`FAIL missing delete safety marker: ${marker}`);
  } else {
    console.log(`PASS delete safety marker present: ${marker}`);
  }
}

const requestStart = program.indexOf("private static string DeleteProjectFileRequestTool(");
const requestEnd = program.indexOf("private static string ApprovePendingFileDeleteTool(");

if (requestStart < 0 || requestEnd < 0 || requestEnd <= requestStart) {
  failures += 1;
  console.log("FAIL could not locate delete request block");
} else {
  const requestBlock = program.slice(requestStart, requestEnd);

  if (requestBlock.includes("File.Delete(")) {
    failures += 1;
    console.log("FAIL delete request deletes directly instead of creating pending approval");
  } else {
    console.log("PASS delete request does not delete directly");
  }

  if (!requestBlock.includes("PendingApprovalStoreV1.Set") ||
      !requestBlock.includes("PendingApprovalTypeV1.FileDelete")) {
    failures += 1;
    console.log("FAIL delete request does not create pending file delete approval");
  } else {
    console.log("PASS delete request creates pending file delete approval");
  }
}

if (failures > 0) {
  process.exit(1);
}
