// Verifierar att Brain-fönstret är på plats och att slash-routing finns.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const csharp = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const brainWindow = fs.readFileSync(path.join(root, "app", "BrainWindow.cs"), "utf8");
const explorerWindow = fs.readFileSync(path.join(root, "app", "FileExplorerWindow.cs"), "utf8");
const brainHtml = fs.readFileSync(path.join(root, "dashboard", "brain.html"), "utf8");
const explorerHtml = fs.readFileSync(path.join(root, "dashboard", "explorer.html"), "utf8");

let failures = 0;

// Brain window markers
const brainMarkers = [
  ["BrainWindow", brainWindow, "BrainWindow.cs"],
  ["SetVirtualHostNameToFolderMapping", brainWindow, "BrainWindow.cs uses virtual host"],
  ["jarvis.local", brainWindow, "BrainWindow.cs uses jarvis.local mapping"],
  ["brain.html", brainWindow, "BrainWindow.cs navigates to brain.html"],
  ["ForceClose", brainWindow, "BrainWindow.cs has ForceClose"],
  ["ShowOrFocus", brainWindow, "BrainWindow.cs has ShowOrFocus"]
];
for (const [marker, src, label] of brainMarkers) {
  if (!src.includes(marker)) {
    failures += 1;
    console.log(`FAIL ${label}: missing ${marker}`);
  } else {
    console.log(`PASS ${label}`);
  }
}

// File Explorer window stub
const explorerMarkers = ["FileExplorerWindow", "ShowOrFocus", "ForceClose"];
for (const m of explorerMarkers) {
  if (!explorerWindow.includes(m)) {
    failures += 1;
    console.log(`FAIL FileExplorerWindow.cs missing: ${m}`);
  } else {
    console.log(`PASS FileExplorerWindow.cs has: ${m}`);
  }
}

// Program.cs wiring
const programMarkers = [
  "OpenBrainWindow",
  "OpenFileExplorerWindow",
  "_brainWindow",
  "_fileExplorerWindow",
  "OnMainFormClosing",
  "CommandIntent.BrainWindowOpen",
  "CommandIntent.FileExplorerOpen"
];
for (const m of programMarkers) {
  if (!csharp.includes(m)) {
    failures += 1;
    console.log(`FAIL Program.cs missing: ${m}`);
  } else {
    console.log(`PASS Program.cs has: ${m}`);
  }
}

// Router wiring
const routerMarkers = [
  "BrainWindowOpen",
  "FileExplorerOpen",
  '"brain"',
  '"explorer"'
];
for (const m of routerMarkers) {
  if (!router.includes(m)) {
    failures += 1;
    console.log(`FAIL CommandRouterV1.cs missing: ${m}`);
  } else {
    console.log(`PASS CommandRouterV1.cs has: ${m}`);
  }
}

// brain.html: must use importmap to local vendor + have fallback panel.
const brainHtmlMarkers = [
  "importmap",
  "https://jarvis.local/vendor/three.module.js",
  'id="fallback"',
  'id="brain-canvas"',
  'id="region-list"'
];
for (const m of brainHtmlMarkers) {
  if (!brainHtml.includes(m)) {
    failures += 1;
    console.log(`FAIL brain.html missing: ${m}`);
  } else {
    console.log(`PASS brain.html has: ${m}`);
  }
}

// brain.html must NOT use CDN URLs (offline-first).
const cdnPatterns = ["cdn.jsdelivr.net", "unpkg.com", "skypack.dev", "esm.sh"];
for (const p of cdnPatterns) {
  if (brainHtml.includes(p)) {
    failures += 1;
    console.log(`FAIL brain.html must not use CDN: ${p}`);
  }
}
console.log("PASS brain.html avoids CDN dependencies");

// explorer.html stub minimum
if (!explorerHtml.includes("Jarvis File Explorer")) {
  failures += 1;
  console.log("FAIL explorer.html missing title");
} else {
  console.log("PASS explorer.html has title");
}

// Help text mentions new slash commands.
if (!csharp.includes("/brain") || !csharp.includes("/explorer")) {
  failures += 1;
  console.log("FAIL Help text missing /brain or /explorer");
} else {
  console.log("PASS Help text mentions /brain and /explorer");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll brain-window checks passed.");
}
