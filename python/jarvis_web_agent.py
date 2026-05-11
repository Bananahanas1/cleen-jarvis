"""
Jarvis Web Agent - Playwright-based browser automation (no Gemini dependency)
Ported from ada_v2/backend/web_agent.py
"""

import asyncio
import base64
from typing import Optional

try:
    from playwright.async_api import async_playwright, Browser, Page, BrowserContext
    PLAYWRIGHT_AVAILABLE = True
except ImportError:
    PLAYWRIGHT_AVAILABLE = False


class JarvisWebAgent:
    """Async browser automation agent using Playwright."""

    def __init__(self, headless: bool = True):
        self.headless = headless
        self._playwright = None
        self._browser: Optional[Browser] = None
        self._context: Optional[BrowserContext] = None
        self._page: Optional[Page] = None

    async def _ensure_browser(self):
        if not PLAYWRIGHT_AVAILABLE:
            raise RuntimeError("playwright not installed. Run: pip install playwright && playwright install chromium")
        if self._browser is None:
            self._playwright = await async_playwright().start()
            self._browser = await self._playwright.chromium.launch(headless=self.headless)
            self._context = await self._browser.new_context(
                user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            )
            self._page = await self._context.new_page()

    async def navigate(self, url: str) -> str:
        """Navigate to a URL and return page title."""
        await self._ensure_browser()
        await self._page.goto(url, wait_until="domcontentloaded", timeout=30000)
        return await self._page.title()

    async def search(self, query: str, engine: str = "duckduckgo") -> list[dict]:
        """Search the web and return results as list of {title, url, snippet}."""
        await self._ensure_browser()
        if engine == "duckduckgo":
            url = f"https://duckduckgo.com/?q={query.replace(' ', '+')}&kl=wt-wt"
        else:
            url = f"https://www.google.com/search?q={query.replace(' ', '+')}"
        await self._page.goto(url, wait_until="domcontentloaded", timeout=30000)
        await self._page.wait_for_timeout(1500)

        results = []
        if engine == "duckduckgo":
            items = await self._page.query_selector_all("[data-result='web']")
            for item in items[:5]:
                try:
                    title_el = await item.query_selector("h2 a")
                    snippet_el = await item.query_selector("[data-result='snippet']")
                    title = await title_el.inner_text() if title_el else ""
                    href = await title_el.get_attribute("href") if title_el else ""
                    snippet = await snippet_el.inner_text() if snippet_el else ""
                    if title:
                        results.append({"title": title, "url": href, "snippet": snippet})
                except Exception:
                    continue
        return results

    async def click(self, selector: str) -> bool:
        """Click an element by CSS selector. Returns True on success."""
        await self._ensure_browser()
        try:
            await self._page.click(selector, timeout=5000)
            return True
        except Exception:
            return False

    async def type_text(self, selector: str, text: str) -> bool:
        """Type text into an element. Returns True on success."""
        await self._ensure_browser()
        try:
            await self._page.fill(selector, text)
            return True
        except Exception:
            return False

    async def screenshot(self) -> bytes:
        """Take a screenshot and return PNG bytes."""
        await self._ensure_browser()
        return await self._page.screenshot(type="png")

    async def screenshot_base64(self) -> str:
        """Take a screenshot and return base64-encoded PNG."""
        data = await self.screenshot()
        return base64.b64encode(data).decode()

    async def get_page_text(self) -> str:
        """Extract visible text content from the current page."""
        await self._ensure_browser()
        return await self._page.evaluate("() => document.body.innerText")

    async def get_page_url(self) -> str:
        """Return current page URL."""
        if self._page is None:
            return ""
        return self._page.url

    async def close(self):
        """Close browser and clean up resources."""
        if self._page:
            await self._page.close()
        if self._context:
            await self._context.close()
        if self._browser:
            await self._browser.close()
        if self._playwright:
            await self._playwright.stop()
        self._page = None
        self._context = None
        self._browser = None
        self._playwright = None


_agent: Optional[JarvisWebAgent] = None


def get_agent(headless: bool = True) -> JarvisWebAgent:
    global _agent
    if _agent is None:
        _agent = JarvisWebAgent(headless=headless)
    return _agent


async def web_search(query: str) -> list[dict]:
    """Convenience: search the web and return results."""
    agent = get_agent()
    try:
        return await agent.search(query)
    except Exception as e:
        return [{"title": "Error", "url": "", "snippet": str(e)}]


async def open_url(url: str) -> str:
    """Convenience: navigate to URL and return title."""
    agent = get_agent(headless=False)
    return await agent.navigate(url)
