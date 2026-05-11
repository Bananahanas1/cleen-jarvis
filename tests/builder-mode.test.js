const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const validator = fs.readFileSync(path.join(root, "app", "CommandValidatorV1.cs"), "utf8");
const builder = fs.readFileSync(path.join(root, "app", "Brain", "BuilderMode.cs"), "utf8");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

function blockBetween(text, startMarker, endMarker) {
  const start = text.indexOf(startMarker);
  const end = start < 0 ? -1 : text.indexOf(endMarker, start + startMarker.length);
  if (start < 0) return "";
  if (end < 0 || end <= start) return text.slice(start);
  return text.slice(start, end);
}

let failures = 0;

const builderMarkers = [
  "BuilderMode",
  "BuilderSessionV1",
  "BuildQuestionPrompt",
  "BuildPlanPrompt",
  "BuildPlanMarkdown",
  "PlanRelativePath",
  "vault/builds/",
  "PendingApproval"
];

for (const marker of builderMarkers) {
  if (!builder.includes(marker)) {
    failures += 1;
    console.log("FAIL BuilderMode missing: " + marker);
  } else {
    console.log("PASS BuilderMode has: " + marker);
  }
}

const programMarkers = [
  "BuilderStartToolAsync",
  "BuilderAnswerTool",
  "BuilderPlanToolAsync",
  "BuilderCancelTool",
  "ModelCatalog.Smart.Name",
  "CreateProjectFileRequestTool(relativePath, planContent)",
  "PendingApprovalStoreV1.HasPending"
];

for (const marker of programMarkers) {
  if (!program.includes(marker)) {
    failures += 1;
    console.log("FAIL Program missing BuilderMode marker: " + marker);
  } else {
    console.log("PASS Program has BuilderMode marker: " + marker);
  }
}

const planBlock = blockBetween(
  program,
  "private static async Task<string> BuilderPlanToolAsync()",
  "private static string BuilderCancelTool()"
);

for (const marker of ["File.WriteAllText", "File.AppendAllText", "File.Delete", "Directory.CreateDirectory"]) {
  if (planBlock.includes(marker)) {
    failures += 1;
    console.log("FAIL BuilderPlanToolAsync must not mutate disk directly: " + marker);
  } else {
    console.log("PASS BuilderPlanToolAsync avoids direct mutation: " + marker);
  }
}

const routerMarkers = [
  "BuilderStart",
  "BuilderAnswer",
  "BuilderPlan",
  "BuilderStatus",
  "BuilderCancel",
  "ParseBuilderSlashCommand",
  "builder.plan",
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

if (!validator.includes("case CommandIntent.BuilderStart") ||
    !validator.includes("case CommandIntent.BuilderPlan") ||
    !validator.includes("Builder-plan måste sparas via pending/godkännande.")) {
  failures += 1;
  console.log("FAIL CommandValidatorV1 must validate BuilderMode commands");
} else {
  console.log("PASS CommandValidatorV1 validates BuilderMode commands");
}

for (const marker of ['"/bygg "', '"/bygg svar "', '"/bygg plan"', '"/bygg status"', '"/bygg avbryt"']) {
  if (!dashboard.includes(marker)) {
    failures += 1;
    console.log("FAIL dashboard missing slash autocomplete: " + marker);
  } else {
    console.log("PASS dashboard autocomplete has: " + marker);
  }
}

if (failures > 0) {
  process.exit(1);
}
