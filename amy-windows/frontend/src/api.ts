import type { AmyRow, SetupStatus } from "./types";

async function json<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    headers: { "Content-Type": "application/json" },
    ...init
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return (await res.json()) as T;
}

export const api = {
  setup: () => json<SetupStatus>("/api/setup"),
  rows: (kind: "memory" | "tasks" | "notes" | "runs") => json<AmyRow[]>(`/api/${kind}`),
  createTask: (title: string, details: string) =>
    json<AmyRow>("/api/tasks", { method: "POST", body: JSON.stringify({ title, details }) }),
  createMemory: (title: string, body: string) =>
    json<AmyRow>("/api/memory", { method: "POST", body: JSON.stringify({ title, body }) }),
  createNote: (title: string, body: string) =>
    json<AmyRow>("/api/notes", { method: "POST", body: JSON.stringify({ title, body }) }),
  chat: (prompt: string, context = "") =>
    json<{ provider: string; configured: boolean; text: string }>("/api/chat", {
      method: "POST",
      body: JSON.stringify({ prompt, context })
    }),
  browserPlan: (goal: string) =>
    json<{ goal: string; status: string; needsApproval: boolean; steps: string[]; reason: string }>("/api/browser/plan", {
      method: "POST",
      body: JSON.stringify({ goal })
    })
};
