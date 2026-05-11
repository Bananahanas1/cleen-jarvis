const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const validator = fs.readFileSync(path.join(root, "app", "CommandValidatorV1.cs"), "utf8");
const tool = fs.readFileSync(path.join(root, "app", "Brain", "NaturalEditTool.cs"), "utf8");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

function blockBetween(text, startMarker, endMarker) {
  const start = text.indexOf(startMarker);
  const end = start < 0 ? -1 : text.indexOf(endMarker, start + startMarker.length);
  if (start < 0) return "";
  if (end < 0 || end <= start) return text.slice(start);
  return text.slice(start, end);
}

let failures = 0;

const requiredToolMarkers = [
  "NaturalEditTool",
  "TryParseNaturalLanguage",
  "BuildPrompt",
  "CleanFullFileContent",
  "MaxEditableChars"
];

for (const marker of requiredToolMarkers) {
  if (!tool.includes(marker)) {
    failures += 1;
    console.log("FAIL NaturalEditTool missing: " + marker);
  } else {
    console.log("PASS NaturalEditTool has: " + marker);
  }
}

const naturalEditBlock = blockBetween(
  program,
  "private static async Task<string> NaturalEditRequestToolAsync(",
  "private static string WriteProjectFileTool("
);

const requiredProgramMarkers = [
  "NaturalEditRequestToolAsync",
  "AskOllamaRawModelAsync",
  "ModelCatalog.Coder.Name",
  "TryGetSafeProjectFilePath(relativePath, mustBeExisting: true",
  "IsWritableProjectFileExtension(extension)",
  "PendingApprovalStoreV1.HasPending",
  "PendingApprovalStoreV1.Set",
  "PendingApprovalTypeV1.FileWrite",
  "BuildPendingApprovalPreviewV1(proposedContent)"
];

for (const marker of requiredProgramMarkers) {
  if (!naturalEditBlock.includes(marker) && !program.includes(marker)) {
    failures += 1;
    console.log("FAIL Program missing natural edit marker: " + marker);
  } else {
    console.log("PASS Program natural edit marker present: " + marker);
  }
}

const forbiddenProgramMarkers = [
  "File.WriteAllText(fullPath",
  "File.AppendAllText(fullPath",
  "File.Delete(fullPath"
];

for (const marker of forbiddenProgramMarkers) {
  if (naturalEditBlock.includes(marker)) {
    failures += 1;
    console.log("FAIL NaturalEditRequestToolAsync must not mutate disk directly: " + marker);
  } else {
    console.log("PASS NaturalEditRequestToolAsync avoids direct mutation: " + marker);
  }
}

const routerMarkers = [
  "NaturalCodeEdit",
  "natural_edit.request",
  'command == "edit"',
  "RequiresApproval = true"
];

for (const marker of routerMarkers) {
  if (!router.includes(marker)) {
    failures += 1;
    console.log("FAIL CommandRouterV1 missing: " + marker);
  } else {
    console.log("PASS CommandRouterV1 has: " + marker);
  }
}

if (!validator.includes("case CommandIntent.NaturalCodeEdit") ||
    !validator.includes("Naturligt språk-edit måste kräva pending/godkännande.")) {
  failures += 1;
  console.log("FAIL CommandValidatorV1 must validate NaturalCodeEdit approval");
} else {
  console.log("PASS CommandValidatorV1 validates NaturalCodeEdit approval");
}

if (!dashboard.includes('"/edit "')) {
  failures += 1;
  console.log("FAIL dashboard slash autocomplete missing /edit");
} else {
  console.log("PASS dashboard slash autocomplete includes /edit");
}

if (failures > 0) {
  process.exit(1);
}
