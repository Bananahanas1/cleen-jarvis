import io, os, shutil, subprocess, tempfile, warnings, zipfile
from pathlib import Path

try:
    import requests
    _requests_ok = True
except ImportError:
    _requests_ok = False

_DIR = Path(__file__).parent / "models" / "tts"
_PIPER = _DIR / "piper" / "piper.exe"
_VOICE = "en_GB-northern_english_male-medium"
_MODEL = _DIR / f"{_VOICE}.onnx"
_CFG = _DIR / f"{_VOICE}.onnx.json"
_PIPER_URL = "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip"
_MODEL_URL = f"https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_GB/northern_english_male/medium/{_VOICE}.onnx"
_SV = str.maketrans("åäöÅÄÖ", "aoooAO")

def _dl(url, dest):
    dest.parent.mkdir(parents=True, exist_ok=True)
    r = requests.get(url, stream=True, timeout=60)
    r.raise_for_status()
    with open(dest, "wb") as f:
        for chunk in r.iter_content(65536): f.write(chunk)

class JarvisTTS:
    def __init__(self): self.ready = False
    def initialize(self) -> bool:
        if not _requests_ok: return False
        try:
            _DIR.mkdir(parents=True, exist_ok=True)
            if not _PIPER.exists():
                tmp = Path(tempfile.mktemp(suffix=".zip"))
                _dl(_PIPER_URL, tmp)
                with zipfile.ZipFile(tmp) as z: z.extractall(_DIR / "piper")
                tmp.unlink(missing_ok=True)
            if not _MODEL.exists(): _dl(_MODEL_URL, _MODEL)
            if not _CFG.exists(): _dl(_MODEL_URL + ".json", _CFG)
            self.ready = True; return True
        except Exception as e:
            warnings.warn(f"JarvisTTS init: {e}"); return False
    def synthesize(self, text: str) -> bytes:
        if not self.ready: return b""
        r = subprocess.run([str(_PIPER), "--model", str(_MODEL), "--output-raw"],
                           input=text.translate(_SV).encode("utf-8"), capture_output=True)
        return _to_wav(r.stdout)

def _to_wav(pcm, sr=22050):
    import struct
    h = struct.pack("<4sI4s4sIHHIIHH4sI", b"RIFF", 36+len(pcm), b"WAVE",
                    b"fmt ", 16, 1, 1, sr, sr*2, 2, 16, b"data", len(pcm))
    return h + pcm

def speak(text: str) -> bytes:
    t = JarvisTTS()
    return t.synthesize(text) if t.initialize() else b""

def speak_to_file(text: str, path: str):
    w = speak(text)
    if w: Path(path).write_bytes(w)
