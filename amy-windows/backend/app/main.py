from __future__ import annotations

import asyncio
from pathlib import Path
from typing import Literal

from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel, Field

from .config import ROOT, get_settings
from .database import init_db, insert_row, list_rows
from .services.browser_agent import browser_status, plan_browser_task
from .services.providers import ask_claude, setup_status


class ChatRequest(BaseModel):
    prompt: str = Field(min_length=1, max_length=8000)
    context: str = ""


class CreateItemRequest(BaseModel):
    title: str = Field(min_length=1, max_length=300)
    body: str = Field(default="", max_length=8000)


class CreateTaskRequest(BaseModel):
    title: str = Field(min_length=1, max_length=300)
    details: str = Field(default="", max_length=8000)


class BrowserTaskRequest(BaseModel):
    goal: str = Field(min_length=1, max_length=2000)


def create_app() -> FastAPI:
    app = FastAPI(title="Amy Windows", version="0.1.0")
    app.add_middleware(
        CORSMiddleware,
        allow_origins=[
            "http://127.0.0.1:5177",
            "http://localhost:5177",
        ],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    @app.on_event("startup")
    async def startup() -> None:
        init_db()

    @app.get("/api/health")
    async def health() -> dict[str, object]:
        settings = get_settings()
        return {
            "ok": True,
            "app": "amy-windows",
            "database": str(settings.database_path),
        }

    @app.get("/api/setup")
    async def setup() -> dict[str, object]:
        settings = get_settings()
        return {
            **setup_status(),
            "databasePath": str(settings.database_path),
            "browser": await browser_status(),
        }

    @app.post("/api/chat")
    async def chat(req: ChatRequest) -> dict[str, object]:
        result = await ask_claude(req.prompt, req.context)
        insert_row("runs", {"kind": "chat", "status": "completed", "summary": result["text"][:800]})
        return result

    @app.get("/api/{kind}")
    async def list_items(kind: Literal["memory", "tasks", "notes", "runs"]) -> list[dict[str, object]]:
        return list_rows(kind)

    @app.post("/api/memory")
    async def create_memory(req: CreateItemRequest) -> dict[str, object]:
        return insert_row("memory", {"title": req.title, "body": req.body})

    @app.post("/api/notes")
    async def create_note(req: CreateItemRequest) -> dict[str, object]:
        return insert_row("notes", {"title": req.title, "body": req.body})

    @app.post("/api/tasks")
    async def create_task(req: CreateTaskRequest) -> dict[str, object]:
        return insert_row("tasks", {"title": req.title, "details": req.details, "state": "open"})

    @app.post("/api/browser/plan")
    async def browser_plan(req: BrowserTaskRequest) -> dict[str, object]:
        plan = plan_browser_task(req.goal)
        insert_row("runs", {"kind": "browser", "status": plan.status, "summary": plan.goal})
        return {
            "goal": plan.goal,
            "status": plan.status,
            "needsApproval": plan.needs_approval,
            "steps": plan.steps,
            "reason": plan.reason,
        }

    @app.websocket("/ws")
    async def websocket(ws: WebSocket) -> None:
        await ws.accept()
        try:
            while True:
                await ws.send_json(
                    {
                        "type": "status",
                        "setup": setup_status(),
                        "tasks": list_rows("tasks", limit=5),
                        "runs": list_rows("runs", limit=5),
                    }
                )
                await asyncio.sleep(2)
        except WebSocketDisconnect:
            return

    dist = ROOT / "frontend" / "dist"
    if dist.exists():
        app.mount("/assets", StaticFiles(directory=dist / "assets"), name="assets")

        @app.get("/")
        async def index() -> FileResponse:
            return FileResponse(dist / "index.html")

    return app


app = create_app()
