import hashlib
import time
from pathlib import Path
from typing import Any

from graphify_tool import graphify_query, graphify_summary, graphify_visual_graph
from obsidian_tool import obsidian_search


PROJECT_ROOT = Path(__file__).resolve().parents[1]

REGION_BY_KIND = {
    "jarvis_memory": "hippocampus",
    "neurolinked_semantic": "association",
    "graphify_code": "association",
    "obsidian_note": "prefrontal",
    "decision": "prefrontal",
    "open_issue": "predictive",
}


def _clamp(value: float, low: float = 0.0, high: float = 1.0) -> float:
    return max(low, min(high, value))


def _stable_id(prefix: str, value: Any) -> str:
    digest = hashlib.sha1(str(value).encode("utf-8", errors="ignore")).hexdigest()[:12]
    return f"{prefix}:{digest}"


def _short_text(value: Any, max_len: int = 180) -> str:
    text = " ".join(str(value or "").split())
    if len(text) <= max_len:
        return text
    return text[: max_len - 1].rstrip() + "…"


def _safe_relative_path(value: Any) -> str:
    """Keep dashboard payloads safe by never returning arbitrary absolute paths."""
    text = str(value or "").strip().replace("\\", "/")
    if not text:
        return ""
    candidate = Path(text)
    if not candidate.is_absolute():
        return text
    try:
        return candidate.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return candidate.name


def _freshness_from_timestamp(timestamp_value: Any) -> float:
    try:
        age = max(0.0, time.time() - float(timestamp_value))
    except (TypeError, ValueError):
        return 0.45
    if age < 3600:
        return 1.0
    if age < 86400:
        return 0.82
    if age < 604800:
        return 0.62
    return 0.38


def _memory_node(entry: dict[str, Any]) -> dict[str, Any]:
    entry_id = entry.get("id") or entry.get("entry_id") or entry.get("timestamp") or entry.get("text")
    strength = entry.get("strength")
    try:
        confidence = _clamp(0.55 + min(float(strength or 1.0), 2.0) * 0.18)
    except (TypeError, ValueError):
        confidence = 0.72
    text = entry.get("text") or entry.get("content") or ""
    source = str(entry.get("source") or "jarvis")
    return {
        "id": _stable_id("memory", entry_id),
        "label": _short_text(text, 44) or "Jarvis-minne",
        "kind": "jarvis_memory",
        "source": source,
        "confidence": round(confidence, 3),
        "freshness": round(_freshness_from_timestamp(entry.get("timestamp")), 3),
        "region": "hippocampus",
        "summary": _short_text(text, 180),
        "path": "",
        "related_ids": ["anchor:hippocampus", "anchor:memory_bridge"],
        "metrics": {
            "strength": strength,
            "source": source,
        },
    }


def _graph_node(item: dict[str, Any]) -> dict[str, Any]:
    node_id = item.get("id") or item.get("label") or item.get("source_file")
    degree = int(item.get("degree") or 0)
    score = float(item.get("score") or 0)
    confidence = _clamp(0.58 + min(degree, 80) / 220 + min(score, 10000) / 40000)
    label = item.get("label") or item.get("id") or "Graphify-nod"
    source_file = _safe_relative_path(item.get("source_file") or item.get("source_location"))
    return {
        "id": _stable_id("graphify", node_id),
        "label": _short_text(label, 54),
        "kind": "graphify_code",
        "source": "graphify",
        "confidence": round(confidence, 3),
        "freshness": 0.7,
        "region": "association",
        "summary": _short_text(f"{label} • {source_file} • {degree} kopplingar", 180),
        "path": source_file,
        "related_ids": ["anchor:graphify", "anchor:association"],
        "metrics": {
            "degree": degree,
            "community": item.get("community", ""),
            "score": item.get("score", 0),
        },
    }


def _obsidian_node(item: dict[str, Any]) -> dict[str, Any]:
    path = _safe_relative_path(item.get("path"))
    label = item.get("title") or Path(path).stem or "Obsidian-anteckning"
    score = float(item.get("score") or 0)
    confidence = _clamp(0.55 + min(score, 25) / 80)
    excerpt = item.get("excerpt") or ""
    return {
        "id": _stable_id("obsidian", path or label),
        "label": _short_text(label, 54),
        "kind": "obsidian_note",
        "source": "obsidian",
        "confidence": round(confidence, 3),
        "freshness": 0.74,
        "region": "prefrontal",
        "summary": _short_text(excerpt or path, 180),
        "path": path,
        "related_ids": ["anchor:obsidian", "anchor:prefrontal"],
        "metrics": {
            "score": item.get("score", 0),
            "bytes": item.get("bytes", 0),
        },
    }


