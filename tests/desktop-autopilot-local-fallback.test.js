const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const launcher = fs.readFileSync(path.join(root, "app", "Desktop", "SafeAppLauncher.cs"), "utf8");
const todo = fs.readFileSync(path.join(root, "TODO_NEXT.md"), "utf8");

let failures = 0;

function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("SafeAppLauncher can resolve simple launch text", launcher.includes("TryResolveAllowedAppNameFromText"));
check("SafeAppLauncher normalizes Swedish open words", launcher.includes("oppna") && launcher.includes("starta"));
check("SafeAppLauncher can resolve notepad from text", launcher.includes('"notepad"') && launcher.includes("NormalizeLaunchTextV1"));
check("Program has desktop autopilot local fallback", program.includes("TryRunDesktopAutopilotLocalFallbackV1"));
check("Desktop fallback runs before UI-TARS vision", program.indexOf("TryRunDesktopAutopilotLocalFallbackV1") > -1 && program.indexOf("TryRunDesktopAutopilotLocalFallbackV1") < program.indexOf("DesktopVisionRequestToolAsync(desktopRun.Instruction)"));
check("Desktop fallback uses SafeAppLauncher", program.includes("SafeAppLauncher.TryResolveAllowedAppNameFromText") && program.includes("SafeAppLauncher.TryLaunch"));
check("Desktop fallback explains UI-TARS was not needed", program.includes("UI-TARS behövdes inte"));
check("Desktop fallback stops completed one-step autopilot", program.includes("AgentAutopilotModeV1.Stop(out _)") && program.includes("Desktop Autopilot stoppades"));
check("TODO tracks local fallback", todo.includes("Desktop Autopilot local app fallback"));

if (failures > 0) process.exit(1);
console.log("\nDesktop autopilot local fallback checks passed.");
