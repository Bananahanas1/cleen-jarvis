const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const contextPath = path.join(root, "app", "Brain", "ContextPackV1.cs");
const routerPath = path.join(root, "app", "Brain", "HybridModelRouterV1.cs");
const programPath = path.join(root, "app", "Program.cs");
const dashboardPath = path.join(root, "dashboard", "index.html");
const todoPath = path.join(root, "TODO_NEXT.md");
const statePath = path.join(root, "CURRENT_STATE.md");

const context = fs.existsSync(contextPath) ? fs.readFileSync(contextPath, "utf8") : "";
const router = fs.existsSync(routerPath) ? fs.readFileSync(routerPath, "utf8") : "";
const program = fs.readFileSync(programPath, "utf8");
const dashboard = fs.readFileSync(dashboardPath, "utf8");
const todo = fs.readFileSync(todoPath, "utf8");
const state = fs.readFileSync(statePath, "utf8");

let failures = 0;
function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("ContextPackV1 exists", fs.existsSync(contextPath));
check("ContextPackV1 has clear bounded inputs", context.includes("MemoryContext") && context.includes("ProjectContext") && context.includes("TerminalContext"));
check("ContextPackV1 states models may not execute actions", context.includes("Modellen fÃ¥r inte kÃ¶ra tools") || context.includes("Modellen får inte köra tools"));
check("ContextPackV1 limits context size", context.includes("MaxSectionChars") && context.includes("LimitSection"));

check("HybridModelRouterV1 exists", fs.existsSync(routerPath));
check("Hybrid router has offline and auto modes", router.includes("LocalOnly") && router.includes("AutoFree"));
check("Hybrid router knows free online providers", router.includes("Groq") && router.includes("Gemini") && router.includes("GitHubModels"));
check("Hybrid router reads keys only from environment", router.includes("GROQ_API_KEY") && router.includes("GEMINI_API_KEY") && router.includes("GITHUB_TOKEN"));
check("Hybrid router never stores secret values in status", router.includes("IsConfigured") && router.includes("Environment.GetEnvironmentVariable") && !router.includes("WriteAllText"));

check("Program exposes hybrid model commands", program.includes("HybridProviderStatusTool") && program.includes("SetHybridModelModeTool"));
check("Program status/overview includes model backend", program.includes("BuildModelProviderOverviewLineV1") && program.includes("[\"modelProvider\"]"));
check("Program chat fallback uses hybrid model path", program.includes("AskHybridModelAsync(text)") && !program.includes("return await AskOllamaAsync(text);"));
check("Program local router still runs before hybrid chat", program.indexOf("TryHandleCommandRouterV1UiAsync(text)") < program.indexOf("AskHybridModelAsync(text)"));
check("Program gives online provider prompt only as advisor", program.includes("Du fÃ¥r inte utfÃ¶ra lokala actions") || program.includes("Du får inte utföra lokala actions"));

check("Dashboard shows model provider", dashboard.includes("visualModelProviderState") && dashboard.includes("Modellmotor"));
check("Dashboard has quick model provider button", dashboard.includes("data-command=\"/modell provider\"") || dashboard.includes("data-command=\"/modell status\""));

check("Docs mention hybrid router slice", todo.includes("HybridModelRouterV1") && state.includes("HybridModelRouterV1"));

if (failures > 0) {
  console.log(`\n${failures} hybrid model/context failure(s)`);
  process.exit(1);
}

console.log("\nHybrid model/context checks passed.");
