import time
import warnings

try:
    from duckduckgo_search import DDGS
    _ddgs_ok = True
except ImportError:
    _ddgs_ok = False
    warnings.warn("duckduckgo-search not installed — jarvis_news unavailable")

_CACHE: dict = {}
_CACHE_TTL = 900
_CATEGORIES = {
    "technology": "technology news", "science": "science news",
    "top": "top news today", "world": "world news",
    "business": "business news", "health": "health news",
}

def get_news(category: str = "technology", max_results: int = 5) -> list:
    if not _ddgs_ok:
        return []
    now = time.time()
    key = f"{category}_{max_results}"
    if key in _CACHE and (now - _CACHE[key]["ts"]) < _CACHE_TTL:
        return _CACHE[key]["data"]
    query = _CATEGORIES.get(category, category + " news")
    try:
        results = []
        with DDGS() as ddgs:
            for r in ddgs.news(query, max_results=max_results):
                results.append({"title": r.get("title",""), "url": r.get("url",""),
                                  "snippet": r.get("body",""), "source": r.get("source",""),
                                  "published": r.get("date",""), "category": category})
        _CACHE[key] = {"data": results, "ts": now}
        return results
    except Exception:
        return []

def get_briefing() -> list:
    top = get_news("top", 5)
    tech = get_news("technology", 5)
    seen = set()
    result = []
    for item in top + tech:
        t = item["title"].lower()
        if t not in seen:
            seen.add(t); result.append(item)
    return result
