const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const source = fs.readFileSync(programPath, "utf8");

const requiredMarkers = [
  "jarvis_review_file_change_v1",
  "SendLatestFileChangeReviewV1Async",
  "BuildFileChangeReviewV1",
  "LatestFileChangeReviewV1",
  "window.jarvisShowFileChangeReviewV1",
  "SendProjectFileToEditor(reviewPath)",
  "SendTreeFolderV7Async(reviewFolder"
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!source.includes(marker)) {
    failures += 1;
    console.log(`FAIL missing change review marker: ${marker}`);
  } else {
    console.log(`PASS change review marker present: ${marker}`);
  }
}

const approveStart = source.indexOf("private static string ApprovePendingFileWriteTool()");
const approveEnd = source.indexOf("private static string CancelPendingFileWriteTool()");
const approveBlock = approveStart >= 0 && approveEnd > approveStart
  ? source.slice(approveStart, approveEnd)
  : "";

if (!approveBlock.includes("File.ReadAllText(fullPath)") ||
    !approveBlock.includes("BuildFileChangeReviewV1")) {
  failures += 1;
  console.log("FAIL approve flow does not capture before/after content for review");
} else {
  console.log("PASS approve flow captures before/after content for review");
}

if (failures > 0) {
  process.exit(1);
}
