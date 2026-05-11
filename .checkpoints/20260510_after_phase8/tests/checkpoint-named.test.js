// Verifierar att Program.cs har de namngivna checkpoint-funktionerna och sanitize-regler.
// Faktiska C#-tester för slash-routing finns i tests/CommandRouterV1.Tests.

const fs = require("fs");
const path = require("path");

const csharp = fs.readFileSync(
  path.join(__dirname, "..", "app", "Program.cs"),
  "utf8"
);
const router = fs.readFileSync(
  path.join(__dirname, "..", "app", "CommandRouterV1.cs"),
  "utf8"
);

let failures = 0;

const csharpMarkers = [
  "CreateNamedCheckpointTool",
  "SanitizeCheckpointNameV1",
  "ResolveCheckpointByNameV1",
  "RestoreCheckpointByNameTool",
  "RestoreCheckpointFromPathV1",
  "ExtractCheckpointNameFromCommandV1",
  "CommandIntent.CheckpointCreate",
  "CommandIntent.CheckpointList",
  "CommandIntent.CheckpointRestore"
];

for (const marker of csharpMarkers) {
  if (!csharp.includes(marker)) {
    failures += 1;
    console.log(`FAIL Program.cs missing: ${marker}`);
  } else {
    console.log(`PASS Program.cs has: ${marker}`);
  }
}

const routerMarkers = [
  "ParseCheckpointSlashCommand",
  "CheckpointRestore",
  '"checkpoint skapa "',
  '"checkpoint aterstall "',
  '"checkpoint lista"'
];

for (const marker of routerMarkers) {
  if (!router.includes(marker)) {
    failures += 1;
    console.log(`FAIL CommandRouterV1.cs missing: ${marker}`);
  } else {
    console.log(`PASS CommandRouterV1.cs has: ${marker}`);
  }
}

// Sanitize: verify that the predicate logic in C# enforces safety.
// Searches the whole file but the markers are unique to the SanitizeCheckpointNameV1 function.
const sanitizePredicates = ["char.IsLetterOrDigit(ch)", "ch == '-'", "ch == '_'", "Substring(0, 40)"];
for (const m of sanitizePredicates) {
  if (!csharp.includes(m)) {
    failures += 1;
    console.log(`FAIL SanitizeCheckpointNameV1 missing safety predicate: ${m}`);
  } else {
    console.log(`PASS SanitizeCheckpointNameV1 enforces: ${m}`);
  }
}
// Negative: file must not explicitly accept path-traversal chars in checkpoint names.
for (const bad of ["ch == '/'", "ch == '\\\\'", "ch == ':'", "ch == '.'"]) {
  if (csharp.includes(bad)) {
    failures += 1;
    console.log(`FAIL SanitizeCheckpointNameV1 should NOT accept ${bad}`);
  }
}

// Verify natural-language routing accepts named checkpoints
if (!csharp.includes('CommandStartsWith(command, "skapa checkpoint", "ny checkpoint", "create checkpoint")')) {
  failures += 1;
  console.log("FAIL Natural-language named-create routing missing");
} else {
  console.log("PASS Natural-language named-create routing present");
}
if (!csharp.includes('CommandStartsWith(command, "aterstall checkpoint"')) {
  failures += 1;
  console.log("FAIL Natural-language named-restore routing missing");
} else {
  console.log("PASS Natural-language named-restore routing present");
}

// Help text should mention the new slash commands
if (!csharp.includes("/checkpoint skapa") || !csharp.includes("/checkpoint lista")) {
  failures += 1;
  console.log("FAIL Help text missing /checkpoint examples");
} else {
  console.log("PASS Help text includes /checkpoint examples");
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll named-checkpoint checks passed.");
}
