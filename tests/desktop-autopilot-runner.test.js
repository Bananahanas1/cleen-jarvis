const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const runnerPath = path.join(root, "app", "Agents", "DesktopAutopilotRunnerV1.cs");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const todo = fs.readFileSync(path.join(root, "TODO_NEXT.md"), "utf8");

let failures = 0;

function pass(message) {
  console.log("PASS " + message);
}

function fail(message) {
  failures += 1;
  console.log("FAIL " + message);
}

if (!fs.existsSync(runnerPath)) {
  fail("DesktopAutopilotRunnerV1 exists");
} else {
  pass("DesktopAutopilotRunnerV1 exists");
}

const runner = fs.existsSync(runnerPath) ? fs.readFileSync(runnerPath, "utf8") : "";

for (const marker of [
  "DesktopAutopilotRunnerV1",
  "StartOrContinue",
  "BuildVisionInstruction",
  "MaxStepsPerMission",
  "BlockedTerms",
  "PendingApprovalV1",
  "kill-switch",
  "BroadDesktopControl",
  "DesktopActionGate"
]) {
  if (!runner.includes(marker)) fail("Runner missing marker: " + marker);
  else pass("Runner has marker: " + marker);
}

for (const blocked of [
  "password",
  "betal",
  "bankid",
  "secret",
  "admin",
  "powershell",
  "regedit",
  "taskmgr"
]) {
  if (!runner.includes(blocked)) fail("Runner blocked terms missing: " + blocked);
  else pass("Runner blocks sensitive term: " + blocked);
}

for (const forbidden of [
  "DesktopActionExecutor.Execute",
  "SetCursorPos",
  "mouse_event",
  "SendKeys.SendWait",
  "Process.Start"
]) {
  if (runner.includes(forbidden)) fail("Runner must not execute directly: " + forbidden);
  else pass("Runner avoids direct execution: " + forbidden);
}

if (!program.includes("DesktopAutopilotRunnerV1.StartOrContinue")) {
  fail("Program starts DesktopAutopilotRunnerV1 for desktop mode");
} else {
  pass("Program starts DesktopAutopilotRunnerV1 for desktop mode");
}

if (!program.includes("await DesktopVisionRequestToolAsync(desktopRun.Instruction)")) {
  fail("Program turns desktop runner step into pending vision request");
} else {
  pass("Program turns desktop runner step into pending vision request");
}

if (!program.includes('RecordWorkMonitorV1("Desktop Autopilot"')) {
  fail("Program records desktop autopilot monitor events");
} else {
  pass("Program records desktop autopilot monitor events");
}

if (!todo.includes("Desktop Autopilot Runner V1")) {
  fail("Docs track Desktop Autopilot Runner V1");
} else {
  pass("Docs track Desktop Autopilot Runner V1");
}

if (failures > 0) process.exit(1);
console.log("\nDesktop autopilot runner checks passed.");
