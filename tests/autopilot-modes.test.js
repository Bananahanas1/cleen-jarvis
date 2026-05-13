const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const modePath = path.join(root, "app", "Agents", "AgentAutopilotModeV1.cs");
const mode = fs.existsSync(modePath) ? fs.readFileSync(modePath, "utf8") : "";
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const validator = fs.readFileSync(path.join(root, "app", "CommandValidatorV1.cs"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const todo = fs.readFileSync(path.join(root, "TODO_NEXT.md"), "utf8");
const sessionLog = fs.readFileSync(path.join(root, "docs", "SESSION_LOG.md"), "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) {
    failures += 1;
    console.log(`FAIL ${name}`);
  } else {
    console.log(`PASS ${name}`);
  }
}

check("AgentAutopilotModeV1 exists", fs.existsSync(modePath));
check("AgentAutopilotModeV1 defines all five levels",
  ["Safe", "Approval", "BrowserAutopilot", "DesktopAutopilot", "BuildAgent"].every(x => mode.includes(x)));
check("Autopilot safety names Opera policy and blocked scopes",
  mode.includes("BrowserPolicyV1") && mode.includes("F:\\New project") && mode.includes("kill-switch") && mode.includes("betalningar"));
check("Desktop Autopilot is broad app control with denylist",
  mode.includes("BroadDesktopControl") && mode.includes("denylist") && mode.includes("nastan alla normala appar"));
check("Autopilot has scoped mission state",
  mode.includes("Mission") && mode.includes("StartedAt") && mode.includes("SetMode") && mode.includes("Stop"));

check("Router exposes autopilot intents",
  ["AutopilotStatus", "AutopilotSetMode", "AutopilotStop"].every(x => router.includes(x)));
check("Router parses /autopilot status and stop",
  router.includes("ParseAutopilotSlashCommand") && router.includes("autopilot.status") && router.includes("autopilot.stop"));
check("Router parses browser desktop build levels",
  router.includes("BrowserAutopilot") && router.includes("DesktopAutopilot") && router.includes("BuildAgent"));
check("Autopilot commands stay local",
  router.includes("ToolName = \"autopilot.mode\"") && router.includes("ShouldSendToOllama = false"));
check("Validator requires mission for scoped autopilot",
  validator.includes("case CommandIntent.AutopilotSetMode") && validator.includes("Autopilot-uppdrag saknas"));

check("Program exposes autopilot tools",
  ["AgentAutopilotStatusTool", "AgentAutopilotSetTool", "AgentAutopilotStopTool", "BuildAgentAutopilotOverviewLineV1"].every(x => program.includes(x)));
check("Program overview payload includes autopilot",
  program.includes("[\"autopilot\"] = BuildAgentAutopilotOverviewLineV1()"));
check("Program stops desktop gate when autopilot safe/stop",
  program.includes("DesktopActionGate.Disable()") && program.includes("AgentAutopilotModeV1.Stop"));
check("Program keeps Browser Autopilot on Opera policy",
  program.includes("BrowserPolicyV1.DisplayName") && program.includes("BrowserPolicyV1.InternalAutomationEngine"));

check("Dashboard shows autopilot state",
  dashboard.includes("visualAutopilotState") && dashboard.includes("Autopilot"));
check("Dashboard has autopilot quick buttons",
  dashboard.includes('data-command="/autopilot status"') &&
  dashboard.includes('data-command="/autopilot approval"') &&
  dashboard.includes('data-command="/autopilot stop"'));
check("Dashboard renders autopilot overview key",
  dashboard.includes("overview.autopilot") || dashboard.includes("overview.Autopilot"));

check("Docs track Autopilot Modes V1",
  todo.includes("Agent Autopilot Modes V1") && sessionLog.includes("Agent Autopilot Modes V1"));

if (failures > 0) {
  console.log(`\n${failures} autopilot mode failure(s)`);
  process.exit(1);
}

console.log("\nAutopilot mode checks passed.");