def _dedupe_nodes(nodes: list[dict[str, Any]]) -> list[dict[str, Any]]:
    seen: set[str] = set()
    deduped: list[dict[str, Any]] = []
    for node in nodes:
        node_id = str(node.get("id") or "")
        if not node_id or node_id in seen:
            continue
        seen.add(node_id)
        deduped.append(node)
    return deduped


def _edges_for_nodes(nodes: list[dict[str, Any]]) -> list[dict[str, Any]]:
    edges: list[dict[str, Any]] = []
    for node in nodes:
        node_id = node["id"]
        region = node.get("region") or "association"
        edges.append({
            "id": _stable_id("edge", f"anchor:{region}->{node_id}"),
            "source": f"anchor:{region}",
            "target": node_id,
            "kind": "region_anchor",
            "weight": round(float(node.get("confidence", 0.5)), 3),
        })
        for related in node.get("related_ids", [])[:3]:
            if str(related).startswith("anchor:") and related != f"anchor:{region}":
                edges.append({
                    "id": _stable_id("edge", f"{related}->{node_id}"),
                    "source": related,
                    "target": node_id,
                    "kind": "source_anchor",
                    "weight": 0.45,
                })
    return edges


def _activity_payload(
    *,
    visual_graph: dict[str, Any],
    visual_nodes: list[dict[str, Any]],
    visual_edges: list[dict[str, Any]],
    memory_total: int,
    obsidian_count: int,
) -> dict[str, Any]:
    graph_totals = visual_graph.get("totals") or {}
    graph_nodes = int(graph_totals.get("included_nodes") or len(visual_graph.get("nodes") or []))
    graph_edges = int(graph_totals.get("included_edges") or len(visual_graph.get("edges") or []))
    knowledge_nodes = graph_nodes + len(visual_nodes)
    knowledge_synapses = graph_edges + len(visual_edges)
    return {
        "knowledge_nodes": knowledge_nodes,
        "knowledge_synapses": knowledge_synapses,
        "memory_nodes": memory_total,
        "obsidian_nodes": obsidian_count,
        "graph_nodes": graph_nodes,
        "graph_edges": graph_edges,
        "memory_load": round(_clamp(memory_total / 500), 3),
        "graph_load": round(_clamp(graph_nodes / 4000), 3),
        "obsidian_load": round(_clamp(obsidian_count / 240), 3),
        "synapse_load": round(_clamp(graph_edges / 9000), 3),
    }


