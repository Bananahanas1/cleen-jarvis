// Verifierar att OllamaAgentHarness är read-only och inte kan skriva filer eller köra terminal.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const harness = fs.readFileSync(path.join(root, "app", "Agents", "OllamaAgentHarness.cs"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");

let failures = 0;

// Read-only tools defined
const readOnlyTools = ["read_file", "list_files", "glob_files", "grep_text", "get_project_context"];
for (const t of readOnlyTools) {
  if (!harness.includes(`"${t}"`)) {
    failures += 1;
    console.log(`FAIL OllamaAgentHarness missing read tool: ${t}`);
  } else {
    console.log(`PASS OllamaAgentHarness defines: ${t}`);
  }
}

// Write/exec tools must be EXPLICITLY blocked, not just absent.
const blockedTools = ["write_file", "replace_in_file", "run_command", "run_shell", "delete_file"];
for (const t of blockedTools) {
  if (!harness.includes(`"${t}"`)) {
    failures += 1;
    console.log(`FAIL OllamaAgentHarness must explicitly block: ${t}`);
  } else {
    console.log(`PASS OllamaAgentHarness explicitly blocks: ${t}`);
  }
}

// Block-message must mention PendingApproval flow.
if (!harness.includes("approval-popup") && !harness.includes("/fil skapa")) {
  failures += 1;
  console.log("FAIL Block message should redirect users to /fil skapa or approval popup");
} else {
  console.log("PASS Block message redirects to safe approval flow");
}

// Path safety
if (!harness.includes("ResolveSafePath") || !harness.includes("StartsWith(fullRoot")) {
  failures += 1;
  console.log("FAIL Agent must enforce path-traversal guard");
} else {
  console.log("PASS Agent enforces path-traversal guard");
}

// MaxAgentSteps must be reasonable (not unbounded)
if (!harness.includes("MaxAgentSteps = 6") && !harness.includes("MaxAgentSteps = 8")) {
  failures += 1;
  console.log("FAIL MaxAgentSteps should be bounded (4-8)");
} else {
  console.log("PASS MaxAgentSteps is bounded");
}

// Slash routing
if (!router.includes("AgentRun") || !router.includes('command == "agent"')) {
  failures += 1;
  console.log("FAIL CommandRouterV1 missing /agent routing");
} else {
  console.log("PASS CommandRouterV1 routes /agent");
}

// Program.cs wiring
if (!program.includes("RunOllamaAgentToolAsync") || !program.includes("_ollamaAgent")) {
  failures += 1;
  console.log("FAIL Program.cs missing agent wiring");
} else {
  console.log("PASS Program.cs wires agent");
}

// Help text mentions /agent
if (!program.includes("/agent ")) {
  failures += 1;
  console.log("FAIL Help text missing /agent");
} else {
  console.log("PASS Help text mentions /agent");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll ollama-agent-safety checks passed.");
}
