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
  "approvalModal",
  "approvalTitle",
  "approvalDescription",
  "approvalMeta",
  "approvalPreview",
  "approvalApproveBtn",
  "approvalCancelBtn",
  "pendingApprovalHint"
];

let failures = 0;

for (const id of requiredIds) {
  if (!html.includes(`id="${id}"`)) {
    failures += 1;
    console.log(`FAIL approval popup id missing: ${id}`);
  } else {
    console.log(`PASS approval popup id present: ${id}`);
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
    style: {},
    dataset: {},
    className: "",
    title: "",
    disabled: false,
    nextElementSibling: null,
    appendChild() {},
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
    insertAdjacentElement() {},
    remove() {},
    focus() {
      context.__focusedElement = this;
    },
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

const postedMessages = [];
const scheduledTimers = [];
const context = {
  console,
  setTimeout(callback) {
    scheduledTimers.push(callback);
    return scheduledTimers.length;
  },
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
      postMessage(payload) {
        postedMessages.push(payload);
      }
    }
  }
};

context.window = context;
vm.createContext(context);
vm.runInContext(scriptMatch[1], context, { filename: "dashboard/index.html" });
scheduledTimers.length = 0;

if (typeof context.jarvisShowPendingApprovalV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisShowPendingApprovalV1 is missing");
}

if (typeof context.jarvisHidePendingApprovalV1 !== "function") {
  failures += 1;
  console.log("FAIL jarvisHidePendingApprovalV1 is missing");
}

if (typeof context.jarvisShowPendingApprovalV1 === "function") {
  context.jarvisShowPendingApprovalV1({
    title: "Godkann filskrivning",
    description: "Jarvis vill ersatta innehall i docs/test-agent.md.",
    meta: "Lage: skriv/ersatt",
    preview: "TESTAR PENDING APPROVAL"
  });

  if (!elements.approvalApproveBtn.disabled) {
    failures += 1;
    console.log("FAIL approve button should start locked");
  } else {
    console.log("PASS approve button starts locked");
  }

  if (context.__focusedElement !== elements.approvalCancelBtn) {
    failures += 1;
    console.log("FAIL cancel button should receive initial focus");
  } else {
    console.log("PASS cancel button receives initial focus");
  }

  elements.approvalApproveBtn.click();
  if (postedMessages.length !== 0) {
    failures += 1;
    console.log(`FAIL locked approve button posted payload: ${JSON.stringify(postedMessages.at(-1))}`);
  } else {
    console.log("PASS locked approve button does not post approval");
  }

  const unlock = scheduledTimers.shift();
  if (typeof unlock !== "function") {
    failures += 1;
    console.log("FAIL approve unlock timer was not scheduled");
  } else {
    unlock();
  }

  if (elements.approvalApproveBtn.disabled) {
    failures += 1;
    console.log("FAIL approve button should unlock after timer");
  } else {
    console.log("PASS approve button unlocks after timer");
  }

  if (elements.approvalModal.style.display !== "flex") {
    failures += 1;
    console.log(`FAIL approval modal should be visible, got ${JSON.stringify(elements.approvalModal.style.display)}`);
  } else {
    console.log("PASS approval modal becomes visible");
  }

  if (elements.pendingApprovalHint.style.display !== "flex" ||
      !elements.pendingApprovalHint.textContent.toLowerCase().includes("pending")) {
    failures += 1;
    console.log(`FAIL pending hint should show active approval state: ${elements.pendingApprovalHint.textContent}`);
  } else {
    console.log("PASS pending hint shows active approval state");
  }

  if (!elements.approvalTitle.textContent.includes("filskrivning")) {
    failures += 1;
    console.log("FAIL approval title was not rendered");
  } else {
    console.log("PASS approval title rendered");
  }

  if (!elements.approvalDescription.textContent.includes("docs/test-agent.md")) {
    failures += 1;
    console.log("FAIL approval description was not rendered");
  } else {
    console.log("PASS approval description rendered");
  }

  if (!elements.approvalMeta.textContent.includes("skriv/ersatt")) {
    failures += 1;
    console.log("FAIL approval meta was not rendered");
  } else {
    console.log("PASS approval meta rendered");
  }

  if (!elements.approvalPreview.textContent.includes("TESTAR PENDING APPROVAL")) {
    failures += 1;
    console.log("FAIL approval preview was not rendered");
  } else {
    console.log("PASS approval preview rendered");
  }

  elements.approvalApproveBtn.click();
  const approveMessage = postedMessages.at(-1);
  if (!approveMessage || approveMessage.type !== "jarvis_pending_approval_v1" || approveMessage.action !== "approve") {
    failures += 1;
    console.log(`FAIL approve button posted wrong payload: ${JSON.stringify(approveMessage)}`);
  } else {
    console.log("PASS approve button posts approval decision");
  }

  context.jarvisShowPendingApprovalV1({
    title: "Godkann filskrivning",
    description: "Jarvis vill lagga till text i docs/test-agent.md.",
    meta: "Lage: lagg till",
    preview: "mer text"
  });
  const secondUnlock = scheduledTimers.shift();
  if (typeof secondUnlock === "function") secondUnlock();
  elements.approvalCancelBtn.click();
  const cancelMessage = postedMessages.at(-1);
  if (!cancelMessage || cancelMessage.type !== "jarvis_pending_approval_v1" || cancelMessage.action !== "cancel") {
    failures += 1;
    console.log(`FAIL cancel button posted wrong payload: ${JSON.stringify(cancelMessage)}`);
  } else {
    console.log("PASS cancel button posts approval decision");
  }

  if (elements.pendingApprovalHint.style.display !== "none") {
    failures += 1;
    console.log("FAIL pending hint should hide after cancel");
  } else {
    console.log("PASS pending hint hides after cancel");
  }
}

if (failures > 0) {
  process.exit(1);
}