def build_knowledge_map(
    *,
    memory_entries: list[dict[str, Any]] | None = None,
    memory_stats: dict[str, Any] | None = None,
    graph_query: str = "JarvisForm",
    obsidian_query: str = "OpenAiAgentHarness",
    top_k: int = 8,
    graph_node_limit: int = 4000,
    graph_edge_limit: int = 9000,
) -> dict[str, Any]:
    """
    Build the shared visual node contract used by NeuroLinked's 3D dashboard.

    This intentionally normalizes Graphify, Obsidian and Jarvis memory into one
    small DTO so the UI does not need to parse raw graph/vault/backend shapes.
    """
    limit = max(1, min(int(top_k), 20))
    visual_node_limit = max(50, min(int(graph_node_limit), 4000))
    visual_edge_limit = max(50, min(int(graph_edge_limit), 9000))
    nodes: list[dict[str, Any]] = []
    errors: list[str] = []

    for entry in (memory_entries or [])[:limit]:
        if isinstance(entry, dict):
            nodes.append(_memory_node(entry))

    graph_totals: dict[str, Any] = {"total_nodes": 0, "total_links": 0}
    visual_graph: dict[str, Any] = {
        "source": "graphify",
        "layout": "unavailable",
        "nodes": [],
        "edges": [],
        "totals": {"included_nodes": 0, "included_edges": 0},
    }
    try:
        graph_totals = graphify_summary(top_k=limit)
        for item in graphify_query(graph_query, top_k=limit).get("matches", []):
            nodes.append(_graph_node(item))
        visual_graph = graphify_visual_graph(
            graph_query,
            max_nodes=visual_node_limit,
            max_edges=visual_edge_limit,
        )
    except Exception as exc:  # pragma: no cover - defensive for local missing graph artifacts
        errors.append(f"graphify: {exc}")

    obsidian_available = False
    obsidian_count = 0
    try:
        obsidian_payload = obsidian_search(obsidian_query, top_k=limit)
        obsidian_available = bool(obsidian_payload.get("available", True))
        obsidian_count = int(obsidian_payload.get("count") or len(obsidian_payload.get("matches") or []))
        for item in obsidian_payload.get("matches", []):
            nodes.append(_obsidian_node(item))
    except Exception as exc:  # pragma: no cover - defensive for optional vault availability
        errors.append(f"obsidian: {exc}")

    nodes = _dedupe_nodes(nodes)
    visual_edges = _edges_for_nodes(nodes)
    memory_total = int((memory_stats or {}).get("total_entries") or len(memory_entries or []))
    counts: dict[str, int] = {}
    for node in nodes:
        kind = str(node.get("kind") or "unknown")
        counts[kind] = counts.get(kind, 0) + 1

    anchors = [
        {"id": "anchor:chat", "label": "Chatt", "region": "chat"},
        {"id": "anchor:jarvis_bridge", "label": "Jarvis-brygga", "region": "jarvis_bridge"},
        {"id": "anchor:memory_bridge", "label": "Minnesbrygga", "region": "memory_bridge"},
        {"id": "anchor:hippocampus", "label": "Hippocampus", "region": "hippocampus"},
        {"id": "anchor:association", "label": "Associationsområde", "region": "association"},
        {"id": "anchor:prefrontal", "label": "Prefrontal bark", "region": "prefrontal"},
        {"id": "anchor:graphify", "label": "Graphify", "region": "graphify"},
        {"id": "anchor:obsidian", "label": "Obsidian", "region": "obsidian"},
    ]

    return {
        "status": "ok" if not errors else "degraded",
        "generated_at": time.time(),
        "query": {
            "graphify": graph_query,
            "obsidian": obsidian_query,
            "top_k": limit,
            "graph_node_limit": visual_node_limit,
            "graph_edge_limit": visual_edge_limit,
        },
        "nodes": nodes,
        "edges": visual_edges,
        "graph": visual_graph,
        "anchors": anchors,
        "counts": counts,
        "totals": {
            "jarvis_memory": memory_total,
            "graphify_nodes": int(graph_totals.get("total_nodes") or 0),
            "graphify_links": int(graph_totals.get("total_links") or 0),
            "graph_nodes": int((visual_graph.get("totals") or {}).get("included_nodes") or len(visual_graph.get("nodes") or [])),
            "graph_edges": int((visual_graph.get("totals") or {}).get("included_edges") or len(visual_graph.get("edges") or [])),
            "obsidian_matches": obsidian_count,
            "visual_nodes": len(nodes) + len(visual_graph.get("nodes") or []),
        },
        "neural_activity": _activity_payload(
            visual_graph=visual_graph,
            visual_nodes=nodes,
            visual_edges=visual_edges,
            memory_total=memory_total,
            obsidian_count=obsidian_count,
        ),
        "health": {
            "jarvis": "ok",
            "neurolinked": "ok",
            "graphify": "ok" if graph_totals.get("total_nodes") else "warn",
            "obsidian": "ok" if obsidian_available else "warn",
        },
        "errors": errors,
    }


def build_knowledge_status(knowledge_map: dict[str, Any]) -> dict[str, Any]:
    totals = knowledge_map.get("totals", {}) or {}
    health = knowledge_map.get("health", {}) or {}
    chips = [
        {"id": "jarvis", "label": "Jarvis", "status": health.get("jarvis", "ok"), "value": "online"},
        {"id": "neurolinked", "label": "NeuroLinked", "status": health.get("neurolinked", "ok"), "value": "aktiv"},
        {"id": "graphify", "label": "Graphify", "status": health.get("graphify", "warn"), "value": f"{totals.get('graphify_nodes', 0)} noder"},
        {"id": "obsidian", "label": "Obsidian", "status": health.get("obsidian", "warn"), "value": f"{totals.get('obsidian_matches', 0)} träffar"},
        {"id": "codex", "label": "Codex", "status": "ok", "value": "verktyg"},
        {"id": "groq_fallback", "label": "Groq fallback", "status": "warn", "value": "text-only"},
        {"id": "ollama_fallback", "label": "Ollama Qwen3", "status": "warn", "value": "lokal text"},
    ]
    return {
        "status": knowledge_map.get("status", "unknown"),
        "generated_at": knowledge_map.get("generated_at"),
        "chips": chips,
        "totals": totals,
        "counts": knowledge_map.get("counts", {}),
        "errors": knowledge_map.get("errors", []),
    }
