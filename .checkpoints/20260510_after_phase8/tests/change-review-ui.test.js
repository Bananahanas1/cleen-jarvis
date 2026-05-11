const fs = require("fs");
const path = require("path");
const vm = require("vm");

const htmlPath = path.join(__dirname, "..", "dashboard", "index.html");
const html = fs.readFileSync(htmlPath, "utf8");
const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>/);

if (!scriptMatch) {
  throw new Error("Could not find dashboard script block.");
}

const requiredIds = [
  "changeReviewBar",
  "changeReviewSummary",
  "reviewChangesBtn",
  "closeChangeReviewBtn",
  "diffViewer",
  "diffHeader",
  "diffBody"
];

let failures = 0;

for (const id of requiredIds) {
  if (!html.includes(`id="${id}"`)) {
    failures += 1;
    console.log(`FAIL change review id missing: ${id}`);
  } else {
    console.log(`PASS change review id present: ${id}`);
  }
}

const sourceMarkers = [
  "window.jarvisShowFileChangeReviewV1",
  "window.jarvisHighlightProjectPathV1",
  "jarvis_review_file_change_v1",
  "row.dataset.filePath",
  "review-highlight",
  "diff-line added",
  "diff-line removed"
];

for (const marker of sourceMarkers) {
  if (!html.includes(marker)) {
    failures += 1;
    console.log(`FAIL dashboard source marker missing: ${marker}`);
  } else {
    console.log(`PASS dashboard source marker present: ${marker}`);
  }
}

function makeElement() {
  const listeners = {};
  const element = {
    value: "",
    textContent: "",
    scrollTop: 0,
    scrollHeight: 0,
    readOnly: true,
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

  Object.defineProperty(element, "innerHTML", {
    get() {
      return "";
    },
    set(value) {
      element.children = [];
      element.textContent = String(value || "").replace(/<[^>]+>/g, "");
    }
  });

  return element;
}

const elements = {};

function getElement(id) {
  elements[id] = elements[id] || makeElement();
  return elements[id];
}

const postedMessages = [];
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
    getElementById: getElement,
    createElement() {
      return makeElement();
    }
  },
  chrome: {
    webview: {
      postMessage(payload) {
        postedMessages.push(payload);
      }
    }
  }
};

context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });

if (typeof context.jarvisShowFileChangeReviewV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisShowFileChangeReviewV1 is missing");
}

if (typeof context.jarvisHighlightProjectPathV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisHighlightProjectPathV1 is missing");
}

if (typeof context.jarvisShowFileChangeReviewV1 === "function") {
  context.jarvisShowFileChangeReviewV1({
    path: "docs/test-agent.md",
    added: 2,
    removed: 1,
    lines: [
      { kind: "context", text: "same" },
      { kind: "removed", text: "old line" },
      { kind: "added", text: "new line" },
      { kind: "added", text: "another line" }
    ]
  });

  if (elements.changeReviewBar.style.display !== "flex") {
    failures += 1;
    console.log("FAIL change review bar should be visible");
  } else {
    console.log("PASS change review bar becomes visible");
  }

  if (!elements.changeReviewSummary.textContent.includes("+2") ||
      !elements.changeReviewSummary.textContent.includes("-1")) {
    failures += 1;
    console.log(`FAIL change review summary missing counts: ${elements.changeReviewSummary.textContent}`);
  } else {
    console.log("PASS change review summary renders counts");
  }

  context.jarvisShowFileChangeReviewV1({
    path: "docs/test-agent.md",
    changeKind: "delete",
    added: 0,
    removed: 1,
    lines: [
      { kind: "removed", text: "deleted line" }
    ]
  });

  if (!elements.changeReviewSummary.textContent.includes("1 fil raderad") ||
      !elements.changeReviewSummary.textContent.includes("+0") ||
      !elements.changeReviewSummary.textContent.includes("-1")) {
    failures += 1;
    console.log(`FAIL delete review summary should say file deleted: ${elements.changeReviewSummary.textContent}`);
  } else {
    console.log("PASS delete review summary says file deleted");
  }

  const expectedLabels = [
    ["create", "1 fil skapad"],
    ["write", "1 fil skriven"],
    ["append", "1 fil utökad"],
    ["undo", "1 fil återställd"]
  ];

  for (const [changeKind, expectedLabel] of expectedLabels) {
    context.jarvisShowFileChangeReviewV1({
      path: "docs/test-agent.md",
      changeKind,
      added: 1,
      removed: 0,
      lines: [
        { kind: "added", text: "changed line" }
      ]
    });

    if (!elements.changeReviewSummary.textContent.includes(expectedLabel)) {
      failures += 1;
      console.log(`FAIL ${changeKind} review summary should say ${expectedLabel}: ${elements.changeReviewSummary.textContent}`);
    } else {
      console.log(`PASS ${changeKind} review summary label`);
    }
  }

  context.jarvisShowFileChangeReviewV1({
    path: "docs/test-agent.md",
    added: 2,
    removed: 1,
    lines: [
      { kind: "context", text: "same" },
      { kind: "removed", text: "old line" },
      { kind: "added", text: "new line" },
      { kind: "added", text: "another line" }
    ]
  });

  elements.reviewChangesBtn.click();

  if (elements.diffViewer.style.display !== "flex") {
    failures += 1;
    console.log("FAIL diff viewer should be visible after review click");
  } else {
    console.log("PASS diff viewer opens on review click");
  }

  if (elements.editorArea.style.display !== "none") {
    failures += 1;
    console.log("FAIL editor textarea should hide while diff is visible");
  } else {
    console.log("PASS editor textarea hides while diff is visible");
  }

  const diffClasses = elements.diffBody.children.map(child => child.className);
  if (!diffClasses.includes("diff-line added") || !diffClasses.includes("diff-line removed")) {
    failures += 1;
    console.log(`FAIL diff line highlight classes missing: ${JSON.stringify(diffClasses)}`);
  } else {
    console.log("PASS diff line highlight classes rendered");
  }

  const reviewMessage = postedMessages.at(-1);
  if (!reviewMessage ||
      reviewMessage.type !== "jarvis_review_file_change_v1" ||
      reviewMessage.path !== "docs/test-agent.md") {
    failures += 1;
    console.log(`FAIL review button posted wrong payload: ${JSON.stringify(reviewMessage)}`);
  } else {
    console.log("PASS review button asks C# to open changed file path");
  }

  context.jarvisSetEditorFile("docs/test-agent.md", "new line\nanother line\n", true);

  if (elements.diffViewer.style.display !== "flex" ||
      elements.editorArea.style.display !== "none") {
    failures += 1;
    console.log("FAIL review diff should remain visible when C# opens the same file");
  } else {
    console.log("PASS review diff remains visible when C# opens the same file");
  }

  if (elements.closeChangeReviewBtn) {
    elements.closeChangeReviewBtn.click();

    if (elements.changeReviewBar.style.display !== "none" ||
        elements.diffViewer.style.display !== "none" ||
        elements.editorArea.style.display !== "") {
      failures += 1;
      console.log("FAIL close review button should hide review UI and return to file view");
    } else {
      console.log("PASS close review button hides review UI");
    }
  }
}

if (failures > 0) {
  process.exit(1);
}
