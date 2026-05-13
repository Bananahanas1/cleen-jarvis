const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const runner = fs.readFileSync(path.join(root, "app", "Agents", "DesktopAutopilotRunnerV1.cs"), "utf8");
const todo = fs.readFileSync(path.join(root, "TODO_NEXT.md"), "utf8");

let failures = 0;

function pass(message) {
  console.log("PASS " + message);
}

function fail(message) {
  failures += 1;
  console.log("FAIL " + message);
}

function expectIncludes(source, marker, message) {
  if (!source.includes(marker)) fail(message + " missing: " + marker);
  else pass(message);
}

expectIncludes(dashboard, 'id="desktopAutopilotContinueBtn"', "Overview has continue button");
expectIncludes(dashboard, 'data-command="/autopilot desktop fortsätt"', "Continue button sends Swedish continue command");
expectIncludes(dashboard, "desktopAutopilotCanContinue", "Overview reads continue availability");
expectIncludes(dashboard, "desktopAutopilotContinueCommand", "Overview reads continue command from payload");
expectIncludes(dashboard, "desktopAutopilotContinueBtn.disabled", "Overview disables continue button when inactive");
expectIncludes(dashboard, 'style.display = canContinue ? "" : "none"', "Overview hides inactive continue button");

expectIncludes(program, '["desktopAutopilotCanContinue"]', "Overview payload exposes continue availability");
expectIncludes(program, '["desktopAutopilotContinueCommand"]', "Overview payload exposes continue command");
expectIncludes(program, "AppendDesktopAutopilotContinueHintV1", "Approved desktop action gets continue hint");
expectIncludes(program, "/autopilot desktop fortsätt", "Approved desktop action mentions continue command");

if (!runner.includes('Replace("å", "a")') && !runner.includes('"fortsätt"')) {
  fail("Desktop runner normalizes or accepts Swedish continue aliases");
} else {
  pass("Desktop runner normalizes or accepts Swedish continue aliases");
}

expectIncludes(todo, "Desktop Autopilot auto-continue UI", "TODO tracks completed auto-continue UI");
expectIncludes(todo, "Agent VM Sandbox", "TODO records VM sandbox direction for freer agent control");

if (failures > 0) process.exit(1);
console.log("\nDesktop autopilot continue UI checks passed.");
