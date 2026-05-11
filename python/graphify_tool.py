import json
import os
import re
import hashlib
import math
import sys
from collections import deque
from pathlib import Path
from typing import Any


PROJECT_ROOT = Path(__file__).resolve().parents[1]
GRAPH_PATH = Path(os.environ.get("JARVIS_GRAPHIFY_GRAPH", PROJECT_ROOT / "graphify-out" / "graph.json"))
_GRAPH_CACHE: dict[str, Any] | None = None
_DEGREE_CACHE: dict[str, int] | None = None

_VISUAL_PALETTE = [
    "#5b8bd9",
    "#ff9a2e",
    "#f45b69",
    "#73d2de",
    "#67b346",
    "#f4d35e",
    "#bd7eb8",
    "#ff8fa3",
    "#b8b8c8",
    "#a77c5b",
    "#4ad3f5",
    "#ff6f00",
    "#46e27f",
    "#f24fff",
    "#8dd7cf",
    "#ffd447",
]


def _load_graph() -> dict[str, Any]:
    global _GRAPH_CACHE
    if _GRAPH_CACHE is None:
        if not GRAPH_PATH.exists():
            raise FileNotFoundError(f"Graphify graph not found: {GRAPH_PATH}")
        _GRAPH_CACHE = json.loads(GRAPH_PATH.read_text(encoding="utf-8"))
    return _GRAPH_CACHE


def _links(graph: dict[str, Any]) -> list[dict[str, Any]]:
    raw_links = graph.get("links") or graph.get("edges") or []
    return [item for item in raw_links if isinstance(item, dict)]


def _degree_map(graph: dict[str, Any]) -> dict[str, int]:
    global _DEGREE_CACHE
    if _DEGREE_CACHE is not None:
        return _DEGREE_CACHE

    degrees: dict[str, int] = {}
    for link in _links(graph):
        source = str(link.get("source") or link.get("_src") or "")
        target = str(link.get("target") or link.get("_tgt") or "")
        if source:
            degrees[source] = degrees.get(source, 0) + 1
        if target:
            degrees[target] = degrees.get(target, 0) + 1

    _DEGREE_CACHE = degrees
    return degrees


def _terms(query: str) -> list[str]:
    return [term for term in re.findall(r"[a-zA-Z0-9_]+", query.lower()) if len(term) > 1]


def _node_text(node: dict[str, Any]) -> str:
    values = [
        node.get("id"),
        node.get("label"),
        node.get("norm_label"),
        node.get("source_file"),
        node.get("file_type"),
    ]
    return " ".join(str(value or "") for value in values).lower()


def _node_summary(node: dict[str, Any], degrees: dict[str, int], score: int | None = None) -> dict[str, Any]:
    node_id = str(node.get("id") or "")
    payload = {
        "id": node_id,
        "label": str(node.get("label") or node_id),
        "source_file": str(node.get("source_file") or ""),
        "source_location": str(node.get("source_location") or ""),
        "file_type": str(node.get("file_type") or ""),
        "community": node.get("community", ""),
        "degree": degrees.get(node_id, 0),
    }
    if score is not None:
        payload["score"] = score
    return payload


def _safe_graph_path(value: Any) -> str:
    """Return dashboard-safe relative paths for real Graphify source references."""
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


def _stable_unit(value: Any, salt: str = "") -> float:
    digest = hashlib.sha1(f"{value}:{salt}".encode("utf-8", errors="ignore")).hexdigest()[:12]
    return int(digest, 16) / float(0xFFFFFFFFFFFF)


def _community_center(community: Any, index: int, total: int) -> tuple[float, float, float]:
    """Spread communities into a 3D cosmic map while keeping layout deterministic."""
    golden_angle = math.pi * (3 - math.sqrt(5))
    angle = index * golden_angle
    ring = 3.2 + (index % 5) * 0.42 + min(total, 120) * 0.002
    vertical = ((index % 9) - 4) * 0.34
    wobble = (_stable_unit(community, "wobble") - 0.5) * 0.55
    return (
        math.cos(angle) * ring * 1.18,
        vertical + wobble,
        math.sin(angle) * ring * 0.92,
    )


