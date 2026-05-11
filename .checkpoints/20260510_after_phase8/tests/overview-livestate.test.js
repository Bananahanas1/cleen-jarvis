const fs = require("fs");
const path = require("path");
const vm = require("vm");

const htmlPath = path.join(__dirname, "..", "dashboard", "index.html");
const html = fs.readFileSync(htmlPath, "utf8");
const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>/);

if (!scriptMatch) {
  throw new Error("Could not find dashboard script block.");
}

const requiredMarkers = [
  'id="visualActiveFolder"',
  'id="visualLatestChange"',
  "computeActiveFolderLabelV1"
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL overview live-state marker missing: ${marker}`);
  } else {
    console.log(`PASS overview live-state marker present: ${marker}`);
  }
}

function makeElement() {
  const listeners = {};
  const el = {
    value: "",
    textContent: "",
    innerHTML: "",
    scrollTop: 0,
    scrollHeight: 0,
    readOnly: true,
    disabled: false,
    style: {},
    dataset: {},
    className: "",
    title: "",
    children: [],
    nextElementSibling: null,
    appendChild(child) { this.children.push(child); return child; },
    addEventListener(event, handler) {
      listeners[event] = listeners[event] || [];
      listeners[event].push(handler);
    },
    click() { for (const h of (listeners.click || [])) h({preventDefault(){}}); },
    querySelector() { return null; },
    querySelectorAll() { return []; },
    insertAdjacentElement() {},
    remove() {},
    focus() {},
    setSelectionRange() {},
    scrollIntoView() {}
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

if (typeof context.computeActiveFolderLabelV1 !== "function") {
  failures += 1;
  console.log("FAIL computeActiveFolderLabelV1 must be defined");
} else {
  const cases = [
    ["docs/foo.md", "docs"],
    ["app/Program.cs", "app"],
    ["foo.md", "(projektrot)"],
    ["", "(projektrot)"],
    ["a/b/c/file.txt", "a/b/c"]
  ];
  for (const [input, expected] of cases) {
    const actual = context.computeActiveFolderLabelV1(input);
    if (actual !== expected) {
      failures += 1;
      console.log(`FAIL computeActiveFolderLabelV1(${JSON.stringify(input)}) expected ${JSON.stringify(expected)} got ${JSON.stringify(actual)}`);
    } else {
      console.log(`PASS computeActiveFolderLabelV1(${JSON.stringify(input)}) -> ${JSON.stringify(actual)}`);
    }
  }
}

if (typeof context.jarvisShowVisualPanelV1 !== "function" || typeof context.jarvisSetEditorFile !== "function") {
  failures += 1;
  console.log("FAIL jarvisShowVisualPanelV1 or jarvisSetEditorFile missing");
} else {
  context.jarvisSetEditorFile("docs/test-active.md", "hej", true, false);
  context.jarvisShowVisualPanelV1();

  if (!String(elements.visualActiveFolder.textContent || "").includes("docs")) {
    failures += 1;
    console.log(`FAIL visualActiveFolder should mention 'docs', got ${JSON.stringify(elements.visualActiveFolder.textContent)}`);
  } else {
    console.log("PASS visualActiveFolder shows parent folder for active file");
  }
}

if (typeof context.jarvisShowFileChangeReviewV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisShowFileChangeReviewV1 missing");
} else {
  context.jarvisShowFileChangeReviewV1({
    path: "docs/test-create.md",
    changeKind: "create",
    added: 1,
    removed: 0,
    lines: []
  });
  context.jarvisShowVisualPanelV1();

  const txt = String(elements.visualLatestChange.textContent || "");
  if (!txt.includes("docs/test-create.md") || !txt.toLowerCase().includes("skapad")) {
    failures += 1;
    console.log(`FAIL visualLatestChange should describe latest create change, got ${JSON.stringify(txt)}`);
  } else {
    console.log("PASS visualLatestChange shows latest file change kind and path");
  }
}

if (failures > 0) {
  process.exit(1);
}
