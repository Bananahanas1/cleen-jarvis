const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const dashboardPath = path.join(__dirname, "..", "dashboard", "index.html");

const source = fs.readFileSync(programPath, "utf8");
const dashboard = fs.readFileSync(dashboardPath, "utf8");

function blockBetween(text, startMarker, endMarker) {
  const start = text.indexOf(startMarker);
  const end = start < 0 ? -1 : text.indexOf(endMarker, start + startMarker.length);

  if (start < 0) return "";
  if (end < 0 || end <= start) return text.slice(start);

  return text.slice(start, end);
}

let failures = 0;

const requiredDashboardMarkers = [
  ["edit button exists", 'id="editBtn"'],
  ["save button exists", 'id="saveBtn"'],
  ["edit button enables textarea", "editorArea.readOnly = false"],
  ["save posts pending request", 'type: "jarvis_editor_save_pending_v1"'],
  ["save posts active path", "path: activeEditorPathV1"],
  ["save posts editor content", "content: editorArea.value"],
  ["tracks truncated editor content", "editorSaveBlockedV1"],
  ["blocks editing truncated previews", "editBtn.disabled = !hasFile || editorSaveBlockedV1"],
  ["blocks saving truncated previews", "saveBtn.disabled = !hasFile || editorSaveBlockedV1"]
];

for (const [label, marker] of requiredDashboardMarkers) {
  if (!dashboard.includes(marker)) {
    failures += 1;
    console.log(`FAIL dashboard missing ${label}: ${marker}`);
  } else {
    console.log(`PASS dashboard ${label}`);
  }
}

const forbiddenDashboardMarkers = [
  "Edit-läge senare",
  "Spara efter godkännande senare",
  "Just nu är panelen säker read-only. Edit/spara byggs i nästa fas."
];

for (const marker of forbiddenDashboardMarkers) {
  if (dashboard.includes(marker)) {
    failures += 1;
    console.log(`FAIL old disabled editor marker remains: ${marker}`);
  } else {
    console.log(`PASS old disabled editor marker absent: ${marker}`);
  }
}

const handlerBlock = blockBetween(
  source,
  'if (type == "jarvis_editor_save_pending_v1")',
  'if (type == "jarvis_pending_approval_v1")'
);

if (!handlerBlock.includes("EditorSavePendingTool(editorPath, editorContent)")) {
  failures += 1;
  console.log("FAIL C# WebView handler must route editor save to pending tool");
} else {
  console.log("PASS C# WebView handler routes editor save to pending tool");
}

const sendEditorBlock = blockBetween(
  source,
  "private async Task SendProjectFileToEditor(",
  "private async Task AddAssistantMessage("
);

if (!sendEditorBlock.includes("isEditorContentTruncated")) {
  failures += 1;
  console.log("FAIL C# editor sender must mark truncated previews as save-blocked");
} else {
  console.log("PASS C# editor sender marks truncated previews as save-blocked");
}

const editorSaveBlock = blockBetween(
  source,
  "private static string EditorSavePendingTool(",
  "private static bool TryExtractDeleteProjectFileRequest("
);

const requiredEditorSaveMarkers = [
  "TryGetSafeProjectFilePath(relativePath, mustBeExisting: true",
  "PendingApprovalStoreV1.Set",
  "PendingApprovalTypeV1.FileWrite",
  "RequiresUserApproval = true"
];

for (const marker of requiredEditorSaveMarkers) {
  if (!editorSaveBlock.includes(marker)) {
    failures += 1;
    console.log(`FAIL editor save safety marker missing: ${marker}`);
  } else {
    console.log(`PASS editor save safety marker present: ${marker}`);
  }
}

const forbiddenEditorSaveMarkers = [
  "File.WriteAllText(fullPath",
  "File.AppendAllText(fullPath",
  "File.Delete(fullPath"
];

for (const marker of forbiddenEditorSaveMarkers) {
  if (editorSaveBlock.includes(marker)) {
    failures += 1;
    console.log(`FAIL editor save must not directly mutate disk: ${marker}`);
  } else {
    console.log(`PASS editor save avoids direct disk mutation: ${marker}`);
  }
}

if (failures > 0) {
  process.exit(1);
}
