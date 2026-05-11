import json
import time
import warnings
from pathlib import Path

try:
    import requests
    _requests_ok = True
except ImportError:
    _requests_ok = False
    warnings.warn("requests not installed — jarvis_weather unavailable")

_CACHE = {"data": None, "ts": 0}
_CACHE_TTL = 900

_WMO_SV = {
    0: "Klart", 1: "Mestadels klart", 2: "Delvis molnigt", 3: "Mulet",
    45: "Dimma", 48: "Isande dimma",
    51: "Lätt duggregn", 53: "Duggregn", 55: "Kraftigt duggregn",
    61: "Lätt regn", 63: "Regn", 65: "Kraftigt regn",
    71: "Lätt snö", 73: "Snö", 75: "Kraftigt snöfall", 77: "Snökorn",
    80: "Lätt skur", 81: "Skurar", 82: "Kraftiga skurar",
    85: "Snöskurar", 86: "Kraftiga snöskurar",
    95: "Åska", 96: "Åska med hagel", 99: "Kraftig åska med hagel",
}

def _load_coords():
    p = Path(__file__).parent / "settings.json"
    if p.exists():
        try:
            s = json.loads(p.read_text(encoding="utf-8"))
            return s.get("weather",{}).get("latitude",59.3293), s.get("weather",{}).get("longitude",18.0686)
        except Exception:
            pass
    return 59.3293, 18.0686

def get_weather() -> dict:
    if not _requests_ok:
        return {}
    now = time.time()
    if _CACHE["data"] and (now - _CACHE["ts"]) < _CACHE_TTL:
        return _CACHE["data"]
    lat, lon = _load_coords()
    try:
        r = requests.get("https://api.open-meteo.com/v1/forecast", params={
            "latitude": lat, "longitude": lon,
            "current": "temperature_2m,weather_code,is_day",
            "hourly": "temperature_2m,weather_code",
            "temperature_unit": "celsius", "timezone": "auto", "forecast_days": 1,
        }, timeout=5)
        r.raise_for_status()
        raw = r.json()
    except Exception:
        return _CACHE["data"] or {}
    cur = raw.get("current", {})
    code = cur.get("weather_code", 0)
    hourly = raw.get("hourly", {})
    times = hourly.get("time", [])
    temps = hourly.get("temperature_2m", [])
    codes = hourly.get("weather_code", [])
    from datetime import datetime
    now_h = datetime.now().hour
    forecast = []
    for i, t in enumerate(times[:24]):
        h = int(t[11:13]) if len(t) >= 13 else i
        if h >= now_h and len(forecast) < 6:
            forecast.append({"time": t[11:16], "temp": temps[i] if i < len(temps) else None,
                              "condition": _WMO_SV.get(codes[i] if i < len(codes) else 0, "Okänt")})
    result = {"temp": cur.get("temperature_2m"), "weather_code": code,
               "condition": _WMO_SV.get(code, "Okänt"), "is_day": bool(cur.get("is_day", 1)),
               "hourly_forecast": forecast}
    _CACHE["data"] = result; _CACHE["ts"] = now
    return result

def get_weather_summary() -> str:
    w = get_weather()
    return f"{w['temp']:.0f}°C, {w['condition']}" if w else "Väder ej tillgängligt"