def _visual_position(node_id: str, community: Any, community_index: int, community_total: int, degree: int) -> dict[str, float]:
    cx, cy, cz = _community_center(community, community_index, community_total)
    angle = _stable_unit(node_id, "angle") * math.pi * 2
    tilt = (_stable_unit(node_id, "tilt") - 0.5) * math.pi
    degree_pull = min(max(degree, 0), 90) / 90
    local_radius = 0.12 + (1 - degree_pull) * 1.05 + _stable_unit(node_id, "radius") * 0.74
    local_y = math.sin(tilt) * local_radius * 0.52 + (_stable_unit(node_id, "lift") - 0.5) * 0.55
    return {
        "x": round(cx + math.cos(angle) * local_radius, 4),
        "y": round(cy + local_y, 4),
        "z": round(cz + math.sin(angle) * local_radius * 0.82, 4),
    }


def _visual_node_payload(
    node: dict[str, Any],
    *,
    degrees: dict[str, int],
    community_index: int,
    community_total: int,
    highlighted: bool,
) -> dict[str, Any]:
    raw_id = str(node.get("id") or "")
    degree = int(degrees.get(raw_id, 0))
    community = node.get("community", "")
    color = _VISUAL_PALETTE[community_index % len(_VISUAL_PALETTE)]
    label = str(node.get("label") or raw_id or "Graphify-nod")
    source_file = _safe_graph_path(node.get("source_file") or "")
    source_location = str(node.get("source_location") or "")
    return {
        "id": f"graphify:{raw_id}",
        "raw_id": raw_id,
        "label": label,
        "kind": "graphify_code",
        "source": "graphify",
        "path": source_file,
        "source_location": source_location,
        "community": community,
        "degree": degree,
        "confidence": round(min(0.98, 0.56 + min(degree, 90) / 180), 3),
        "freshness": 0.72,
        "region": "association",
        "summary": f"{label} | {source_file or source_location or 'Graphify'} | {degree} kopplingar",
        "related_ids": ["anchor:graphify", "anchor:association"],
        "position": _visual_position(raw_id, community, community_index, community_total, degree),
        "color": color,
        "size": round(0.42 + min(degree, 80) / 80 * 1.25, 3),
        "highlighted": highlighted,
        "metrics": {
            "degree": degree,
            "community": community,
            "file_type": node.get("file_type") or "",
        },
    }


def _selected_visual_node_ids(
    nodes: list[dict[str, Any]],
    links: list[dict[str, Any]],
    degrees: dict[str, int],
    query: str,
    limit: int,
) -> tuple[list[str], set[str]]:
    """Pick enough real nodes for a readable graph while preserving search focus."""
    node_by_id = {str(node.get("id") or ""): node for node in nodes}
    valid_ids = [node_id for node_id in node_by_id if node_id]
    if limit >= len(valid_ids):
        terms = _terms(query)
        highlighted = {
            node_id
            for node_id, node in node_by_id.items()
            if terms and all(term in _node_text(node) for term in terms)
        }
        return valid_ids, highlighted

    terms = _terms(query)
    highlighted: set[str] = set()
    for node_id, node in node_by_id.items():
        haystack = _node_text(node)
        if terms and all(term in haystack for term in terms):
            highlighted.add(node_id)

    neighbor_ids: set[str] = set()
    if highlighted:
        for link in links:
            source = str(link.get("source") or link.get("_src") or "")
            target = str(link.get("target") or link.get("_tgt") or "")
            if source in highlighted and target:
                neighbor_ids.add(target)
            if target in highlighted and source:
                neighbor_ids.add(source)

    ranked = sorted(valid_ids, key=lambda node_id: degrees.get(node_id, 0), reverse=True)
    selected: list[str] = []
    seen: set[str] = set()
    for bucket in (sorted(highlighted), sorted(neighbor_ids, key=lambda node_id: degrees.get(node_id, 0), reverse=True), ranked):
        for node_id in bucket:
            if node_id not in node_by_id or node_id in seen:
                continue
            selected.append(node_id)
            seen.add(node_id)
            if len(selected) >= limit:
                return selected, highlighted
    return selected, highlighted


