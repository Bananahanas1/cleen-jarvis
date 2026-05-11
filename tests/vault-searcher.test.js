// Verifierar VaultSearcher (BR2/BR6) — finns med rätt API och hookas in i AskOllamaAsync.
const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const searcher = fs.readFileSync(path.join(root, "app", "Brain", "VaultSearcher.cs"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const validator = fs.readFileSync(path.join(root, "app", "CommandValidatorV1.cs"), "utf8");
const html = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");

let failures = 0;

function blockBetween(text, startMarker, endMarker) {
  const start = text.indexOf(startMarker);
  const end = start < 0 ? -1 : text.indexOf(endMarker, start + startMarker.length);
  if (start < 0) return "";
  if (end < 0 || end <= start) return text.slice(start);
  return text.slice(start, end);
}

const searcherMarkers = [
  "VaultRoot",
  "AutoContextEnabled",
  "EnsureIndexFresh",
  "TopMatches",
  "BuildContextPrefix",
  "InvalidateIndex",
  "MaxContextChars",
  "TokenizeAndCount",
  "StopWords"
];
for (const m of searcherMarkers) {
  if (!searcher.includes(m)) { failures++; console.log("FAIL VaultSearcher missing: " + m); }
  else console.log("PASS VaultSearcher has: " + m);
}

// Default ON enligt användarens val
if (!searcher.includes("AutoContextEnabled { get; set; } = true")) {
  failures++; console.log("FAIL AutoContextEnabled måste vara default true");
} else console.log("PASS AutoContextEnabled default true");

// Vault scope: bara F:\Jarvis-clean\vault\
if (!searcher.includes('VaultRoot = @"F:\\Jarvis-clean\\vault"')) {
  failures++; console.log("FAIL VaultRoot ska vara F:\\Jarvis-clean\\vault");
} else console.log("PASS VaultRoot är clean-vault");

// Hård context-cap
if (!searcher.includes("MaxContextChars = 4000")) {
  failures++; console.log("FAIL MaxContextChars ska vara 4000");
} else console.log("PASS MaxContextChars = 4000");

// AskOllamaAsync ska kalla BuildContextPrefix
if (!program.includes("VaultSearcher.BuildContextPrefix(text")) {
  failures++; console.log("FAIL AskOllamaAsync måste injicera vault-kontext");
} else console.log("PASS AskOllamaAsync använder VaultSearcher");

// Slash-routing
const routerMarkers = ["VaultSearch", "VaultCreate", "VaultToggle", "VaultStatus", "ParseVaultSlashCommand"];
for (const m of routerMarkers) {
  if (!router.includes(m)) { failures++; console.log("FAIL Router missing: " + m); }
  else console.log("PASS Router has: " + m);
}

// Tool-funktioner
const toolMarkers = ["VaultStatusTool", "VaultToggleTool", "VaultSearchTool", "VaultCreateTool", "SanitizeVaultNoteNameV1"];
for (const m of toolMarkers) {
  if (!program.includes(m)) { failures++; console.log("FAIL Program missing tool: " + m); }
  else console.log("PASS Program has tool: " + m);
}

const vaultCreateBlock = blockBetween(
  program,
  "private static string VaultCreateTool(",
  "private static string SanitizeVaultNoteNameV1("
);

if (!vaultCreateBlock.includes("CreateProjectFileRequestTool(relativePath, note)")) {
  failures++; console.log("FAIL VaultCreateTool must create pending file-create preview");
} else console.log("PASS VaultCreateTool uses pending file-create preview");

for (const marker of ["File.WriteAllText", "File.AppendAllText", "Directory.CreateDirectory", "VaultSearcher.InvalidateIndex()"]) {
  if (vaultCreateBlock.includes(marker)) {
    failures++; console.log("FAIL VaultCreateTool must not mutate disk/index directly: " + marker);
  } else console.log("PASS VaultCreateTool avoids direct mutation: " + marker);
}

if (!validator.includes("case CommandIntent.VaultCreate") ||
    !validator.includes("Vault-skapande måste kräva pending/godkännande.")) {
  failures++; console.log("FAIL CommandValidatorV1 must require approval for VaultCreate");
} else console.log("PASS CommandValidatorV1 validates VaultCreate approval");

if (!program.includes("InvalidateVaultIndexIfTargetIsVaultPathV1") ||
    !program.includes("InvalidateVaultIndexIfTargetIsVaultPathV1(pending.Target)")) {
  failures++; console.log("FAIL approved vault file writes must invalidate the vault index");
} else console.log("PASS approved vault file writes invalidate vault index");

// Översikt-cell + dashboard-rendering
if (!html.includes('id="visualVaultState"')) {
  failures++; console.log("FAIL Översikt saknar Vault-cell");
} else console.log("PASS Översikt har Vault-cell");
if (!program.includes('["vault"] = BuildVaultOverviewLineV1')) {
  failures++; console.log("FAIL Översikt-payload saknar vault-key");
} else console.log("PASS Översikt-payload har vault-key");

// Hjälptext
if (!program.includes("/vault status")) {
  failures++; console.log("FAIL Hjälptext saknar /vault");
} else console.log("PASS Hjälptext mentions /vault");

if (failures > 0) { console.log("\n" + failures + " failure(s)"); process.exit(1); }
else console.log("\nAll vault-searcher checks passed.");
