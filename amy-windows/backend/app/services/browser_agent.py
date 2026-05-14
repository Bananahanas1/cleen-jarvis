from __future__ import annotations

from dataclasses import dataclass

from ..config import get_settings


DENY_TERMS = (
    "login",
    "password",
    "credential",
    "payment",
    "checkout",
    "bank",
    "publish",
    "submit",
)


@dataclass(frozen=True)
class BrowserPlan:
    goal: str
    status: str
    needs_approval: bool
    steps: list[str]
    reason: str


def plan_browser_task(goal: str) -> BrowserPlan:
    normalized = goal.lower()
    blocked = [term for term in DENY_TERMS if term in normalized]
    if blocked:
        return BrowserPlan(
            goal=goal,
            status="blocked",
            needs_approval=True,
            steps=[],
            reason="Task includes sensitive browser terms: " + ", ".join(blocked),
        )

    steps = [
        "Open isolated Playwright Chromium profile",
        "Navigate/search for the requested target",
        "Read visible page state",
        "Report planned click/type actions before doing risky actions",
    ]
    return BrowserPlan(
        goal=goal,
        status="dry-run" if not get_settings().browser_autorun else "ready",
        needs_approval=True,
        steps=steps,
        reason="Browser autorun is disabled" if not get_settings().browser_autorun else "Browser autorun enabled",
    )


async def browser_status() -> dict[str, object]:
    return {
        "engine": "playwright",
        "autorun": get_settings().browser_autorun,
        "denyTerms": DENY_TERMS,
        "mode": "approval-first",
    }
