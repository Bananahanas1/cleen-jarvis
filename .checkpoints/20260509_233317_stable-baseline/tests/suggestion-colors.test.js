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
  ".suggestion-command",
  ".suggestion-folder",
  ".suggestion-file",
  "colorizeSuggestionText"
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL suggestion color marker missing: ${marker}`);
  } else {
    console.log(`PASS suggestion color marker present: ${marker}`);
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
  setTimeout(fn) { if (typeof fn === "function") fn(); },
  clearTimeout() {},
  navigator: { clipboard: { async writeText() {} } },
  CSS: { escape(v) { return String(v || ""); } },
  document: { getElementById: getElement, createElement() { return makeElement(); } },
  chrome: { webview: { postMessage() {} } }
};
context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });

if (typeof context.colorizeSuggestionText !== "function") {
  failures += 1;
  console.log("FAIL colorizeSuggestionText must be defined");
} else {
  function findKind(parts, kind) {
    return parts.find(p => p.kind === kind);
  }

  const cases = [
    {
      input: "/fil skapa docs/",
      command: "/fil skapa ",
      folderText: "docs/",
      expectFile: false
    },
    {
      input: "/fil öppna README.md",
      command: "/fil öppna ",
      fileText: "README.md",
      expectFolder: false
    },
    {
      input: "skriv fil: docs/foo.md",
      command: "skriv fil: ",
      fileText: "docs/foo.md",
      expectFolder: false
    },
    {
      input: "skapa fil: app/",
      command: "skapa fil: ",
      folderText: "app/"
    },
    {
      input: "öppna app/Program.cs",
      command: "öppna ",
      fileText: "app/Program.cs"
    },
    {
      input: "/hjälp",
      command: "/hjälp",
      expectFile: false,
      expectFolder: false
    },
    {
      input: "/minne viktiga",
      command: "/minne viktiga",
      expectFile: false,
      expectFolder: false
    }
  ];

  for (const c of cases) {
    const parts = context.colorizeSuggestionText(c.input);
    const cmd = findKind(parts, "command");
    const folder = findKind(parts, "folder");
    const file = findKind(parts, "file");

    if (!cmd || cmd.text !== c.command) {
      failures += 1;
      console.log(`FAIL colorize ${JSON.stringify(c.input)} command expected ${JSON.stringify(c.command)}, got ${JSON.stringify(cmd && cmd.text)}`);
    } else {
      console.log(`PASS colorize ${JSON.stringify(c.input)} command=${JSON.stringify(cmd.text)}`);
    }

    if (c.folderText !== undefined) {
      if (!folder || folder.text !== c.folderText) {
        failures += 1;
        console.log(`FAIL colorize ${JSON.stringify(c.input)} folder expected ${JSON.stringify(c.folderText)}, got ${JSON.stringify(folder && folder.text)}`);
      } else {
        console.log(`PASS colorize ${JSON.stringify(c.input)} folder=${JSON.stringify(folder.text)}`);
      }
    }

    if (c.fileText !== undefined) {
      if (!file || file.text !== c.fileText) {
        failures += 1;
        console.log(`FAIL colorize ${JSON.stringify(c.input)} file expected ${JSON.stringify(c.fileText)}, got ${JSON.stringify(file && file.text)}`);
      } else {
        console.log(`PASS colorize ${JSON.stringify(c.input)} file=${JSON.stringify(file.text)}`);
      }
    }

    if (c.expectFile === false && file) {
      failures += 1;
      console.log(`FAIL colorize ${JSON.stringify(c.input)} should not have file part, got ${JSON.stringify(file)}`);
    }
    if (c.expectFolder === false && folder) {
      failures += 1;
      console.log(`FAIL colorize ${JSON.stringify(c.input)} should not have folder part, got ${JSON.stringify(folder)}`);
    }
  }
}

if (failures > 0) {
  process.exit(1);
}
