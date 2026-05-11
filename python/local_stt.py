import io, warnings
from pathlib import Path

class JarvisSTT:
    def __init__(self, model="base", language="sv"):
        self.model_name = model; self.language = language
        self._model = None; self.ready = False
    def initialize(self) -> bool:
        try:
            from faster_whisper import WhisperModel
            self._model = WhisperModel(self.model_name, device="auto", compute_type="int8")
            self.ready = True; return True
        except ImportError:
            warnings.warn("faster-whisper not installed — local STT unavailable"); return False
        except Exception as e:
            warnings.warn(f"JarvisSTT: {e}"); return False
    def transcribe(self, audio) -> str | None:
        if not self.ready: return None
        try:
            if isinstance(audio, bytes): audio = io.BytesIO(audio)
            elif isinstance(audio, (str, Path)): audio = str(audio)
            segs, _ = self._model.transcribe(audio, language=self.language)
            return " ".join(s.text.strip() for s in segs).strip()
        except Exception as e:
            warnings.warn(f"STT: {e}"); return None

def transcribe_file(path: str, language="sv") -> str | None:
    s = JarvisSTT(language=language)
    return s.transcribe(path) if s.initialize() else None

def transcribe_bytes(audio: bytes, language="sv") -> str | None:
    s = JarvisSTT(language=language)
    return s.transcribe(audio) if s.initialize() else None
