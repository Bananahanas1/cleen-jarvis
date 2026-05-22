const fs = require("fs");
const path = require("path");

const widgetsJs = fs.readFileSync(
  path.join(__dirname, "..", "dashboard", "widgets-v1.js"), "utf8"
);
const program = fs.readFileSync(
  path.join(__dirname, "..", "app", "Program.cs"), "utf8"
);
const router = fs.readFileSync(
  path.join(__dirname, "..", "app", "CommandRouterV1.cs"), "utf8"
);

let failures = 0;
function check(name, cond) {
  if (!cond) { failures++; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// Router intents
["WidgetLayoutSave", "WidgetLayoutLoad", "WidgetLayoutList", "WidgetLayoutDelete"]
  .forEach(function (x) { check("Router har " + x, router.includes(x)); });

// Router parsning
check("Router parsar 'spara layout som'", router.includes("spara layout som"));
check("Router parsar 'ladda layout'", router.includes("ladda layout"));
check("Router parsar 'ta bort layout'", router.includes("ta bort layout"));

// JS API
check("widgets-v1.js har jarvisWidgetCollectCurrentLayoutV1",
  widgetsJs.includes("jarvisWidgetCollectCurrentLayoutV1"));
check("widgets-v1.js har jarvisWidgetApplyLayoutV1",
  widgetsJs.includes("jarvisWidgetApplyLayoutV1"));
check("widgets-v1.js har jarvisWidgetSetLayoutsV1",
  widgetsJs.includes("jarvisWidgetSetLayoutsV1"));

// Program.cs handlers
["widget_layout_list", "widget_layout_save_finalize", "widget_layout_load", "widget_layout_delete"]
  .forEach(function (x) { check("Program.cs har handler " + x, program.includes(x)); });
check("Program.cs har SendWidgetLayoutsAsync", program.includes("SendWidgetLayoutsAsync"));

// Keyboard shortcuts
check("widgets-v1.js har Ctrl+W handler",
  /e\.ctrlKey[\s\S]{0,150}["']w["']/.test(widgetsJs));
check("widgets-v1.js har Ctrl+M handler",
  /e\.ctrlKey[\s\S]{0,150}["']m["']/.test(widgetsJs));
check("widgets-v1.js har Escape handler",
  widgetsJs.includes('e.key === "Escape"'));
check("widgets-v1.js har Tab-cycling",
  widgetsJs.includes("cycleWidgetFocus"));

if (failures > 0) {
  console.log("\n" + failures + " widget-layouts failure(s)");
  process.exit(1);
}
console.log("\nWidget layouts checks passed.");
