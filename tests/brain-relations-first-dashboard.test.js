const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const builder = fs.readFileSync(path.join(root, "app", "Brain", "FileGraphBuilder.cs"), "utf8");

let failures = 0;

function check(label, ok) {
  if (!ok) {
    failures += 1;
    console.log("FAIL " + label);
  } else {
    console.log("PASS " + label);
  }
}

check("FileGraphBuilder declares relations-first graph meta", builder.includes("GraphMeta") && builder.includes('"relations-first"'));
check("FileGraphBuilder uses cache builder version", builder.includes("BuilderVersion") && builder.includes("builderVersion"));
check("Dashboard has brain graph mode field", dashboard.includes('id="brainGraphMode"'));
check("Dashboard has hidden generated count field", dashboard.includes('id="brainHiddenGeneratedCount"'));
check("Dashboard renders graph meta from payload", dashboard.includes("graph.meta") && dashboard.includes("hiddenGeneratedFiles"));
check("Dashboard does not expose json as default Brain filter", !dashboard.includes('data-kind="json" checked'));

if (failures > 0) process.exit(1);
console.log("\nBrain relations-first dashboard checks passed.");
