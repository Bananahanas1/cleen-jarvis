const fs = require("fs");
const path = require("path");
const vm = require("vm");

const htmlPath = path.join(__dirname, "..", "dashboard", "index.html");
const html = fs.readFileSync(htmlPath, "utf8");
const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>/);

if (!scriptMatch) {
  throw new Error("Could not find dashboard script block.");
}

const csharpPath = path.join(__dirname, "..", "app", "Program.cs");
const csharp = fs.readFileSync(csharpPath, "utf8");

let failures = 0;

const htmlMarkers = [
  'id="visualBuildStatusState"',
  'id="visualMemoryChangeState"',
  '>Senaste bygge<',
  '>Senaste minnesförändring<'
];

for (const marker of htmlMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL dashboard marker missing: ${marker}`);
  } else {
    console.log(`PASS dashboard marker present: ${marker}`);
  }
}

const csharpMarkers = [
  "BuildStatusV1",
  "MemoryChangeV1",
  "LatestBuildStatusV1",
  "LatestMemoryChangeV1",
  "RecordMemoryChangeV1",
  "IsDotnetBuildLikeCommandV1",
  "BuildBuildStatusOverviewLineV1",
  "BuildMemoryChangeOverviewLineV1",
  "[\"buildStatus\"]",
  "[\"memoryChange\"]"
];

for (const marker of csharpMarkers) {
  if (!csharp.includes(marker)) {
    failures += 1;
    console.log(`FAIL C# marker missing: ${marker}`);
  } else {
    console.log(`PASS C# marker present: ${marker}`);
  }
}

// Verify dotnet command detection — IsDotnetBuildLikeCommandV1 should accept these and reject non-dotnet.
// We can't run C# from node, but we verify the regex/string predicates match by inspecting the function body.
const fnMatch = csharp.match(/IsDotnetBuildLikeCommandV1\([^)]*\)\s*\{([\s\S]*?)\n    \}/);
if (!fnMatch) {
  failures += 1;
  console.log("FAIL IsDotnetBuildLikeCommandV1 body not parseable");
} else {
  const body = fnMatch[1];
  const expected = ["dotnet build", "dotnet publish", "dotnet test", "dotnet run"];
  for (const cmd of expected) {
    if (!body.includes(cmd)) {
      failures += 1;
      console.log(`FAIL IsDotnetBuildLikeCommandV1 should match '${cmd}'`);
    } else {
      console.log(`PASS IsDotnetBuildLikeCommandV1 matches '${cmd}'`);
    }
  }
}

// Verify RecordMemoryChangeV1 is called from the two real memory-write paths (smart memory + forget/archive).
// It should NOT be called from the create-empty-file branches (those are not user-facing changes).
const recordCalls = (csharp.match(/RecordMemoryChangeV1\(/g) || []).length;
if (recordCalls < 2) {
  failures += 1;
  console.log(`FAIL RecordMemoryChangeV1 should be called from at least 2 memory-write paths, got ${recordCalls}`);
} else {
  console.log(`PASS RecordMemoryChangeV1 wired into ${recordCalls} memory-write paths`);
}

// Verify dashboard rendering reads buildStatus and memoryChange from overview payload.
function makeElement() {
  const listeners = {};
  const el = {
    value: "", textContent: "", innerHTML: "", scrollTop: 0, scrollHeight: 0,
    readOnly: true, disabled: false, style: {}, dataset: {}, className: "",
    title: "", children: [], nextElementSibling: null,
    appendChild(child) { this.children.push(child); return child; },
    addEventListener(event, handler) {
      listeners[event] = listeners[event] || [];
      listeners[event].push(handler);
    },
    click() { for (const h of (listeners.click || [])) h({preventDefault(){}}); },
    querySelector() { return null; },
    querySelectorAll() { return []; },
    insertAdjacentElement() {}, remove() {}, focus() {},
    setSelectionRange() {}, scrollIntoView() {}
  };
  el.classList = {
    _set: new Set(),
    contains(c) { return this._set.has(c); },
    add(c) { this._set.add(c); el.className = Array.from(this._set).join(" "); },
    remove(c) { this._set.delete(c); el.className = Array.from(this._set).join(" "); }
  };
  return el;
}

const elements = {};
function getElement(id) {
  elements[id] = elements[id] || makeElement();
  return elements[id];
}

const context = {
  console,
  setTimeout() {},
  clearTimeout() {},
  navigator: { clipboard: { async writeText() {} } },
  CSS: { escape(v) { return String(v || ""); } },
  document: { getElementById: getElement, createElement() { return makeElement(); } },
  chrome: { webview: { postMessage() {} } }
};
context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });

if (typeof context.jarvisSetJarvisOverviewV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisSetJarvisOverviewV1 missing");
} else {
  context.jarvisSetJarvisOverviewV1({
    memory: "Lokalt minne: 5 minnen",
    obsidian: "Obsidian: hittad",
    buildStatus: "OK (exit 0)\ndotnet build\n2026-05-09 14:00:00",
    memoryChange: "sparat (smart_memory) 2026-05-09 14:01:00\nfavoritfärg är blå"
  });
  context.jarvisShowVisualPanelV1();

  const buildTxt = String(elements.visualBuildStatusState.textContent || "");
  if (!buildTxt.includes("OK") || !buildTxt.includes("dotnet build")) {
    failures += 1;
    console.log(`FAIL visualBuildStatusState should render build payload, got ${JSON.stringify(buildTxt)}`);
  } else {
    console.log("PASS visualBuildStatusState renders build status from payload");
  }

  const memTxt = String(elements.visualMemoryChangeState.textContent || "");
  if (!memTxt.includes("sparat") || !memTxt.includes("favoritfärg")) {
    failures += 1;
    console.log(`FAIL visualMemoryChangeState should render memory change, got ${JSON.stringify(memTxt)}`);
  } else {
    console.log("PASS visualMemoryChangeState renders memory change from payload");
  }
}

if (failures > 0) {
  console.log(`\n${failures} failure(s)`);
  process.exit(1);
} else {
  console.log("\nAll overview build+memory checks passed.");
}
