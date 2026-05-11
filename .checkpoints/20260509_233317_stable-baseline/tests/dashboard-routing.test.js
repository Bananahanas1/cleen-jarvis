const fs = require("fs");
const path = require("path");
const vm = require("vm");

const htmlPath = path.join(__dirname, "..", "dashboard", "index.html");
const html = fs.readFileSync(htmlPath, "utf8");
const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>/);

if (!scriptMatch) {
  throw new Error("Could not find dashboard script block.");
}

function makeElement() {
  return {
    value: "",
    textContent: "",
    innerHTML: "",
    scrollTop: 0,
    scrollHeight: 0,
    readOnly: true,
    style: {},
    dataset: {},
    className: "",
    title: "",
    nextElementSibling: null,
    appendChild() {},
    addEventListener() {},
    querySelector() { return null; },
    insertAdjacentElement() {},
    remove() {},
    focus() {},
    setSelectionRange() {},
    classList: {
      contains() { return false; },
      add() {},
      remove() {}
    }
  };
}

const context = {
  console,
  setTimeout() {},
  navigator: {
    clipboard: {
      async writeText() {}
    }
  },
  CSS: {
    escape(value) {
      return String(value || "");
    }
  },
  document: {
    getElementById() {
      return makeElement();
    },
    createElement() {
      return makeElement();
    }
  }
};

context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });

const tests = [
  ["slash memory command is not intercepted by smart file-open", "/minne viktiga", ""],
  ["slash status command is not intercepted by smart file-open", "/status", ""],
  ["slash file write command is not intercepted by smart file-open", "/fil skriv docs/test-agent.md | text", ""],
  ["slash file append command is not intercepted by smart file-open", "/fil lagg-till docs/test-agent.md | text", ""],
  ["slash file create command is not intercepted by smart file-open", "/fil skapa docs/test-new.md | text", ""],
  ["slash terminal root is not intercepted by smart file-open", "/terminal", ""],
  ["slash terminal show is not intercepted by smart file-open", "/terminal visa", ""],
  ["slash terminal command is not intercepted by smart file-open", "/terminal preview dotnet build", ""],
  ["slash terminal approve is not intercepted by smart file-open", "/terminal godkann", ""],
  ["slash terminal cancel is not intercepted by smart file-open", "/terminal avbryt", ""],
  ["file append ascii command is not intercepted by smart file-open", "lagg till fil: docs/test-agent.md | TESTAR PENDING APPROVAL", ""],
  ["append file command is not intercepted by smart file-open", "append fil: docs/test-agent.md | TESTAR PENDING APPROVAL", ""],
  ["propose change ascii command is not intercepted by smart file-open", "foresla andring: docs/test-agent.md | TESTAR", ""],
  ["propose heading ascii command is not intercepted by smart file-open", "foresla rubrik: docs/test-agent.md | TESTAR", ""],
  ["approve file write ascii command is not intercepted by smart file-open", "godkann filskrivning", ""],
  ["cancel file write command is not intercepted by smart file-open", "avbryt filskrivning", ""],
  ["terminal preview command is not intercepted by smart file-open", "terminal preview: dotnet build", ""],
  ["terminal confirm ascii command is not intercepted by smart file-open", "bekrafta kor", ""],
  ["generic cancel is not intercepted by smart file-open", "avbryt", ""],
  ["generic cancel all is not intercepted by smart file-open", "avbryt allt", ""],
  ["generic stop is not intercepted by smart file-open", "stoppa", ""],
  ["terminal show phrase is not file-open", "visa terminal", ""],
  ["terminal question is not file-open", "vad stod i terminalen", ""],
  ["latest terminal phrase is not file-open", "senaste terminal", ""],
  ["terminal output phrase is not file-open", "terminal output", ""],
  ["terminal panel phrase is not file-open", "terminalpanelen", ""],
  ["show terminal panel phrase is not file-open", "visa terminalpanelen", ""],
  ["open terminal phrase is not file-open", "oppna terminal", ""],
  ["terminal alone is not file-open", "terminal", ""],
  ["overview phrase is not file-open", "visa oversikt", ""],
  ["obsidian status phrase is not file-open", "obsidian status", ""],
  ["slash obsidian status is not file-open", "/obsidian status", ""],
  ["slash overview is not file-open", "/oversikt", ""],
  ["delete file command is not intercepted by smart file-open", "radera fil: docs/test-agent.md", ""],
  ["remove file command is not intercepted by smart file-open", "ta bort fil: docs/test-agent.md", ""],
  ["memory display phrase is not file-open", "visa viktiga minnen", ""],
  ["natural important memory phrase is not file-open", "visa mina viktiga minnen", ""],
  ["slash file command is not intercepted by smart file-open", "/fil öppna README.md", ""],
  ["file write command is not intercepted by smart file-open", "skriv fil: docs/test-agent.md | TESTAR PENDING APPROVAL", ""],
  ["file create command is not intercepted by smart file-open", "skapa fil: docs/test-new.md | TESTAR PENDING APPROVAL", ""],
  ["file append command is not intercepted by smart file-open", "lägg till fil: docs/test-agent.md | TESTAR PENDING APPROVAL", ""],
  ["explicit file open still works", "öppna README.md", "README.md"],
  ["explicit nested file open still works", "oppna tests/terminal-approval-safety.test.js", "tests/terminal-approval-safety.test.js"],
  ["explicit file display still works", "visa docs/PROJECT_INDEX.md", "docs/PROJECT_INDEX.md"]
];

