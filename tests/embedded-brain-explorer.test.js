// Verifierar att Brain är inbäddad vy i main, samt att Explorer-vyn är borttagen helt
// (Project Explorer i vänsterpanelen är "the" explorer per användarfeedback 2026-05-10).
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const html = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const fgb = fs.readFileSync(path.join(root, "app", "Brain", "FileGraphBuilder.cs"), "utf8");

let failures = 0;

// 1. Inga separata Form-klasser för Brain/Explorer + ingen Explorer-vy
const obsoleteFiles = [
  path.join(root, "app", "BrainWindow.cs"),
  path.join(root, "app", "FileExplorerWindow.cs"),
  path.join(root, "dashboard", "brain.html"),
  path.join(root, "dashboard", "explorer.html")
];
for (const f of obsoleteFiles) {
  if (fs.existsSync(f)) {
    failures += 1;
    console.log(`FAIL obsolete file should be removed: ${path.basename(f)}`);
  } else {
    console.log(`PASS obsolete removed: ${path.basename(f)}`);
  }
}

// 2. Program.cs får inte instansiera Window-klasser eller Explorer-routing
const obsoleteRefs = [
  "new BrainWindow()", "new FileExplorerWindow()",
  "OpenFileExplorerWindow", "FileExplorerOpen",
  "explorer_list_tree", "SendExplorerTreeAsync"
];
for (const r of obsoleteRefs) {
  if (program.includes(r)) {
    failures += 1;
    console.log(`FAIL Program.cs still references: ${r}`);
  } else {
    console.log(`PASS Program.cs cleaned: ${r}`);
  }
}

// 3. OpenBrainWindow ska kalla TriggerWorkspaceMode("brain")
if (!program.includes('TriggerWorkspaceMode("brain")')) {
  failures += 1;
  console.log("FAIL OpenBrainWindow must trigger embedded mode");
} else {
  console.log("PASS OpenBrainWindow triggers embedded mode");
}

// 4. Dashboarden har Brain-panel inbäddad (ej Explorer)
const htmlMarkers = [
  'id="brainPanel"',
  'id="brainCanvas"',
  'id="showBrainBtn"',
  'window.jarvisShowBrainPanelV1',
  'window.jarvisShowWorkspaceMode',
  'window.initBrainGraphV1',
  'window.jarvisSetBrainGraphV1',
  'body.brain-mode',
  'classList.toggle("brain-mode"'
];
for (const m of htmlMarkers) {
  if (!html.includes(m)) {
    failures += 1;
    console.log(`FAIL dashboard missing: ${m}`);
  } else {
    console.log(`PASS dashboard has: ${m}`);
  }
}

// 5. Importmap måste peka på lokal vendor (offline-säker)
if (!html.includes("https://jarvis.local/dashboard/vendor/three.module.js")) {
  failures += 1;
  console.log("FAIL importmap must point to local vendor");
} else {
  console.log("PASS importmap uses local vendor");
}
if (html.includes("cdn.jsdelivr") || html.includes("unpkg.com")) {
  failures += 1;
  console.log("FAIL dashboard must not use CDN");
}

// 6. Main-fönstret använder virtual host (inte NavigateToString)
if (!program.includes('Navigate("https://jarvis.local/dashboard/index.html")')) {
  failures += 1;
  console.log("FAIL main must navigate via virtual host");
} else {
  console.log("PASS main navigates via virtual host");
}

// 7. FileGraphBuilder är på plats
const fgbMarkers = ["BuildJsonForRoot", "ScanCsFile", "ScanJsFile", "ScanPyFile", "RuntimeOrGeneratedDirs", "MaxProjectNodes"];
for (const m of fgbMarkers) {
  if (!fgb.includes(m)) {
    failures += 1;
    console.log(`FAIL FileGraphBuilder.cs missing: ${m}`);
  } else {
    console.log(`PASS FileGraphBuilder.cs has: ${m}`);
  }
}

// 8. Brain message handler wired
if (!program.includes('"brain_request_graph"')) {
  failures += 1;
  console.log(`FAIL Program.cs missing handler: brain_request_graph`);
} else {
  console.log(`PASS Program.cs handles: brain_request_graph`);
}

// 9. Explorer-knapp ska vara borta från dashboarden
if (html.includes('id="showExplorerBtn"')) {
  failures += 1;
  console.log("FAIL Explorer-knapp får inte finnas kvar");
} else {
  console.log("PASS Explorer-knapp borttagen");
}

// 10. FileGraphBuilder ska scanna Obsidian-vault
const vaultMarkers = ["ScanVault", "BuildVaultWikiIndex", "WikiLinks", "vault-link", "vault-source"];
for (const m of vaultMarkers) {
  if (!fgb.includes(m)) {
    failures += 1;
    console.log(`FAIL FileGraphBuilder.cs missing vault feature: ${m}`);
  } else {
    console.log(`PASS FileGraphBuilder.cs has vault feature: ${m}`);
  }
}

// 11. ToLookup istället för ToDictionary för att undvika __init__-krasch
if (!fgb.includes("ToLookup(")) {
  failures += 1;
  console.log("FAIL FileGraphBuilder.cs must use ToLookup to avoid duplicate-key crash");
} else {
  console.log("PASS FileGraphBuilder.cs uses ToLookup (handles duplicate filenames)");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll embedded-brain-explorer checks passed.");
}
