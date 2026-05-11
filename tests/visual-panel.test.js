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
  'id="showWorkspaceBtn"',
  'id="showVisualBtn"',
  'id="visualPanel"',
  'id="visualActiveFile"',
  'id="visualPendingState"',
  'id="visualTerminalState"',
  'id="visualMemoryState"',
  'id="visualObsidianState"',
  'id="visualLoopState"',
  "window.jarvisShowVisualPanelV1",
  "window.jarvisShowWorkspacePanelV1",
  "window.jarvisSetJarvisOverviewV1",
  "renderVisualPanelV1",
  "Workspace Panel",
  "Jarvis Översikt"
];

// Tomma forbidden-listan: refaktor 2026-05-10 lägger Brain-vy som inbyggd panel,
// så three.js / canvas / requestAnimationFrame får finnas i samma index.html.
// (Översikt-panelen själv är fortfarande lättviktig — det testas implicit via cell-id-markörerna.)
const forbiddenMarkers = [];

let failures = 0;

for (const marker of requiredMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL visual panel marker missing: ${marker}`);
  } else {
    console.log(`PASS visual panel marker present: ${marker}`);
  }
}

for (const marker of forbiddenMarkers) {
  if (html.toLowerCase().includes(marker.toLowerCase())) {
    failures += 1;
    console.log(`FAIL Visual Lab V1 must stay lightweight, found: ${marker}`);
  } else {
    console.log(`PASS lightweight Visual Lab avoids: ${marker}`);
  }
}

function makeElement() {
  const listeners = {};
  return {
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
    classList: {
      contains() { return false; },
      add() {},
      remove() {}
    }
  };
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
    getElementById: getElement,
    createElement() {
      return makeElement();
    }
  },
  chrome: {
    webview: {
      postMessage() {}
    }
  }
};

context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });

if (typeof context.jarvisShowVisualPanelV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisShowVisualPanelV1 is missing");
} else {
  context.jarvisShowVisualPanelV1();

  if (elements.visualPanel.style.display !== "flex") {
    failures += 1;
    console.log(`FAIL visual panel should become visible, got ${JSON.stringify(elements.visualPanel.style.display)}`);
  } else {
    console.log("PASS visual panel opens");
  }

  if (elements.editorArea.style.display !== "none") {
    failures += 1;
    console.log("FAIL editor textarea should hide while visual panel is visible");
  } else {
    console.log("PASS editor textarea hides for visual panel");
  }
}

if (typeof context.jarvisSetJarvisOverviewV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisSetJarvisOverviewV1 is missing");
} else {
  context.jarvisSetJarvisOverviewV1({
    memory: "Memory: 4 minnen",
    obsidian: "Obsidian: inte konfigurerat",
    loop: "Observe -> Think -> Plan -> Ask -> Act -> Verify -> Report -> Remember"
  });
  context.jarvisShowVisualPanelV1();

  if (!elements.visualMemoryState.textContent.includes("4 minnen")) {
    failures += 1;
    console.log("FAIL overview should render memory state");
  } else {
    console.log("PASS overview renders memory state");
  }

  if (!elements.visualObsidianState.textContent.includes("inte konfigurerat")) {
    failures += 1;
    console.log("FAIL overview should render obsidian state");
  } else {
    console.log("PASS overview renders obsidian state");
  }

  if (!elements.visualLoopState.textContent.includes("Observe")) {
    failures += 1;
    console.log("FAIL overview should render Jarvis loop state");
  } else {
    console.log("PASS overview renders Jarvis loop state");
  }
}

if (typeof context.jarvisShowWorkspacePanelV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisShowWorkspacePanelV1 is missing");
} else {
  context.jarvisShowWorkspacePanelV1();

  if (elements.visualPanel.style.display !== "none") {
    failures += 1;
    console.log("FAIL workspace panel should hide visual panel");
  } else {
    console.log("PASS workspace panel hides visual panel");
  }
}

if (failures > 0) {
  process.exit(1);
}
