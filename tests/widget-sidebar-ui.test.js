const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;
function check(name, cond) {
  if (!cond) { failures++; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

check("Dashboard har #widgetSidebar", dashboard.includes('id="widgetSidebar"'));
check("Dashboard har #widgetSidebarToggle", dashboard.includes('id="widgetSidebarToggle"'));
check("Dashboard har widget-sidebar ikoner",
  dashboard.includes('data-widget-type="text"') &&
  dashboard.includes('data-widget-type="image"') &&
  dashboard.includes('data-widget-type="iframe"') &&
  dashboard.includes('data-widget-type="webcam"') &&
  dashboard.includes('data-widget-type="chat-mini"'));
check("Dashboard har sidebar-collapse-toggle handler",
  /widgetSidebarToggle[\s\S]{0,300}data-collapsed/.test(dashboard));
check("Dashboard har widget-create-on-click handler",
  /data-widget-type[\s\S]{0,500}JarvisWidgetsV1\.create/.test(dashboard));

if (failures > 0) {
  console.log("\n" + failures + " widget-sidebar-ui failure(s)");
  process.exit(1);
}
console.log("\nWidget sidebar UI checks passed.");
