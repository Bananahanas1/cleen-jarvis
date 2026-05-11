import json
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


DEFAULT_VAULT = Path.home() / "Documents" / "obsidian-vault"
VAULT_PATH = Path(os.environ.get("JARVIS_OBSIDIAN_VAULT", DEFAULT_VAULT))
MAX_NOTE_BYTES = 1024 * 1024


def _terms(query: str) -> list[str]:
    return [term for term in re.findall(r"[a-zA-Z0-9_]+", query.lower()) if len(term) > 1]


def _resolve_note_path(relative_path: str) -> Path:
    value = str(relative_path or "").strip().replace("\\", "/")
    if not value:
        raise ValueError("note path is required")
    candidate = Path(value)
    if candidate.is_absolute():
        raise ValueError("absolute note paths are not allowed")
    resolved_vault = VAULT_PATH.resolve()
    resolved_note = (resolved_vault / candidate).resolve()
    vault_text = str(resolved_vault).casefold()
    note_text = str(resolved_note).casefold()
    if note_text != vault_text and not note_text.startswith(vault_text + os.sep):
        raise ValueError("note path leaves the Obsidian vault")
    return resolved_note


def _relative(path: Path) -> str:
    return path.relative_to(VAULT_PATH.resolve()).as_posix()


def _read_text(path: Path, max_chars: int | None = None) -> str:
    content = path.read_text(encoding="utf-8", errors="ignore")
    if max_chars is not None and len(content) > max_chars:
        return content[:max_chars] + "\n[truncated]"
    return content


def _excerpt(content: str, terms: list[str], max_chars: int = 240) -> str:
    lowered = content.lower()
    index = -1
    for term in terms:
        index = lowered.find(term)
        if index >= 0:
            break
    if index < 0:
        index = 0
    start = max(0, index - 60)
    text = " ".join(content[start:start + max_chars].split())
    return text


def obsidian_search(query: str, top_k: int = 10) -> dict[str, Any]:
    """Search markdown notes inside the configured Obsidian vault."""
    if not VAULT_PATH.exists():
        return {"query": query, "matches": [], "vault_path": str(VAULT_PATH), "available": False}
    terms = _terms(query)
    if not terms:
        return {"query": query, "matches": [], "vault_path": str(VAULT_PATH), "available": True}

    matches: list[dict[str, Any]] = []
    for path in VAULT_PATH.rglob("*.md"):
        if not path.is_file() or path.stat().st_size > MAX_NOTE_BYTES:
            continue
        content = _read_text(path)
        lowered = content.lower()
        title = path.stem.lower()
        score = 0
        for term in terms:
            score += title.count(term) * 8
            score += lowered.count(term)
        if score <= 0:
            continue
        matches.append({
            "path": _relative(path),
            "title": path.stem,
            "score": score,
            "bytes": path.stat().st_size,
            "excerpt": _excerpt(content, terms),
        })

    matches.sort(key=lambda item: item["score"], reverse=True)
    limit = max(1, min(int(top_k), 25))
    return {
        "query": query,
        "matches": matches[:limit],
        "count": len(matches),
        "vault_path": str(VAULT_PATH),
        "available": True,
    }


def obsidian_read(relative_path: str, max_chars: int = 20000) -> dict[str, Any]:
    """Read one note while enforcing that the path stays inside the vault."""
    note_path = _resolve_note_path(relative_path)
    if not note_path.exists() or not note_path.is_file():
        raise FileNotFoundError(f"note not found: {relative_path}")
    limit = max(500, min(int(max_chars), 60000))
    content = _read_text(note_path, max_chars=limit)
    return {
        "path": _relative(note_path),
        "bytes": note_path.stat().st_size,
        "truncated": content.endswith("\n[truncated]"),
        "content": content,
    }


def obsidian_write(relative_path: str, content: str, tags: list[str] | None = None) -> dict[str, Any]:
    """Create or replace a note in the vault with basic Jarvis frontmatter."""
    safe_path = relative_path if relative_path.lower().endswith(".md") else relative_path + ".md"
    note_path = _resolve_note_path(safe_path)
    note_path.parent.mkdir(parents=True, exist_ok=True)
    clean_tags = tags or ["jarvis"]
    tag_line = "[" + ", ".join(json.dumps(tag, ensure_ascii=False) for tag in clean_tags) + "]"
    frontmatter = (
        "---\n"
        f"tags: {tag_line}\n"
        f"updated: {datetime.now(timezone.utc).isoformat()}\n"
        "source: jarvis\n"
        "---\n\n"
    )
    note_path.write_text(frontmatter + str(content or "").strip() + "\n", encoding="utf-8")
    return {"status": "ok", "path": _relative(note_path), "bytes": note_path.stat().st_size}


def main() -> int:
    command = sys.argv[1] if len(sys.argv) > 1 else "search"
    try:
        if command == "read":
            payload = obsidian_read(sys.argv[2] if len(sys.argv) > 2 else "", int(sys.argv[3]) if len(sys.argv) > 3 else 20000)
        elif command == "write":
            payload = obsidian_write(sys.argv[2] if len(sys.argv) > 2 else "", sys.argv[3] if len(sys.argv) > 3 else "")
        else:
            payload = obsidian_search(sys.argv[2] if len(sys.argv) > 2 else "", int(sys.argv[3]) if len(sys.argv) > 3 else 10)
        print(json.dumps(payload, ensure_ascii=False))
        return 0
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
