const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const runnerPath = path.join(root, "tests", "run-full-smoke.ps1");
const guidePath = path.join(root, "docs", "FULL_SYSTEM_TEST.md");
const testsReadmePath = path.join(root, "tests", "README.md");

let failures = 0;

function pass(message) {
  console.log("PASS " + message);
}

function fail(message) {
  failures += 1;
  console.log("FAIL " + message);
}

if (!fs.existsSync(runnerPath)) fail("full smoke PowerShell runner exists");
else pass("full smoke PowerShell runner exists");

if (!fs.existsSync(guidePath)) fail("full system test guide exists");
else pass("full system test guide exists");

const runner = fs.existsSync(runnerPath) ? fs.readFileSync(runnerPath, "utf8") : "";
const guide = fs.existsSync(guidePath) ? fs.readFileSync(guidePath, "utf8") : "";
const testsReadme = fs.readFileSync(testsReadmePath, "utf8");

for (const marker of [
  "Run-NodeRegression",
  "Run-CommandRouterTests",
  "Run-AppBuild",
  "Test-MarkdownLength",
  "Write-TestLog",
  "Start-Sleep -Milliseconds",
  "data/test-runs",
  "ALL CHECKS PASSED",
  "dotnet run --project",
  "dotnet build",
  "node $_.FullName"
]) {
  if (!runner.includes(marker)) fail("runner missing marker: " + marker);
  else pass("runner has marker: " + marker);
}

for (const marker of [
  "run-full-smoke.ps1",
  "/autopilot status",
  "/autopilot browser",
  "/autopilot desktop",
  "/jobb status",
  "/projekt audit",
  "PendingApprovalV1",
  "OperaGX/Opera"
]) {
  if (!guide.includes(marker)) fail("guide missing marker: " + marker);
  else pass("guide has marker: " + marker);
}

if (!testsReadme.includes("run-full-smoke.ps1")) {
  fail("tests README points to full smoke runner");
} else {
  pass("tests README points to full smoke runner");
}

if (failures > 0) process.exit(1);
console.log("\nFull smoke runner checks passed.");
