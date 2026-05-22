const fs = require("fs");
const path = require("path");

const widgetsJs = fs.readFileSync(
  path.join(__dirname, "..", "dashboard", "widgets-v1.js"), "utf8"
);

let failures = 0;
function check(name, cond) {
  if (!cond) { failures++; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// Snap-grid model
check("widgets-v1.js har GRID_COLS = 12", /GRID_COLS\s*=\s*12/.test(widgetsJs));
check("widgets-v1.js har GRID_ROWS = 8", /GRID_ROWS\s*=\s*8/.test(widgetsJs));
check("widgets-v1.js har pixelsToGridCell helper",
  widgetsJs.includes("pixelsToGridCell"));
check("widgets-v1.js har gridCellToPixels helper",
  widgetsJs.includes("gridCellToPixels"));
check("widgets-v1.js har snapToGrid funktion",
  widgetsJs.includes("snapToGrid"));
check("widgets-v1.js drag använder snapToGrid vid mouseup",
  /mouseup[\s\S]{0,400}snapToGrid/.test(widgetsJs));
check("widgets-v1.js resize använder snapToGrid vid mouseup",
  /resizing[\s\S]{0,800}snapToGrid/.test(widgetsJs));

const widgetsCss = fs.readFileSync(
  path.join(__dirname, "..", "dashboard", "widgets-v1.css"), "utf8"
);
check("widgets-v1.css har .widget-grid-overlay",
  widgetsCss.includes(".widget-grid-overlay"));
check("widgets-v1.css har .widget-grid-overlay.visible",
  widgetsCss.includes(".widget-grid-overlay.visible"));
check("widgets-v1.js har ensureGridOverlay",
  widgetsJs.includes("ensureGridOverlay"));

if (failures > 0) {
  console.log("\n" + failures + " snap-grid failure(s)");
  process.exit(1);
}
console.log("\nWidget snap-grid checks passed.");
