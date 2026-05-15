const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const graph = fs.readFileSync(path.join(root, "app", "Brain", "FileGraphBuilder.cs"), "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) {
    failures += 1;
    console.log("FAIL " + name);
  } else {
    console.log("PASS " + name);
  }
}

check("Program.cs does not enumerate all project files before pruning",
  !program.includes('Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories)'));
check("Program.cs does not enumerate all project folders before pruning",
  !program.includes('Directory.GetDirectories(ProjectRoot, "*", SearchOption.AllDirectories)'));
check("Program.cs has pruned file traversal helper",
  program.includes("EnumerateProjectFilesPrunedV1"));
check("Program.cs has pruned folder traversal helper",
  program.includes("EnumerateProjectDirectoriesPrunedV1"));
check("Project Explorer excludes venv/logs/runtimes",
  [".venv", "logs", "runtimes"].every((name) => program.includes(`"${name}"`)));

check("FileGraphBuilder does not enumerate all files before pruning",
  !graph.includes('Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)'));
check("FileGraphBuilder uses pruned recursive traversal",
  graph.includes("EnumerateSafePruned"));
check("FileGraphBuilder excludes venv/logs/runtimes",
  [".venv", "logs", "runtimes"].every((name) => graph.includes(`"${name}"`)));
check("Brain cache version bumps when pruning changes",
  /BuilderVersion\s*=\s*"relations-first-v1-20260515-pruned"/.test(graph));

if (failures > 0) {
  console.log(`\nProject scan pruning performance checks failed: ${failures}`);
  process.exit(1);
}

console.log("\nProject scan pruning performance checks passed.");