def graphify_visual_graph(query: str = "", max_nodes: int = 4000, max_edges: int = 9000) -> dict[str, Any]:
    """
    Return a click-ready 3D graph from the real Graphify artifact.

    The dashboard uses this as a spiderweb layer: every point maps back to an
    actual Graphify node, and every line is a real relation from graph.json.
    """
    graph = _load_graph()
    nodes = [node for node in graph.get("nodes", []) if isinstance(node, dict)]
    links = _links(graph)
    degrees = _degree_map(graph)
    node_limit = max(50, min(int(max_nodes), 4000))
    edge_limit = max(50, min(int(max_edges), 9000))

    selected_ids, highlighted_ids = _selected_visual_node_ids(nodes, links, degrees, query, node_limit)
    selected_set = set(selected_ids)
    community_values = sorted({str(node.get("community", "")) for node in nodes if str(node.get("id") or "") in selected_set})
    community_order = {community: index for index, community in enumerate(community_values)}
    node_by_id = {str(node.get("id") or ""): node for node in nodes}

    visual_nodes: list[dict[str, Any]] = []
    for node_id in selected_ids:
        node = node_by_id.get(node_id)
        if not node:
            continue
        community_key = str(node.get("community", ""))
        visual_nodes.append(_visual_node_payload(
            node,
            degrees=degrees,
            community_index=community_order.get(community_key, 0),
            community_total=max(1, len(community_values)),
            highlighted=node_id in highlighted_ids,
        ))

    visual_edges: list[dict[str, Any]] = []
    seen_edges: set[tuple[str, str, str]] = set()
    for link in links:
        source = str(link.get("source") or link.get("_src") or "")
        target = str(link.get("target") or link.get("_tgt") or "")
        if not source or not target or source == target:
            continue
        if source not in selected_set or target not in selected_set:
            continue
        relation = str(link.get("relation") or link.get("kind") or "link")
        edge_key = (source, target, relation)
        if edge_key in seen_edges:
            continue
        seen_edges.add(edge_key)
        visual_edges.append({
            "id": f"graphify-edge:{hashlib.sha1('|'.join(edge_key).encode('utf-8', errors='ignore')).hexdigest()[:14]}",
            "source": f"graphify:{source}",
            "target": f"graphify:{target}",
            "kind": relation,
            "weight": float(link.get("weight") or 1.0),
            "confidence": link.get("confidence") or link.get("confidence_score") or "EXTRACTED",
        })
        if len(visual_edges) >= edge_limit:
            break

    return {
        "source": "graphify",
        "layout": "deterministic-community-3d",
        "query": query,
        "graph_path": _safe_graph_path(GRAPH_PATH),
        "nodes": visual_nodes,
        "edges": visual_edges,
        "totals": {
            "available_nodes": len(nodes),
            "available_edges": len(links),
            "included_nodes": len(visual_nodes),
            "included_edges": len(visual_edges),
            "communities": len(community_values),
            "highlighted": len(highlighted_ids),
        },
    }


def graphify_query(query: str, top_k: int = 10) -> dict[str, Any]:
    """Search the generated project graph for architecture context."""
    graph = _load_graph()
    nodes = [node for node in graph.get("nodes", []) if isinstance(node, dict)]
    degrees = _degree_map(graph)
    terms = _terms(query)
    if not terms:
        return {"query": query, "matches": [], "total_nodes": len(nodes), "total_links": len(_links(graph))}

    scored: list[tuple[int, dict[str, Any]]] = []
    for node in nodes:
        haystack = _node_text(node)
        score = 0
        for term in terms:
            if term in haystack.split():
                score += 6
            elif term in haystack:
                score += 3
        if score <= 0:
            continue

        node_id = str(node.get("id") or "")
        scored.append((score * 1000 + min(degrees.get(node_id, 0), 999), node))

    scored.sort(key=lambda item: item[0], reverse=True)
    limit = max(1, min(int(top_k), 25))
    return {
        "query": query,
        "matches": [_node_summary(node, degrees, score=score) for score, node in scored[:limit]],
        "total_nodes": len(nodes),
        "total_links": len(_links(graph)),
        "graph_path": str(GRAPH_PATH),
    }


