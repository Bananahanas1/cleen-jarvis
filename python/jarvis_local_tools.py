import ast
import json
import math
import os
import platform
import shutil
import subprocess
import sys
import unicodedata
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any


ALLOWED_FUNCTIONS = {
    "abs": abs,
    "round": round,
    "sqrt": math.sqrt,
    "sin": math.sin,
    "cos": math.cos,
    "tan": math.tan,
    "log": math.log,
    "log10": math.log10,
}

ALLOWED_CONSTANTS = {
    "pi": math.pi,
    "e": math.e,
    "tau": math.tau,
}


@dataclass
class ToolResponse:
    handled: bool
    reply: str = ""
    action: dict[str, Any] | None = None
    tool: str | None = None

    def to_json(self) -> str:
        return json.dumps(
            {
                "handled": self.handled,
                "reply": self.reply,
                "action": self.action,
                "tool": self.tool,
            },
            ensure_ascii=True,
        )


def main() -> int:
    try:
        sys.stdin.reconfigure(encoding="utf-8")
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

    if "--probe" in sys.argv:
        print(
            json.dumps(
                {
                    "ok": True,
                    "python_version": platform.python_version(),
                    "executable": sys.executable,
                },
                ensure_ascii=True,
            )
        )
        return 0

    raw = sys.stdin.read()
    if not raw.strip():
        print(ToolResponse(False).to_json())
        return 0

    try:
        request = json.loads(raw)
    except json.JSONDecodeError:
        print(ToolResponse(False, "Invalid JSON request.", tool="error").to_json())
        return 0

    text = str(request.get("input", "")).strip()
    workspace = Path(str(request.get("workspace_root", "")).strip() or os.getcwd())
    response = handle_request(text, workspace)
    print(response.to_json())
    return 0


def handle_request(text: str, workspace: Path) -> ToolResponse:
    normalized = normalize(text)

    if not normalized:
        return ToolResponse(False)

    if is_calculation_request(normalized):
        expression = extract_expression(text)
        if not expression:
            return ToolResponse(True, "Give me an expression to calculate.", tool="calculator")

        try:
            result = evaluate_expression(expression)
        except Exception as exc:
            return ToolResponse(True, f"Could not calculate that: {exc}", tool="calculator")

        return ToolResponse(True, f"Result: {format_number(result)}", tool="calculator")

    if contains_any(normalized, "system info", "systeminfo", "verktygsstatus", "tools status", "toolchain", "python status", "dotnet status"):
        return ToolResponse(True, build_system_info(workspace), tool="system-info")

    if contains_any(normalized, "workspace status", "project status", "projektstatus", "build status", "latest build", "senaste build"):
        return ToolResponse(True, build_workspace_info(workspace), tool="workspace-info")

    if contains_any(normalized, "graphify", "kunskapsgraf", "graf sok", "graf sök", "sok graf", "sök graf"):
        query = extract_topic(text, ("graphify", "kunskapsgraf", "graf sök", "sök graf", "sok graf")) or text
        return ToolResponse(True, build_graphify_reply(query), tool="graphify-query")

    if contains_any(normalized, "obsidian", "vault", "valv", "anteckning", "anteckningar"):
        query = extract_topic(text, ("obsidian", "vault", "valv", "anteckning", "anteckningar")) or text
        return ToolResponse(True, build_obsidian_reply(query), tool="obsidian-search")

    if contains_any(normalized, "open project folder", "open workspace", "oppna projektmapp", "oppna projekt", "open dist folder", "oppna dist"):
        if "dist" in normalized:
            target = workspace / "dist"
        else:
            target = workspace

        return ToolResponse(
            True,
            f"Opening folder: {target}",
            action={"type": "open_folder", "target": str(target)},
            tool="folder-open",
        )

    return ToolResponse(False)


def normalize(text: str) -> str:
    lowered = text.lower().strip()
    folded = unicodedata.normalize("NFKD", lowered)
    ascii_only = "".join(char for char in folded if not unicodedata.combining(char))
    return " ".join(ascii_only.split())