let failures = 0;

for (const [name, input, expected] of tests) {
  const actual = context.extractSmartFileOpen(input);

  if (actual !== expected) {
    failures += 1;
    console.log(`FAIL ${name}`);
    console.log(`     expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`);
  } else {
    console.log(`PASS ${name}`);
  }
}

if (failures > 0) {
  process.exit(1);
}

context.jarvisSetAllFilesV7(
  ["README.md", "docs/PROJECT_INDEX.md", "app/Program.cs"],
  ["app", "dashboard", "docs"]
);

const autocompleteTests = [
  [
    "slash root suggestions include help",
    () => context.commandBaseSuggestions("/"),
    "/hjälp"
  ],
  [
    "slash memory suggestions include search",
    () => context.commandBaseSuggestions("/minne"),
    "/minne sök "
  ],
  [
    "slash file open suggestions include matching files",
    () => context.fileSuggestions("/fil öppna r"),
    "/fil öppna README.md"
  ],
  [
    "slash file create suggestion exists",
    () => context.commandBaseSuggestions("/fil"),
    "/fil skapa "
  ],
  [
    "slash obsidian status suggestion exists",
    () => context.commandBaseSuggestions("/obsidian"),
    "/obsidian status"
  ],
  [
    "slash overview suggestion exists",
    () => context.commandBaseSuggestions("/over"),
    "/oversikt"
  ]
];

for (const [name, getSuggestions, expected] of autocompleteTests) {
  const suggestions = getSuggestions();

  if (!suggestions.includes(expected)) {
    failures += 1;
    console.log(`FAIL ${name}`);
    console.log(`     expected ${JSON.stringify(expected)} in ${JSON.stringify(suggestions)}`);
  } else {
    console.log(`PASS ${name}`);
  }
}

const terminalSuggestions = context.commandBaseSuggestions("/terminal");
if (!terminalSuggestions.includes("/terminal preview ")) {
  failures += 1;
  console.log("FAIL slash terminal suggestions include preview");
  console.log(`     expected "/terminal preview " in ${JSON.stringify(terminalSuggestions)}`);
} else {
  console.log("PASS slash terminal suggestions include preview");
}

const noPendingCancelSuggestions = context.commandBaseSuggestions("avbryt");
if (noPendingCancelSuggestions.includes("avbryt kör")) {
  failures += 1;
  console.log("FAIL autocomplete should not suggest terminal cancel when no terminal pending exists");
  console.log(`     suggestions: ${JSON.stringify(noPendingCancelSuggestions)}`);
} else {
  console.log("PASS autocomplete hides terminal cancel with no pending terminal");
}

if (typeof context.jarvisShowPendingApprovalV1 === "function") {
  context.jarvisShowPendingApprovalV1({
    type: "TerminalRun",
    title: "Godkänn terminalkörning",
    description: "Test",
    meta: "",
    preview: "dotnet build"
  });
}

const pendingTerminalCancelSuggestions = context.commandBaseSuggestions("avbryt");
if (!pendingTerminalCancelSuggestions.includes("avbryt kör")) {
  failures += 1;
  console.log("FAIL autocomplete should suggest terminal cancel when terminal pending exists");
  console.log(`     suggestions: ${JSON.stringify(pendingTerminalCancelSuggestions)}`);
} else {
  console.log("PASS autocomplete suggests terminal cancel with pending terminal");
}

if (failures > 0) {
  process.exit(1);
}
