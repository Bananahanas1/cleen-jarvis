const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const launcher = fs.readFileSync(path.join(root, "app", "Desktop", "SafeAppLauncher.cs"), "utf8");
const web = fs.readFileSync(path.join(root, "app", "Brain", "WebSearcher.cs"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const pyAgent = fs.readFileSync(path.join(root, "python", "jarvis_web_agent.py"), "utf8");
const policyPath = path.join(root, "app", "Desktop", "BrowserPolicyV1.cs");
const policy = fs.existsSync(policyPath) ? fs.readFileSync(policyPath, "utf8") : "";

let failures = 0;
function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("BrowserPolicyV1 exists", fs.existsSync(policyPath));
check("BrowserPolicyV1 names OperaGX/Opera", policy.includes('DisplayName = "OperaGX/Opera"'));
check("BrowserPolicyV1 blocks non-Opera visible browsers", policy.includes('"chrome"') && policy.includes('"chromium"') && policy.includes("IsBlockedBrowserName"));
check("BrowserPolicyV1 allows internal Chromium engine", policy.includes("InternalAutomationEngine"));

for (const blocked of ['["chrome"]', '["edge"]', '["firefox"]']) {
  check(`SafeAppLauncher has no visible ${blocked} target`, !launcher.includes(blocked));
}

check("SafeAppLauncher routes browser aliases through policy", launcher.includes("BrowserPolicyV1.IsBrowserAlias"));
check("SafeAppLauncher blocks non-Opera browser names", launcher.includes("BrowserPolicyV1.IsBlockedBrowserName"));
check("SafeAppLauncher uses BrowserPolicyV1 candidates for Opera", launcher.includes("BrowserPolicyV1.ExecutableCandidates"));

check("WebSearcher says OperaGX/Opera", web.includes("OperaGX/Opera"));
check("Help text no longer says DuckDuckGo", !program.includes("DuckDuckGo"));
check("Help text no longer advertises chrome", !program.toLowerCase().includes("whitelist: notepad/vscode/chrome"));

check("Python web agent documents internal Chromium engine", pyAgent.includes("INTERNAL_AUTOMATION_ENGINE"));
check("Python web agent launches isolated Playwright Chromium", pyAgent.includes("chromium.launch") && pyAgent.includes("new_context"));
check("Python web agent does not force Opera executable", !pyAgent.includes("executable_path"));

if (failures > 0) {
  console.log(`\n${failures} browser policy failure(s)`);
  process.exit(1);
}

console.log("\nBrowser policy checks passed.");