def contains_any(text: str, *needles: str) -> bool:
    return any(needle in text for needle in needles)


def extract_topic(text: str, triggers: tuple[str, ...]) -> str:
    normalized_text = normalize(text)
    for trigger in triggers:
        index = normalized_text.find(normalize(trigger))
        if index < 0:
            continue
        original_words = text.split()
        normalized_words = normalized_text.split()
        trigger_words = normalize(trigger).split()
        if len(normalized_words) != len(original_words):
            return text
        for start in range(0, len(normalized_words) - len(trigger_words) + 1):
            if normalized_words[start:start + len(trigger_words)] == trigger_words:
                return " ".join(original_words[start + len(trigger_words):]).strip(" :?-")
    return ""


def is_calculation_request(text: str) -> bool:
    prefixes = (
        "calc ",
        "calculate ",
        "berakna ",
        "beräkna ",
        "rakna ",
        "räkna ",
        "what is ",
        "vad är ",
    )

    if text.startswith(prefixes):
        return True

    math_chars = set("0123456789+-*/().,^ ")
    return len(text) > 0 and all(char in math_chars for char in text)


def extract_expression(text: str) -> str:
    prefixes = [
        "calc ",
        "calculate ",
        "berakna ",
        "beräkna ",
        "rakna ",
        "räkna ",
        "what is ",
        "vad är ",
    ]

    lowered = text.lower()
    for prefix in prefixes:
        if lowered.startswith(prefix):
            expression = text[len(prefix):]
            return expression.strip().rstrip("?")

    return text.strip().rstrip("?")


def evaluate_expression(expression: str) -> float:
    cleaned = expression.replace(",", ".").replace("^", "**")
    tree = ast.parse(cleaned, mode="eval")
    return float(eval_node(tree.body))


def eval_node(node: ast.AST) -> float:
    if isinstance(node, ast.Constant) and isinstance(node.value, (int, float)):
        return float(node.value)

    if isinstance(node, ast.UnaryOp) and isinstance(node.op, (ast.UAdd, ast.USub)):
        value = eval_node(node.operand)
        return value if isinstance(node.op, ast.UAdd) else -value

    if isinstance(node, ast.BinOp) and isinstance(node.op, (ast.Add, ast.Sub, ast.Mult, ast.Div, ast.Pow, ast.Mod)):
        left = eval_node(node.left)
        right = eval_node(node.right)

        if isinstance(node.op, ast.Add):
            return left + right
        if isinstance(node.op, ast.Sub):
            return left - right
        if isinstance(node.op, ast.Mult):
            return left * right
        if isinstance(node.op, ast.Div):
            return left / right
        if isinstance(node.op, ast.Pow):
            return left ** right
        if isinstance(node.op, ast.Mod):
            return left % right

    if isinstance(node, ast.Call) and isinstance(node.func, ast.Name):
        func_name = node.func.id
        if func_name not in ALLOWED_FUNCTIONS:
            raise ValueError(f"Unsupported function '{func_name}'.")

        args = [eval_node(arg) for arg in node.args]
        return float(ALLOWED_FUNCTIONS[func_name](*args))

    if isinstance(node, ast.Name) and node.id in ALLOWED_CONSTANTS:
        return float(ALLOWED_CONSTANTS[node.id])

    raise ValueError("Unsupported expression.")


def build_system_info(workspace: Path) -> str:
    python_path = sys.executable
    dotnet_version = read_command_output(["dotnet", "--version"])
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    lines = [
        "System info",
        f"- time: {now}",
        f"- os: {platform.system()} {platform.release()}",
        f"- machine: {platform.machine()}",
        f"- python: {platform.python_version()} ({python_path})",
        f"- dotnet: {dotnet_version}",
        f"- workspace: {workspace}",
    ]

    return "\n".join(lines)


