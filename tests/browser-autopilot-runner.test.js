const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const runnerPath = path.join(root, "app", "Agents", "BrowserAutopilotRunnerV1.cs");
const runner = fs.existsSync(runnerPath) ? fs.readFileSync(runnerPath, "utf8") : "";
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
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

check("BrowserAutopilotRunnerV1 exists", fs.existsSync(runnerPath));
check("Runner is bound to Opera policy",
  runner.includes("BrowserPolicyV1.DisplayName") && runner.includes("BrowserPolicyV1.InternalAutomationEngine"));
check("Runner blocks sensitive missions",
  runner.includes("BlockedTerms") &&
  ["login", "password", "betal", "skicka", "publicer", "bankid"].every(x => runner.toLowerCase().includes(x)));
check("Runner can open/search through existing safe browser path",
  runner.includes("WebSearcher.OpenSearchInOpera") && runner.includes("SafeAppLauncher.TryOpenUrlInOpera"));
check("Runner can read URL text without browser clicks",
  runner.includes("WebSearcher.FetchTextAsync") && runner.includes("TryExtractUrl"));
check("Runner does not click/type yet",
  runner.includes("NoClickOrTypeInV1") && !runner.includes("DesktopActionExecutor.Execute"));

check("Program uses async autopilot setter",
  program.includes("AgentAutopilotSetToolAsync") && !program.includes("AgentAutopilotSetTool("));
check("Program starts BrowserAutopilotRunnerV1 for browser mode",
  program.includes("BrowserAutopilotRunnerV1.StartAsync") && program.includes("AgentAutopilotLevelV1.BrowserAutopilot"));
check("Program records browser autopilot monitor events",
  program.includes("RecordWorkMonitorV1(\"Browser Autopilot\""));

check("Docs track Browser Autopilot Runner V1",
  todo.includes("Browser Autopilot Runner V1") && sessionLog.includes("Browser Autopilot Runner V1"));

if (failures > 0) {
  console.log(`\n${failures} browser autopilot runner failure(s)`);
  process.exit(1);
}

console.log("\nBrowser autopilot runner checks passed.");
