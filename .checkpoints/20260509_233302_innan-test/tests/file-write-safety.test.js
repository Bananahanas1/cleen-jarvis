const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const source = fs.readFileSync(programPath, "utf8");

const start = source.indexOf("private static string WriteProjectFileTool(");
const end = source.indexOf("private static bool AllowedExtensionForFileTool(");

if (start < 0 || end < 0 || end <= start) {
  throw new Error("Could not locate WriteProjectFileTool block.");
}

const writeToolBlock = source.slice(start, end);
const forbidden = ["File.WriteAllText(fullPath", "File.AppendAllText(fullPath"];
const required = [
  "PendingApprovalStoreV1.Set",
  "PendingApprovalTypeV1.FileWrite",
  "PendingApprovalTypeV1.FileAppend",
  "TryGetSafeProjectFilePath(relativePath, mustBeExisting: true",
  "/fil skapa"
];

const approveStart = source.indexOf("private static string ApprovePendingFileWriteTool()");
const approveEnd = source.indexOf("private static string CancelPendingFileWriteTool()");

if (approveStart < 0 || approveEnd < 0 || approveEnd <= approveStart) {
  throw new Error("Could not locate ApprovePendingFileWriteTool block.");
}

const approveWriteBlock = source.slice(approveStart, approveEnd);
const approveRequired = [
  "TryGetSafeProjectFilePath(pending.Target, mustBeExisting:",
  "PendingApprovalTypeV1.FileCreate",
  "ExistedBefore: false",
  'changeKind = pending.Type == PendingApprovalTypeV1.FileCreate ? "create"'
];

const createStart = source.indexOf("private static string CreateProjectFileRequestTool(");
const createEnd = source.indexOf("private static string EditorSavePendingTool(");

if (createStart < 0 || createEnd < 0 || createEnd <= createStart) {
  throw new Error("Could not locate CreateProjectFileRequestTool block.");
}

const createToolBlock = source.slice(createStart, createEnd);
const createRequired = [
  "TryGetSafeProjectFilePath(relativePath, mustBeExisting: false",
  "if (File.Exists(fullPath))",
  "PendingApprovalStoreV1.Set",
  "PendingApprovalTypeV1.FileCreate",
  "RequiresUserApproval = true"
];

const createForbidden = [
  "File.WriteAllText(fullPath",
  "File.AppendAllText(fullPath",
  "File.Delete(fullPath"
];

let failures = 0;

for (const value of forbidden) {
  if (writeToolBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL direct write remains in WriteProjectFileTool: ${value}`);
  } else {
    console.log(`PASS direct write absent: ${value}`);
  }
}

for (const value of required) {
  if (!writeToolBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL pending approval marker missing: ${value}`);
  } else {
    console.log(`PASS pending approval marker present: ${value}`);
  }
}

for (const value of approveRequired) {
  if (!approveWriteBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL approve guard missing: ${value}`);
  } else {
    console.log(`PASS approve guard present: ${value}`);
  }
}

for (const value of createRequired) {
  if (!createToolBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL create pending marker missing: ${value}`);
  } else {
    console.log(`PASS create pending marker present: ${value}`);
  }
}

for (const value of createForbidden) {
  if (createToolBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL create request must not mutate disk directly: ${value}`);
  } else {
    console.log(`PASS create request avoids direct disk mutation: ${value}`);
  }
}

if (failures > 0) {
  process.exit(1);
}
