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
  'mode: "create"',
  'parsed.mode === "create"',
  'event.key === " "'
];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL create-mode marker missing: ${marker}`);
  } else {
    console.log(`PASS create-mode marker present: ${marker}`);
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
    dispatch(eventType, payload) {
      for (const handler of (listeners[eventType] || [])) handler(payload);
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

if (typeof context.splitFileCommandV11 !== "function") {
  failures += 1;
  console.log("FAIL splitFileCommandV11 must be defined");
} else {
  const cases = [
    ["/fil skapa ", "create", ""],
    ["/fil skapa docs/", "create", "docs/"],
    ["/fil skapa do", "create", "do"],
    ["skapa fil: docs/", "create", "docs/"],
    ["/fil öppna README.md", "open", "README.md"],
    ["skriv fil: docs/foo.md", "open", "docs/foo.md"]
  ];
  for (const [input, expectedMode, expectedQuery] of cases) {
    const r = context.splitFileCommandV11(input);
    if (!r.isFileCommand) {
      failures += 1;
      console.log(`FAIL splitFileCommandV11(${JSON.stringify(input)}) must report isFileCommand`);
      continue;
    }
    if (r.mode !== expectedMode) {
      failures += 1;
      console.log(`FAIL splitFileCommandV11(${JSON.stringify(input)}) mode expected ${expectedMode}, got ${r.mode}`);
    } else {
      console.log(`PASS splitFileCommandV11(${JSON.stringify(input)}) mode=${r.mode}`);
    }
    if (r.query !== expectedQuery) {
      failures += 1;
      console.log(`FAIL splitFileCommandV11(${JSON.stringify(input)}) query expected ${JSON.stringify(expectedQuery)}, got ${JSON.stringify(r.query)}`);
    }
  }
}

if (typeof context.fileSuggestions !== "function") {
  failures += 1;
  console.log("FAIL fileSuggestions must be defined");
} else {
  if (typeof context.jarvisSetAllFilesV7 === "function") {
    context.jarvisSetAllFilesV7(["README.md", "docs/foo.md"], ["docs", "app", "tests", "config"]);
  } else {
    failures += 1;
    console.log("FAIL jarvisSetAllFilesV7 must be defined to seed allFolders");
  }

  const r1 = context.fileSuggestions("/fil skapa ");
  const expected1 = ["/fil skapa docs/", "/fil skapa app/", "/fil skapa tests/", "/fil skapa config/"];
  for (const e of expected1) {
    if (!r1.includes(e)) {
      failures += 1;
      console.log(`FAIL /fil skapa empty query expected to include ${e}, got ${JSON.stringify(r1)}`);
    } else {
      console.log(`PASS /fil skapa empty query includes ${e}`);
    }
  }

  const r2 = context.fileSuggestions("/fil skapa do");
  if (!r2.includes("/fil skapa docs/")) {
    failures += 1;
    console.log(`FAIL /fil skapa do should include /fil skapa docs/, got ${JSON.stringify(r2)}`);
  } else {
    console.log("PASS /fil skapa do filters to docs/");
  }
  if (r2.some(s => s.includes("/fil skapa app/"))) {
    failures += 1;
    console.log(`FAIL /fil skapa do should not include app/, got ${JSON.stringify(r2)}`);
  } else {
    console.log("PASS /fil skapa do excludes non-matching folders");
  }

  const r3 = context.fileSuggestions("skapa fil: ");
  if (!r3.some(s => s.startsWith("skapa fil: ") && s.endsWith("/"))) {
    failures += 1;
    console.log(`FAIL skapa fil: empty query should produce folder suggestions with trailing /, got ${JSON.stringify(r3)}`);
  } else {
    console.log("PASS skapa fil: produces folder suggestions with trailing /");
  }

  const r4 = context.fileSuggestions("/fil skapa docs/foo.md = hej");
  if (r4.length > 0) {
    failures += 1;
    console.log(`FAIL /fil skapa with = should not show suggestions (content phase), got ${JSON.stringify(r4)}`);
  } else {
    console.log("PASS /fil skapa with = stops showing suggestions");
  }
}

if (failures > 0) {
  process.exit(1);
}
