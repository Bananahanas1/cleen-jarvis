const fs = require("fs");
const path = require("path");

const projectPath = path.join(__dirname, "..", "app", "JarvisClean.csproj");
const project = fs.readFileSync(projectPath, "utf8");

const requiredExclusions = [
  'Compile Remove="JarvisCLI\\**\\*.cs"',
  'Compile Remove="PocketBridge\\**\\*.cs"'
];

let failures = 0;

for (const marker of requiredExclusions) {
  if (!project.includes(marker)) {
    failures += 1;
    console.log(`FAIL app project must exclude experimental nested source: ${marker}`);
  } else {
    console.log(`PASS app project excludes experimental nested source: ${marker}`);
  }
}

if (failures > 0) {
  process.exit(1);
}
