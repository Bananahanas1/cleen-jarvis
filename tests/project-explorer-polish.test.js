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
  ".tree-row.active-file",
  ".tree-row.active-folder",
  "window.jarvisSetActiveTreeFileV1",
  "let activeTreePathV1",
  'data-folder-path',
  'data-file-path'
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL project-explorer polish marker missing: ${marker}`);
  } else {
    console.log(`PASS project-explorer polish marker present: ${marker}`);
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
    appendChild(child) {
      this.children.push(child);
      return child;
    },
    addEventListener(event, handler) {
      listeners[event] = listeners[event] || [];
      listeners[event].push(handler);
    },
    click() {
      for (const handler of listeners.click || []) {
        handler({ preventDefault() {} });
      }
    },
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
    add(c) {
      this._set.add(c);
      el.className = Array.from(this._set).join(" ");
    },
    remove(c) {
      this._set.delete(c);
      el.className = Array.from(this._set).join(" ");
    }
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
  document: {
    getElementById: getElement,
    createElement() { return makeElement(); }
  },
  chrome: { webview: { postMessage() {} } }
};
context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });

if (typeof context.jarvisSetActiveTreeFileV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisSetActiveTreeFileV1 must be defined");
} else {
  console.log("PASS jarvisSetActiveTreeFileV1 is defined");
}

if (typeof context.jarvisSetEditorFile !== "function") {
  failures += 1;
  console.log("FAIL jarvisSetEditorFile must be defined");
} else {
  let received = null;
  const original = context.jarvisSetActiveTreeFileV1;
  context.jarvisSetActiveTreeFileV1 = function(p) {
    received = p;
    if (typeof original === "function") {
      try { original(p); } catch (e) {}
    }
  };

  context.jarvisSetEditorFile("docs/test-active.md", "hej", true, false);

  if (received !== "docs/test-active.md") {
    failures += 1;
    console.log(`FAIL jarvisSetEditorFile must propagate active tree path, got ${JSON.stringify(received)}`);
  } else {
    console.log("PASS jarvisSetEditorFile propagates active tree path to jarvisSetActiveTreeFileV1");
  }

  context.jarvisSetActiveTreeFileV1 = original;
}

if (typeof context.jarvisSetTreeFolderV7 === "function" && typeof context.jarvisSetActiveTreeFileV1 === "function") {
  let reRenderReceived = null;
  const original = context.jarvisSetActiveTreeFileV1;
  context.jarvisSetActiveTreeFileV1 = function(p) {
    reRenderReceived = p;
    if (typeof original === "function") {
      try { original(p); } catch (e) {}
    }
  };

  context.jarvisSetEditorFile("docs/persistent-active.md", "x", true, false);
  reRenderReceived = null;
  context.jarvisSetTreeFolderV7("", [
    { type: "folder", name: "docs", path: "docs" },
    { type: "file", name: "README.md", path: "README.md" }
  ]);

  if (reRenderReceived !== "docs/persistent-active.md") {
    failures += 1;
    console.log(`FAIL jarvisSetTreeFolderV7 must re-apply active tree path, got ${JSON.stringify(reRenderReceived)}`);
  } else {
    console.log("PASS jarvisSetTreeFolderV7 re-applies active tree path after re-render");
  }

  context.jarvisSetActiveTreeFileV1 = original;
}

if (failures > 0) {
  process.exit(1);
}