def build_workspace_info(workspace: Path) -> str:
    app_dir = workspace / "app"
    python_dir = workspace / "python"
    src_dir = workspace / "src"
    dist_dir = workspace / "dist"

    app_files = count_files(app_dir)
    python_files = count_files(python_dir)
    src_files = count_files(src_dir)
    latest_exe = latest_file(dist_dir, "*.exe")
    latest_zip = latest_file(dist_dir, "*.zip")

    lines = [
        "Workspace status",
        f"- root: {workspace}",
        f"- app files: {app_files}",
        f"- python files: {python_files}",
        f"- src files: {src_files}",
        f"- latest exe: {format_file_entry(latest_exe)}",
        f"- latest zip: {format_file_entry(latest_zip)}",
    ]

    return "\n".join(lines)


def build_graphify_reply(query: str) -> str:
    try:
        from graphify_tool import graphify_query, graphify_summary

        if not query.strip():
            summary = graphify_summary(top_k=5)
            rows = [f"- {node['label']} ({node['degree']} kopplingar)" for node in summary.get("top_nodes", [])]
            return "Kunskapsgraf\n" + "\n".join([
                f"- noder: {summary.get('total_nodes', 0)}",
                f"- länkar: {summary.get('total_links', 0)}",
                *rows,
            ])

        result = graphify_query(query, top_k=5)
        rows = [
            f"- {item['label']} | {item.get('source_file', '')} | score {item.get('score', 0)}"
            for item in result.get("matches", [])
        ]
        if not rows:
            return f"Jag hittade inga Graphify-träffar för: {query}"
        return "Graphify-träffar\n" + "\n".join(rows)
    except Exception as exc:
        return f"Graphify-sökning misslyckades: {exc}"


def build_obsidian_reply(query: str) -> str:
    try:
        from obsidian_tool import obsidian_search

        result = obsidian_search(query, top_k=5)
        rows = [
            f"- {item['path']} | score {item.get('score', 0)}"
            for item in result.get("matches", [])
        ]
        if not rows:
            return f"Jag hittade inga Obsidian-anteckningar för: {query}"
        return "Obsidian-träffar\n" + "\n".join(rows)
    except Exception as exc:
        return f"Obsidian-sökning misslyckades: {exc}"


def count_files(directory: Path) -> int:
    if not directory.exists():
        return 0

    ignored_directories = {"bin", "obj", ".git", "__pycache__"}
    count = 0

    for path in directory.rglob("*"):
        if not path.is_file():
            continue

        if any(part in ignored_directories for part in path.parts):
            continue

        count += 1

    return count


def latest_file(directory: Path, pattern: str) -> Path | None:
    if not directory.exists():
        return None

    files = [path for path in directory.glob(pattern) if path.is_file()]
    if not files:
        return None

    return max(files, key=lambda file_path: file_path.stat().st_mtime)


def format_file_entry(path: Path | None) -> str:
    if path is None:
        return "none"

    timestamp = datetime.fromtimestamp(path.stat().st_mtime).strftime("%Y-%m-%d %H:%M:%S")
    size_mb = path.stat().st_size / (1024 * 1024)
    return f"{path.name} ({size_mb:.1f} MB, {timestamp})"


def read_command_output(command: list[str]) -> str:
    executable = shutil.which(command[0])
    if executable is None:
        fallback = resolve_fallback_command(command[0])
        if fallback is None:
            return "not found"
        command = [fallback, *command[1:]]

    try:
        completed = subprocess.run(
            command,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
            timeout=8,
            check=False,
        )
    except Exception:
        return "unavailable"

    output = (completed.stdout or "").strip()
    if not output:
        return "unavailable"

    first_line = output.splitlines()[0].strip()
    return first_line


def resolve_fallback_command(name: str) -> str | None:
    if name == "dotnet":
        dotnet_path = Path("C:/Program Files/dotnet/dotnet.exe")
        if dotnet_path.exists():
            return str(dotnet_path)

    return None


def format_number(value: float) -> str:
    if abs(value - round(value)) < 1e-12:
        return str(int(round(value)))

    return f"{value:.10g}"


if __name__ == "__main__":
    raise SystemExit(main())
