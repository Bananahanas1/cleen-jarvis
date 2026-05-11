// Verifierar File Explorer-fönster (Fas 4): multi-root, read-only, säker write-path.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const explorerCs = fs.readFileSync(path.join(root, "app", "FileExplorerWindow.cs"), "utf8");
const explorerHtml = fs.readFileSync(path.join(root, "dashboard", "explorer.html"), "utf8");

let failures = 0;

// Multi-root
if (!explorerCs.includes(`["clean"]      = (@"F:\\Jarvis-clean", false)`)) {
  failures += 1;
  console.log("FAIL Roots dictionary missing clean (rw)");
} else {
  console.log("PASS Roots dictionary has clean (rw)");
}
if (!explorerCs.includes(`["newproject"] = (@"F:\\New project",  true)`)) {
  failures += 1;
  console.log("FAIL Roots dictionary missing newproject (read-only)");
} else {
  console.log("PASS Roots dictionary has newproject (read-only)");
}

// Safe path: must use ResolveSafeFullPath to prevent traversal.
if (!explorerCs.includes("ResolveSafeFullPath") || !explorerCs.includes("StartsWith(fullRoot")) {
  failures += 1;
  console.log("FAIL ResolveSafeFullPath path-traversal guard missing");
} else {
  console.log("PASS Path-traversal guard present");
}

// Allowed extensions list
const allowedMarkers = ['".md"', '".cs"', '".html"', '".json"', '".csproj"'];
for (const m of allowedMarkers) {
  if (!explorerCs.includes(m)) {
    failures += 1;
    console.log(`FAIL AllowedExtensions missing ${m}`);
  } else {
    console.log(`PASS AllowedExtensions has ${m}`);
  }
}

// Excluded dirs
const excludedMarkers = ['"bin"', '"obj"', '"node_modules"', '".git"', '".checkpoints"'];
for (const m of excludedMarkers) {
  if (!explorerCs.includes(m)) {
    failures += 1;
    console.log(`FAIL ExcludedDirs missing ${m}`);
  } else {
    console.log(`PASS ExcludedDirs has ${m}`);
  }
}

// Max file size guard
if (!explorerCs.includes("MaxFileBytes")) {
  failures += 1;
  console.log("FAIL MaxFileBytes guard missing");
} else {
  console.log("PASS MaxFileBytes guard present");
}

// Read-only enforcement
if (!explorerCs.includes("rootDef.Readonly") || !explorerCs.includes("Skrivning blockerad")) {
  failures += 1;
  console.log("FAIL Read-only enforcement missing");
} else {
  console.log("PASS Read-only roots enforced");
}

// HTML markers
const htmlMarkers = [
  'data-root="clean"',
  'data-root="newproject"',
  'fileexplorer_list_tree',
  'fileexplorer_open_file',
  'fileexplorer_save_pending',
  'window.fileExplorerSetTree',
  'window.fileExplorerSetFile',
  'window.fileExplorerSaveResult',
  'multi-root'
];
for (const m of htmlMarkers) {
  if (!explorerHtml.includes(m)) {
    failures += 1;
    console.log(`FAIL explorer.html missing: ${m}`);
  } else {
    console.log(`PASS explorer.html has: ${m}`);
  }
}

// HTML must NOT use CDN
const cdnPatterns = ["cdn.jsdelivr.net", "unpkg.com", "skypack.dev"];
for (const p of cdnPatterns) {
  if (explorerHtml.includes(p)) {
    failures += 1;
    console.log(`FAIL explorer.html must not use CDN: ${p}`);
  }
}
console.log("PASS explorer.html avoids CDN");

// Edit-mode requires explicit toggle
if (!explorerHtml.includes("editor.readOnly = true; // Edit-läge måste aktiveras explicit")) {
  failures += 1;
  console.log("FAIL Editor must default to read-only until Edit-läge clicked");
} else {
  console.log("PASS Editor defaults to read-only");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll file-explorer-window checks passed.");
}
