const fs = require("fs");
const path = require("path");

const htmlPath = path.join(__dirname, "..", "dashboard", "index.html");
const html = fs.readFileSync(htmlPath, "utf8");

const requiredMarkers = [
  "scrollbar-color:",
  "scrollbar-width: thin",
  "::-webkit-scrollbar",
  "::-webkit-scrollbar-track",
  "::-webkit-scrollbar-thumb",
  "::-webkit-scrollbar-thumb:hover",
  "::-webkit-scrollbar-corner"
];

const scrollTargets = [
  "#fileTree",
  "#editorArea",
  "#terminalOutput",
  "#visualPanel",
  "#messages",
  "#suggestions",
  "#approvalPreview",
  "#diffBody"
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL scrollbar style marker missing: ${marker}`);
  } else {
    console.log(`PASS scrollbar style marker present: ${marker}`);
  }
}

for (const target of scrollTargets) {
  if (!html.includes(target)) {
    failures += 1;
    console.log(`FAIL expected scrollable target missing: ${target}`);
  } else {
    console.log(`PASS scrollable target present: ${target}`);
  }
}

if (failures > 0) {
  process.exit(1);
}
