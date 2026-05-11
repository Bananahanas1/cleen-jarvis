const fs = require("fs");
const path = require("path");

const programPath = path.join(__dirname, "..", "app", "Program.cs");
const source = fs.readFileSync(programPath, "utf8");

const start = source.indexOf("private static string BuildHelp()");
const end = source.indexOf("private static string DiskGuardStatusTool()");

if (start < 0 || end < 0 || end <= start) {
  throw new Error("Could not locate BuildHelp block.");
}

const helpBlock = source.slice(start, end).toLowerCase();

const forbidden = ["test csharp", "test ollama", "2+2", "hej"];
const required = ["/hjälp", "/status", "/minne visa", "/fil öppna readme.md", "/fil läs docs/project_index.md"];

let failures = 0;

for (const value of forbidden) {
  if (helpBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL forbidden help text present: ${value}`);
  } else {
    console.log(`PASS forbidden help text absent: ${value}`);
  }
}

for (const value of required) {
  if (!helpBlock.includes(value)) {
    failures += 1;
    console.log(`FAIL required help text missing: ${value}`);
  } else {
    console.log(`PASS required help text present: ${value}`);
  }
}

if (failures > 0) {
  process.exit(1);
}
