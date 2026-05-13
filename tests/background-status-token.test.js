const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const estimatorPath = path.join(root, "app", "Brain", "ContextBudgetEstimatorV1.cs");
const queuePath = path.join(root, "app", "Jobs", "BackgroundJobQueueV1.cs");
const programPath = path.join(root, "app", "Program.cs");

const estimator = fs.existsSync(estimatorPath) ? fs.readFileSync(estimatorPath, "utf8") : "";
const queue = fs.readFileSync(queuePath, "utf8");
const program = fs.readFileSync(programPath, "utf8");

let failures = 0;
function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("ContextBudgetEstimatorV1 exists", fs.existsSync(estimatorPath));
check("estimator can estimate text tokens", estimator.includes("EstimateTokens"));
check("estimator can estimate message tokens", estimator.includes("EstimateMessages"));
check("estimator has compact format", estimator.includes("FormatCompact"));

check("job record stores current step", queue.includes("CurrentStep"));
check("job record stores last action", queue.includes("LastAction"));
check("job record stores next action", queue.includes("NextAction"));
check("job record stores context token estimate", queue.includes("ContextEstimateTokens"));
check("job status prints token/context", queue.includes("Token/context"));
check("job status prints next action", queue.includes("Nästa:"));
check("job update refreshes work status", queue.includes("UpdateWorkStatus"));

check("AskOllamaAsync estimates input messages", program.includes("ContextBudgetEstimatorV1.EstimateMessages"));
check("AskOllamaAsync estimates output tokens", program.includes("ContextBudgetEstimatorV1.EstimateTokens(trimmed)"));
check("AskOllamaAsync returns ctx estimate", program.includes("ctx≈"));
check("AskOllamaAsync returns answer token estimate", program.includes("svar≈"));

if (failures > 0) {
  console.log(`\n${failures} background status/token failure(s)`);
  process.exit(1);
}

console.log("\nBackground status/token checks passed.");
