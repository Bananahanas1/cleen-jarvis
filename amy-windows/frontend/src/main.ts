import "./style.css";
import { api } from "./api";
import { AmyOrb } from "./orb";
import type { AmyRow, SetupStatus } from "./types";

const app = document.querySelector<HTMLDivElement>("#app");
if (!app) throw new Error("Missing #app");

app.innerHTML = `
  <main class="shell">
    <aside class="rail">
      <div class="brand">
        <div class="mark">A</div>
        <div>
          <h1>Amy Windows</h1>
          <p>Standalone agent workspace</p>
        </div>
      </div>
      <section class="panel">
        <h2>Cloud brain</h2>
        <div id="providers" class="providers"></div>
      </section>
      <section class="panel">
        <h2>Browser agent</h2>
        <p id="browserStatus" class="muted">Loading...</p>
        <input id="browserGoal" placeholder="Plan a browser task" />
        <button id="browserPlanBtn">Plan</button>
      </section>
    </aside>

    <section class="stage">
      <canvas id="orbCanvas"></canvas>
      <div class="stageOverlay">
        <p id="setupLine">Checking setup...</p>
        <h2>Think, speak, remember, act.</h2>
        <div class="chatRow">
          <input id="chatPrompt" placeholder="Ask Amy..." />
          <button id="chatBtn">Send</button>
        </div>
        <pre id="chatResult">Amy is ready to boot without secrets.</pre>
      </div>
    </section>

    <aside class="workspace">
      <section class="panel composer">
        <h2>Capture</h2>
        <input id="itemTitle" placeholder="Title" />
        <textarea id="itemBody" placeholder="Task, memory, or note body"></textarea>
        <div class="buttonGrid">
          <button id="taskBtn">Task</button>
          <button id="memoryBtn">Memory</button>
          <button id="noteBtn">Note</button>
        </div>
      </section>
      <section class="panel">
        <h2>Live work</h2>
        <div id="runs" class="rows"></div>
      </section>
      <section class="panel">
        <h2>Tasks</h2>
        <div id="tasks" class="rows"></div>
      </section>
    </aside>
  </main>
`;

const orb = new AmyOrb(document.querySelector<HTMLCanvasElement>("#orbCanvas")!);

function el<T extends HTMLElement>(selector: string): T {
  const found = document.querySelector<T>(selector);
  if (!found) throw new Error(`Missing ${selector}`);
  return found;
}

function renderProviders(setup: SetupStatus) {
  const providers = el<HTMLDivElement>("#providers");
  providers.innerHTML = setup.providers
    .map(
      (p) => `
        <div class="provider ${p.configured ? "ready" : "missing"}">
          <span>${p.name}</span>
          <small>${p.role}${p.model ? ` / ${p.model}` : ""}</small>
        </div>`
    )
    .join("");
  el("#setupLine").textContent = setup.cloudReady
    ? `${setup.ready.length} cloud adapter(s) configured`
    : "No cloud keys yet. Local shell is still usable.";
  el("#browserStatus").textContent = `${setup.browser.engine} / ${setup.browser.mode} / autorun ${setup.browser.autorun ? "on" : "off"}`;
  orb.setReadiness(setup.ready.length);
}

function renderRows(target: string, rows: AmyRow[]) {
  el<HTMLDivElement>(target).innerHTML =
    rows
      .map((row) => {
        const title = row.title || row.kind || `#${row.id}`;
        const body = row.body || row.details || row.summary || row.state || "";
        return `<article class="row"><strong>${escapeHtml(title)}</strong><span>${escapeHtml(body)}</span></article>`;
      })
      .join("") || `<p class="muted">Empty</p>`;
}

function escapeHtml(value: string) {
  return value.replace(/[&<>"']/g, (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[ch]!);
}

async function refresh() {
  const setup = await api.setup();
  renderProviders(setup);
  renderRows("#tasks", await api.rows("tasks"));
  renderRows("#runs", await api.rows("runs"));
}

async function save(kind: "task" | "memory" | "note") {
  const title = el<HTMLInputElement>("#itemTitle").value.trim();
  const body = el<HTMLTextAreaElement>("#itemBody").value.trim();
  if (!title) return;
  if (kind === "task") await api.createTask(title, body);
  if (kind === "memory") await api.createMemory(title, body);
  if (kind === "note") await api.createNote(title, body);
  el<HTMLInputElement>("#itemTitle").value = "";
  el<HTMLTextAreaElement>("#itemBody").value = "";
  await refresh();
}

el("#taskBtn").addEventListener("click", () => save("task"));
el("#memoryBtn").addEventListener("click", () => save("memory"));
el("#noteBtn").addEventListener("click", () => save("note"));
el("#chatBtn").addEventListener("click", async () => {
  const prompt = el<HTMLInputElement>("#chatPrompt").value.trim();
  if (!prompt) return;
  el("#chatResult").textContent = "Thinking...";
  const result = await api.chat(prompt);
  el("#chatResult").textContent = result.text;
  await refresh();
});
el("#browserPlanBtn").addEventListener("click", async () => {
  const goal = el<HTMLInputElement>("#browserGoal").value.trim();
  if (!goal) return;
  const plan = await api.browserPlan(goal);
  el("#chatResult").textContent = `${plan.status}: ${plan.reason}\n- ${plan.steps.join("\n- ")}`;
  await refresh();
});

function connectSocket() {
  const ws = new WebSocket(`ws://${location.hostname}:8787/ws`);
  ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    if (data.type === "status") {
      renderProviders(data.setup);
      renderRows("#tasks", data.tasks || []);
      renderRows("#runs", data.runs || []);
    }
  };
  ws.onclose = () => setTimeout(connectSocket, 2000);
}

refresh().catch((err) => {
  el("#setupLine").textContent = "Backend is not reachable";
  el("#chatResult").textContent = String(err);
});
connectSocket();
