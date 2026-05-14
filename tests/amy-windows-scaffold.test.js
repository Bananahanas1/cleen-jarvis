const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const amy = path.join(root, "amy-windows");

const required = [
  "README.md",
  ".env.example",
  "requirements.txt",
  "Install-AmyWindows.ps1",
  "Start-AmyWindows.ps1",
  "backend/app/main.py",
  "backend/app/config.py",
  "backend/app/database.py",
  "backend/app/services/providers.py",
  "backend/app/services/browser_agent.py",
  "frontend/package.json",
  "frontend/src/main.ts",
  "frontend/src/orb.ts",
  "frontend/src/style.css"
];

let failures = 0;
function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

for (const rel of required) {
  check(`exists ${rel}`, fs.existsSync(path.join(amy, rel)));
}

const main = fs.readFileSync(path.join(amy, "backend/app/main.py"), "utf8");
const db = fs.readFileSync(path.join(amy, "backend/app/database.py"), "utf8");
const providers = fs.readFileSync(path.join(amy, "backend/app/services/providers.py"), "utf8");
const browser = fs.readFileSync(path.join(amy, "backend/app/services/browser_agent.py"), "utf8");
const frontendPkg = fs.readFileSync(path.join(amy, "frontend/package.json"), "utf8");
const ui = fs.readFileSync(path.join(amy, "frontend/src/main.ts"), "utf8");
const readme = fs.readFileSync(path.join(amy, "README.md"), "utf8");

check("FastAPI app exposes setup endpoint", main.includes("FastAPI") && main.includes("/api/setup"));
check("SQLite schema covers memory tasks notes", db.includes("CREATE TABLE IF NOT EXISTS memory") && db.includes("CREATE TABLE IF NOT EXISTS tasks") && db.includes("CREATE TABLE IF NOT EXISTS notes"));
check("Provider adapter never returns raw API keys", providers.includes("configured") && !providers.includes("api_key\":"));
check("Browser agent is approval-first", browser.includes("needs_approval") && browser.includes("DENY_TERMS"));
check("Frontend uses Vite and Three", frontendPkg.includes("\"vite\"") && frontendPkg.includes("\"three\""));
check("Dashboard has orb and provider panels", ui.includes("orbCanvas") && ui.includes("providers") && ui.includes("browserPlan"));
check("README stays under markdown limit", readme.length < 14000);

if (failures > 0) process.exit(1);
console.log("\nAmy Windows scaffold checks passed.");
