const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const bridge = fs.readFileSync(path.join(root, "app", "Bridges", "UiTarsBridge.cs"), "utf8");
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

check("UiTarsBridge resolves package manager before start", bridge.includes("ResolveUiTarsPackageManagerV1"));
check("UiTarsBridge searches for pnpm and corepack", bridge.includes('"pnpm.cmd"') && bridge.includes('"corepack.cmd"'));
check("StartAsync uses resolved package manager file", bridge.includes("packageManager.FileName") && bridge.includes("packageManager.ArgumentsPrefix"));
check("corepack fallback runs pnpm", bridge.includes('ArgumentsPrefix: "pnpm "') || bridge.includes('ArgumentsPrefix = "pnpm "'));
check("missing package manager message tells how to fix", bridge.includes("corepack enable") && bridge.includes("npm install -g pnpm"));
check("status reports package manager capability", bridge.includes("Package manager:") && bridge.includes("DisplayName"));
check("TODO tracks UI-TARS package manager fallback", todo.includes("UI-TARS package manager fallback"));

if (failures > 0) process.exit(1);
console.log("\nUI-TARS package manager checks passed.");
