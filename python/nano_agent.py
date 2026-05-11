"""
Nano Agent - Lightweight agentic loop for Jarvis
Ported from nanocode, supports Anthropic API and OpenRouter
"""

import os
import json
import subprocess
import fnmatch
from pathlib import Path
from typing import Any


try:
    import anthropic
    ANTHROPIC_AVAILABLE = True
except ImportError:
    ANTHROPIC_AVAILABLE = False


TOOLS = [
    {
        "name": "read_file",
        "description": "Read the contents of a file at the given path.",
        "input_schema": {
            "type": "object",
            "properties": {"path": {"type": "string", "description": "File path to read"}},
            "required": ["path"],
        },
    },
    {
        "name": "write_file",
        "description": "Write content to a file, creating it if it doesn't exist.",
        "input_schema": {
            "type": "object",
            "properties": {
                "path": {"type": "string", "description": "File path to write"},
                "content": {"type": "string", "description": "Content to write"},
            },
            "required": ["path", "content"],
        },
    },
    {
        "name": "edit_file",
        "description": "Replace a specific string in a file with new content.",
        "input_schema": {
            "type": "object",
            "properties": {
                "path": {"type": "string"},
                "old_str": {"type": "string", "description": "Exact string to replace"},
                "new_str": {"type": "string", "description": "Replacement string"},
            },
            "required": ["path", "old_str", "new_str"],
        },
    },
    {
        "name": "glob_files",
        "description": "Find files matching a glob pattern.",
        "input_schema": {
            "type": "object",
            "properties": {
                "pattern": {"type": "string", "description": "Glob pattern, e.g. '**/*.py'"},
                "base_dir": {"type": "string", "description": "Base directory (default: current)"},
            },
            "required": ["pattern"],
        },
    },
    {
        "name": "grep_files",
        "description": "Search for a string or regex in files.",
        "input_schema": {
            "type": "object",
            "properties": {
                "pattern": {"type": "string", "description": "Search pattern"},
                "path": {"type": "string", "description": "File or directory to search"},
                "glob": {"type": "string", "description": "File glob filter, e.g. '*.py'"},
            },
            "required": ["pattern"],
        },
    },
    {
        "name": "run_command",
        "description": "Run a shell command and return stdout+stderr.",
        "input_schema": {
            "type": "object",
            "properties": {
                "command": {"type": "string", "description": "Shell command to execute"},
                "timeout": {"type": "integer", "description": "Timeout in seconds (default 30)"},
            },
            "required": ["command"],
        },
    },
]


def _tool_read_file(path: str) -> str:
    try:
        return Path(path).read_text(encoding="utf-8", errors="replace")
    except Exception as e:
        return f"Error reading {path}: {e}"


def _tool_write_file(path: str, content: str) -> str:
    try:
        p = Path(path)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(content, encoding="utf-8")
        return f"Written {len(content)} chars to {path}"
    except Exception as e:
        return f"Error writing {path}: {e}"


def _tool_edit_file(path: str, old_str: str, new_str: str) -> str:
    try:
        text = Path(path).read_text(encoding="utf-8", errors="replace")
        if old_str not in text:
            return f"Error: string not found in {path}"
        updated = text.replace(old_str, new_str, 1)
        Path(path).write_text(updated, encoding="utf-8")
        return f"Replaced 1 occurrence in {path}"
    except Exception as e:
        return f"Error editing {path}: {e}"


def _tool_glob_files(pattern: str, base_dir: str = ".") -> str:
    try:
        base = Path(base_dir)
        matches = list(base.glob(pattern))
        if not matches:
            return "No files matched."
        return "\n".join(str(m) for m in sorted(matches)[:100])
    except Exception as e:
        return f"Error: {e}"


