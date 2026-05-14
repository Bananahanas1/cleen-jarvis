from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ENV_PATH = ROOT / ".env"


def _load_env_file(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    if not path.exists():
        return values

    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        key = key.strip()
        value = value.strip().strip('"').strip("'")
        if key:
            values[key] = value
    return values


_FILE_ENV = _load_env_file(ENV_PATH)


def env_value(key: str, default: str = "") -> str:
    value = os.environ.get(key)
    if value is not None:
        return value
    return _FILE_ENV.get(key, default)


@dataclass(frozen=True)
class ProviderConfig:
    name: str
    env_key: str
    role: str
    optional_model_key: str = ""
    default_model: str = ""

    def status(self) -> dict[str, object]:
        model_key = self.optional_model_key
        model = env_value(model_key, self.default_model) if model_key else self.default_model
        return {
            "name": self.name,
            "role": self.role,
            "configured": bool(env_value(self.env_key)),
            "envKey": self.env_key,
            "model": model,
        }


PROVIDERS = [
    ProviderConfig("Claude", "ANTHROPIC_API_KEY", "brain", "ANTHROPIC_MODEL", "claude-3-5-sonnet-latest"),
    ProviderConfig("Groq Whisper", "GROQ_API_KEY", "speech-to-text", "GROQ_WHISPER_MODEL", "whisper-large-v3-turbo"),
    ProviderConfig("ElevenLabs", "ELEVENLABS_API_KEY", "text-to-speech"),
    ProviderConfig("fal.ai", "FAL_KEY", "brand-dna-visuals"),
]


@dataclass(frozen=True)
class AmySettings:
    host: str
    backend_port: int
    frontend_port: int
    database_path: Path
    browser_autorun: bool


def get_settings() -> AmySettings:
    database = Path(env_value("AMY_DATABASE_PATH", "data/amy.sqlite3"))
    if not database.is_absolute():
        database = ROOT / database
    return AmySettings(
        host=env_value("AMY_HOST", "127.0.0.1"),
        backend_port=int(env_value("AMY_BACKEND_PORT", "8787")),
        frontend_port=int(env_value("AMY_FRONTEND_PORT", "5177")),
        database_path=database,
        browser_autorun=env_value("AMY_BROWSER_AUTORUN", "0") == "1",
    )
