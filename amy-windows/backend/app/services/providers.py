from __future__ import annotations

from ..config import PROVIDERS, env_value


def provider_status() -> list[dict[str, object]]:
    return [provider.status() for provider in PROVIDERS]


def setup_status() -> dict[str, object]:
    providers = provider_status()
    ready = [p["name"] for p in providers if p["configured"]]
    missing = [p["name"] for p in providers if not p["configured"]]
    return {
        "providers": providers,
        "ready": ready,
        "missing": missing,
        "cloudReady": bool(ready),
    }


async def ask_claude(prompt: str, context: str = "") -> dict[str, object]:
    api_key = env_value("ANTHROPIC_API_KEY")
    model = env_value("ANTHROPIC_MODEL", "claude-3-5-sonnet-latest")
    if not api_key:
        return {
            "provider": "Claude",
            "configured": False,
            "text": "Claude is not configured yet. Add ANTHROPIC_API_KEY in amy-windows/.env.",
        }

    try:
        from anthropic import AsyncAnthropic

        client = AsyncAnthropic(api_key=api_key)
        message = await client.messages.create(
            model=model,
            max_tokens=700,
            messages=[
                {
                    "role": "user",
                    "content": (context + "\n\n" + prompt).strip(),
                }
            ],
        )
        text_parts = [part.text for part in message.content if getattr(part, "type", "") == "text"]
        return {
            "provider": "Claude",
            "configured": True,
            "model": model,
            "text": "\n".join(text_parts).strip(),
        }
    except Exception as exc:  # Provider errors must not crash local Amy.
        return {
            "provider": "Claude",
            "configured": True,
            "model": model,
            "text": "Claude adapter error: " + exc.__class__.__name__,
        }
