const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
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

check("overview has command panel grid", dashboard.includes("monitor-panel-grid"));
check("overview has command panel component", dashboard.includes("monitor-command-panel"));
check("overview has panel titles", dashboard.includes("monitor-panel-title") && dashboard.includes(">Projekt<") && dashboard.includes(">Tasks<") && dashboard.includes(">Autopilot<"));
check("overview has short panel descriptions", dashboard.includes("monitor-panel-desc") && dashboard.includes("Vad den gör:"));
check("project commands are grouped", dashboard.includes('data-panel="project"') && dashboard.includes('data-command="/projekt index"') && dashboard.includes('data-command="/projekt audit"'));
check("task commands and quick input are grouped", dashboard.includes('data-panel="tasks"') && dashboard.includes('id="taskQuickInput"') && dashboard.includes('id="taskQuickAddBtn"'));
check("autopilot commands are grouped", dashboard.includes('id="desktopAutopilotPanelV1"') && dashboard.includes('data-command="/autopilot status"') && dashboard.includes('id="desktopAutopilotContinueBtn"'));
check("model and terminal commands are grouped", dashboard.includes('data-panel="model"') && dashboard.includes('data-panel="terminal"'));
check("old flat monitor action row is not the main layout", !dashboard.includes('<div class="monitor-actions">'));
check("TODO tracks overview panel organization", todo.includes("Oversikt command panels"));

if (failures > 0) process.exit(1);
console.log("\nOverview command panel checks passed.");
