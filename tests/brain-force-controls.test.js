const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const html = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;

function pass(message) {
  console.log("PASS " + message);
}

function fail(message) {
  failures += 1;
  console.log("FAIL " + message);
}

for (const marker of [
  'id="brainCenterForce"',
  'id="brainRepelForce"',
  'id="brainLinkForce"',
  'id="brainCenterForceValue"',
  'id="brainRepelForceValue"',
  'id="brainLinkForceValue"',
  "brain-force-control",
  "brainForceSettingsV1",
  "wireBrainForceSliderV1",
  "applyBrainForceSliderV1"
]) {
  if (!html.includes(marker)) fail("Brain force control missing: " + marker);
  else pass("Brain force control has: " + marker);
}

for (const marker of [
  "brainForceSettingsV1.repel",
  "brainForceSettingsV1.link",
  "brainForceSettingsV1.center",
  "* brainForceSettingsV1.repel",
  "* brainForceSettingsV1.link",
  "* brainForceSettingsV1.center",
  "frame = 0"
]) {
  if (!html.includes(marker)) fail("Brain force setting is not wired: " + marker);
  else pass("Brain force setting wired: " + marker);
}

if (failures > 0) process.exit(1);
console.log("\nBrain force controls checks passed.");
