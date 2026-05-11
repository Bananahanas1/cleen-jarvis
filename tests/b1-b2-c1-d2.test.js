// Verifierar B1 ModelRouter + B2 ConversationHistory + C1 WebSearcher + D2 SafeAppLauncher.
const fs = require("fs");
const path = require("path");
const root = path.join(__dirname, "..");

const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const modelRouter = fs.readFileSync(path.join(root, "app", "Brain", "ModelRouter.cs"), "utf8");
const convo = fs.readFileSync(path.join(root, "app", "Brain", "ConversationHistory.cs"), "utf8");
const web = fs.readFileSync(path.join(root, "app", "Brain", "WebSearcher.cs"), "utf8");
const launcher = fs.readFileSync(path.join(root, "app", "Desktop", "SafeAppLauncher.cs"), "utf8");
const html = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;
function check(label, ok) { if (!ok) { failures++; console.log("FAIL " + label); } else console.log("PASS " + label); }

// === B1 ModelRouter ===
check("ModelRouter has CodeRx", modelRouter.includes("CodeRx"));
check("ModelRouter has ReasonRx", modelRouter.includes("ReasonRx"));
check("ModelRouter has PlanRx", modelRouter.includes("PlanRx"));
check("ModelRouter has PickModelForQuery", modelRouter.includes("PickModelForQuery"));
check("ModelRouter has BadgeForModel", modelRouter.includes("BadgeForModel"));
check("ModelRouter respects manual override", modelRouter.includes("manuellt val"));
check("AskOllamaAsync calls ModelRouter", program.includes("ModelRouter.PickModelForQuery"));
check("AskOllamaAsync uses chosenModel", program.includes("model = chosenModel"));
check("Reply includes badge", program.includes("ModelRouter.BadgeForModel"));

// === B2 ConversationHistory ===
check("ConversationHistory has AddUser", convo.includes("AddUser"));
check("ConversationHistory has AddAssistant", convo.includes("AddAssistant"));
check("ConversationHistory has AsOllamaMessages", convo.includes("AsOllamaMessages"));
check("ConversationHistory has Clear", convo.includes("Clear"));
check("ConversationHistory has FormatForChat", convo.includes("FormatForChat"));
check("ConversationHistory caps at MaxTurns", convo.includes("MaxTurns = 20"));
check("ConversationHistory caps at MaxTotalChars", convo.includes("MaxTotalChars = 8000"));
check("AskOllamaAsync stores user turn", program.includes("ConversationHistory.AddUser(text)"));
check("AskOllamaAsync stores assistant turn", program.includes("ConversationHistory.AddAssistant(trimmed)"));
check("AskOllamaAsync uses history in messages", program.includes("ConversationHistory.AsOllamaMessages()"));
check("Router has ConversationShow intent", router.includes("ConversationShow"));
check("Router has ConversationClear intent", router.includes("ConversationClear"));
check("Router parses /historik", router.includes('"historik"'));
check("Router parses /glöm samtal", router.includes('"glöm samtal"'));

// === C1 WebSearcher (Google + Opera) ===
check("WebSearcher has OpenSearchInOpera", web.includes("OpenSearchInOpera"));
check("WebSearcher has FetchTextAsync", web.includes("FetchTextAsync"));
check("WebSearcher uses Google", web.includes("google.com/search"));
check("WebSearcher uses Opera via SafeAppLauncher", web.includes("SafeAppLauncher.TryOpenUrlInOpera"));
check("WebSearcher checks IsInternetOnlineCachedAsync (fetch)", web.includes("IsInternetOnlineCachedAsync"));
check("Router has WebSearch", router.includes("WebSearch"));
check("Router has WebFetch", router.includes("WebFetch"));
check("Router parses /sök", router.includes('"sök"') || router.includes('"sok"'));
check("Router parses /läs", router.includes('"läs"') || router.includes('"las"'));

// === D2 SafeAppLauncher ===
check("SafeAppLauncher has Whitelist", launcher.includes("Whitelist"));
check("SafeAppLauncher has TryLaunch", launcher.includes("TryLaunch"));
check("SafeAppLauncher has ListAllowed", launcher.includes("ListAllowed"));
check("SafeAppLauncher has LogLaunch (audit)", launcher.includes("LogLaunch"));
check("SafeAppLauncher whitelists notepad", launcher.includes('"notepad"'));
check("SafeAppLauncher whitelists vscode", launcher.includes('"vscode"'));
check("SafeAppLauncher whitelists opera", launcher.includes('"opera"'));
check("SafeAppLauncher has TryOpenUrlInOpera", launcher.includes("TryOpenUrlInOpera"));
check("SafeAppLauncher does not allow arguments", launcher.includes("UseShellExecute = true"));
check("Router has ProgramLaunch", router.includes("ProgramLaunch"));
check("Router has ProgramListAllowed", router.includes("ProgramListAllowed"));
check("Router parses öppna program", router.includes('"öppna program"') || router.includes('"oppna program"'));

// === Help text + autocomplete ===
check("Help text mentions /sök", program.includes("/sök <query>"));
check("Help text mentions /läs", program.includes("/läs <url>"));
check("Help text mentions /öppna program", program.includes("/öppna program <namn>"));
check("Help text mentions /historik", program.includes("/historik"));
check("Autocomplete has /sök", html.includes('"/sök "'));
check("Autocomplete has /läs", html.includes('"/läs "'));
check("Autocomplete has /öppna program", html.includes('"/öppna program "'));
check("Autocomplete has /historik", html.includes('"/historik"'));

if (failures > 0) { console.log("\n" + failures + " failure(s)"); process.exit(1); }
console.log("\nAll B1+B2+C1+D2 checks passed.");