def graphify_summary(top_k: int = 10) -> dict[str, Any]:
    """Return a compact overview that can feed dashboards and agent context."""
    graph = _load_graph()
    nodes = [node for node in graph.get("nodes", []) if isinstance(node, dict)]
    links = _links(graph)
    degrees = _degree_map(graph)
    top_nodes = sorted(nodes, key=lambda node: degrees.get(str(node.get("id") or ""), 0), reverse=True)
    limit = max(1, min(int(top_k), 25))
    communities = sorted({str(node.get("community")) for node in nodes if node.get("community") is not None})
    return {
        "total_nodes": len(nodes),
        "total_links": len(links),
        "total_hyperedges": len(graph.get("hyperedges") or graph.get("graph", {}).get("hyperedges") or []),
        "communities": len(communities),
        "graph_path": str(GRAPH_PATH),
        "top_nodes": [_node_summary(node, degrees) for node in top_nodes[:limit]],
    }


def graphify_community(name: str, top_k: int = 50) -> dict[str, Any]:
    graph = _load_graph()
    nodes = [node for node in graph.get("nodes", []) if isinstance(node, dict)]
    degrees = _degree_map(graph)
    wanted = str(name).strip().lower()
    matches = [
        node for node in nodes
        if str(node.get("community", "")).lower() == wanted
        or wanted in str(node.get("label", "")).lower()
        or wanted in str(node.get("source_file", "")).lower()
    ]
    matches.sort(key=lambda node: degrees.get(str(node.get("id") or ""), 0), reverse=True)
    limit = max(1, min(int(top_k), 100))
    return {"community": name, "matches": [_node_summary(node, degrees) for node in matches[:limit]], "count": len(matches)}


def _resolve_node_id(graph: dict[str, Any], value: str) -> str | None:
    wanted = str(value or "").strip().lower()
    if not wanted:
        return None
    nodes = [node for node in graph.get("nodes", []) if isinstance(node, dict)]
    for field in ("id", "label", "norm_label"):
        for node in nodes:
            if str(node.get(field) or "").lower() == wanted:
                return str(node.get("id") or "")
    for node in nodes:
        if wanted in _node_text(node):
            return str(node.get("id") or "")
    return None


def graphify_path(source: str, target: str, max_depth: int = 8) -> dict[str, Any]:
    graph = _load_graph()
    nodes = {str(node.get("id") or ""): node for node in graph.get("nodes", []) if isinstance(node, dict)}
    degrees = _degree_map(graph)
    start = _resolve_node_id(graph, source)
    goal = _resolve_node_id(graph, target)
    if not start or not goal:
        return {"source": source, "target": target, "path": None, "error": "source or target node was not found"}

    adjacency: dict[str, list[str]] = {}
    for link in _links(graph):
        src = str(link.get("source") or link.get("_src") or "")
        tgt = str(link.get("target") or link.get("_tgt") or "")
        if not src or not tgt:
            continue
        adjacency.setdefault(src, []).append(tgt)
        adjacency.setdefault(tgt, []).append(src)

    queue: deque[list[str]] = deque([[start]])
    visited = {start}
    while queue:
        path = queue.popleft()
        if len(path) - 1 > max_depth:
            continue
        current = path[-1]
        if current == goal:
            return {
                "source": source,
                "target": target,
                "length": len(path) - 1,
                "path": [_node_summary(nodes[node_id], degrees) for node_id in path if node_id in nodes],
            }
        for neighbor in adjacency.get(current, []):
            if neighbor in visited:
                continue
            visited.add(neighbor)
            queue.append([*path, neighbor])

    return {"source": source, "target": target, "path": None, "error": "no path found within max_depth"}


def main() -> int:
    command = sys.argv[1] if len(sys.argv) > 1 else "summary"
    try:
        if command == "query":
            payload = graphify_query(sys.argv[2] if len(sys.argv) > 2 else "", int(sys.argv[3]) if len(sys.argv) > 3 else 10)
        elif command == "path":
            payload = graphify_path(sys.argv[2] if len(sys.argv) > 2 else "", sys.argv[3] if len(sys.argv) > 3 else "")
        elif command == "community":
            payload = graphify_community(sys.argv[2] if len(sys.argv) > 2 else "", int(sys.argv[3]) if len(sys.argv) > 3 else 50)
        else:
            payload = graphify_summary(int(sys.argv[2]) if len(sys.argv) > 2 else 10)
        print(json.dumps(payload, ensure_ascii=False))
        return 0
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
