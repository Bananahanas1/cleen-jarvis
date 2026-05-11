// Verifierar NeuroLinkedBridge-livscykeln (Fas 5).
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const bridge = fs.readFileSync(path.join(root, "app", "Bridges", "NeuroLinkedBridge.cs"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const html = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;

const bridgeMarkers = [
  "ResolvePython",
  "JARVIS_PYTHON",
  '"py"',
  '"python"',
  '"python3"',
  "ServerHealthUrl",
  "127.0.0.1:8000",
  "WaitForReadyAsync",
  "StatusChip",
  "IsAliveAsync",
  "StopAsync",
  "Kill(entireProcessTree: true)",
  "BridgeState"
];
for (const m of bridgeMarkers) {
  if (!bridge.includes(m)) { failures += 1; console.log(`FAIL NeuroLinkedBridge.cs missing: ${m}`); }
  else console.log(`PASS NeuroLinkedBridge.cs has: ${m}`);
}

// Bridge must bind only to localhost.
if (!bridge.includes('"127.0.0.1"')) {
  failures += 1;
  console.log("FAIL Bridge must enforce 127.0.0.1 binding");
} else {
  console.log("PASS Bridge binds to 127.0.0.1 only");
}

// Program.cs wires up the bridge.
const programMarkers = [
  "_neuroLinkedBridge",
  "NeuroLinkedBridge _neuroLinkedBridge = new()",
  "StartAsync()",
  "StopAsync()",
  "SendBrainStatusChipAsync",
  "jarvisSetBrainStatusChip"
];
for (const m of programMarkers) {
  if (!program.includes(m)) { failures += 1; console.log(`FAIL Program.cs missing: ${m}`); }
  else console.log(`PASS Program.cs has: ${m}`);
}

// Dashboard chip
const htmlMarkers = [
  'id="visualBrainStatusState"',
  'window.jarvisSetBrainStatusChip',
  '"Brain (NeuroLinked)"'
];
for (const m of htmlMarkers) {
  if (m.startsWith('"')) {
    if (!html.includes(m.slice(1, -1))) { failures += 1; console.log(`FAIL dashboard missing label: ${m}`); }
    else console.log(`PASS dashboard label: ${m}`);
  } else {
    if (!html.includes(m)) { failures += 1; console.log(`FAIL dashboard missing: ${m}`); }
    else console.log(`PASS dashboard has: ${m}`);
  }
}

// Python sources copied
const pythonFiles = [
  "neurolinked/run.py",
  "neurolinked/server.py",
  "python/jarvis_local_tools.py",
  "python/graphify_tool.py",
  "python/obsidian_tool.py"
];
for (const rel of pythonFiles) {
  const abs = path.join(root, rel);
  if (!fs.existsSync(abs)) { failures += 1; console.log(`FAIL python source missing: ${rel}`); }
  else console.log(`PASS python source present: ${rel}`);
}

// Auto-start triggered from OnLoad navigationcompleted (always-on per plan).
if (!program.includes("await _neuroLinkedBridge.StartAsync();")) {
  failures += 1;
  console.log("FAIL Bridge must auto-start (always-on)");
} else {
  console.log("PASS Bridge auto-starts on app load");
}

// Auto-stop wired into OnMainFormClosing.
if (!program.includes("_neuroLinkedBridge.StopAsync()")) {
  failures += 1;
  console.log("FAIL Bridge must auto-stop on app close");
} else {
  console.log("PASS Bridge auto-stops on app close");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll neurolinked-bridge checks passed.");
}