def _tool_grep_files(pattern: str, path: str = ".", glob: str = None) -> str:
    try:
        cmd = ["rg", "--line-number", pattern, path]
        if glob:
            cmd += ["--glob", glob]
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=15)
        out = result.stdout or result.stderr
        lines = out.splitlines()[:50]
        return "\n".join(lines) if lines else "No matches found."
    except FileNotFoundError:
        # fallback: manual grep
        results = []
        import re
        rx = re.compile(pattern, re.IGNORECASE)
        for fp in Path(path).rglob("*"):
            if not fp.is_file():
                continue
            if glob and not fnmatch.fnmatch(fp.name, glob):
                continue
            try:
                for i, line in enumerate(fp.read_text(encoding="utf-8", errors="replace").splitlines(), 1):
                    if rx.search(line):
                        results.append(f"{fp}:{i}: {line.strip()}")
                        if len(results) >= 50:
                            break
            except Exception:
                continue
            if len(results) >= 50:
                break
        return "\n".join(results) if results else "No matches found."
    except Exception as e:
        return f"Error: {e}"


def _tool_run_command(command: str, timeout: int = 30) -> str:
    try:
        result = subprocess.run(
            command, shell=True, capture_output=True, text=True, timeout=timeout
        )
        out = result.stdout + result.stderr
        lines = out.splitlines()[:100]
        return "\n".join(lines) if lines else f"(exit {result.returncode})"
    except subprocess.TimeoutExpired:
        return f"Command timed out after {timeout}s"
    except Exception as e:
        return f"Error: {e}"


def _dispatch_tool(name: str, inputs: dict) -> str:
    if name == "read_file":
        return _tool_read_file(**inputs)
    if name == "write_file":
        return _tool_write_file(**inputs)
    if name == "edit_file":
        return _tool_edit_file(**inputs)
    if name == "glob_files":
        return _tool_glob_files(**inputs)
    if name == "grep_files":
        return _tool_grep_files(**inputs)
    if name == "run_command":
        return _tool_run_command(**inputs)
    return f"Unknown tool: {name}"


def run_task(
    task: str,
    model: str = "claude-haiku-4-5-20251001",
    max_turns: int = 20,
    verbose: bool = False,
) -> str:
    """
    Run a task using a lightweight agentic loop.
    Uses ANTHROPIC_API_KEY or OPENROUTER_API_KEY from env.
    Returns the final text response.
    """
    if not ANTHROPIC_AVAILABLE:
        return "Error: anthropic package not installed. Run: pip install anthropic"

    api_key = os.environ.get("ANTHROPIC_API_KEY")
    base_url = None

    if not api_key:
        api_key = os.environ.get("OPENROUTER_API_KEY")
        if api_key:
            base_url = "https://openrouter.ai/api/v1"
            model = model if "/" in model else f"anthropic/{model}"
        else:
            return "Error: set ANTHROPIC_API_KEY or OPENROUTER_API_KEY"

    client_kwargs = {"api_key": api_key}
    if base_url:
        client_kwargs["base_url"] = base_url

    client = anthropic.Anthropic(**client_kwargs)
    messages = [{"role": "user", "content": task}]

    for turn in range(max_turns):
        response = client.messages.create(
            model=model,
            max_tokens=4096,
            tools=TOOLS,
            messages=messages,
        )

        if verbose:
            print(f"[turn {turn+1}] stop_reason={response.stop_reason}")

        messages.append({"role": "assistant", "content": response.content})

        if response.stop_reason == "end_turn":
            for block in response.content:
                if hasattr(block, "text"):
                    return block.text
            return "(no text response)"

        if response.stop_reason == "tool_use":
            tool_results = []
            for block in response.content:
                if block.type == "tool_use":
                    result = _dispatch_tool(block.name, block.input)
                    if verbose:
                        print(f"  tool={block.name} -> {result[:80]}")
                    tool_results.append({
                        "type": "tool_result",
                        "tool_use_id": block.id,
                        "content": result,
                    })
            messages.append({"role": "user", "content": tool_results})
        else:
            break

    return "(max turns reached)"
