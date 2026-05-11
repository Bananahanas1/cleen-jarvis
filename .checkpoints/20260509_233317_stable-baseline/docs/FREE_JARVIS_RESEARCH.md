# FREE_JARVIS_RESEARCH.md

Senast uppdaterad: 2026-05-06

## 1. Summary

`F:\Free Jarvis` appears to be a bundled Python desktop assistant called `ProjectPixel`.

Confidence: medium-high.

Important evidence:

- Top-level app executable: `ProjectPixel.exe`.
- Executable SHA256 observed: `C5707B2F5A439A08A624B63A32034EF54A3710FD749A0D8081E670CAB4170555`.
- Executable signature status observed through static metadata: `NotSigned`.
- `_internal` contains `python311.dll`, `python3.dll`, `base_library.zip`, many `.pyd` extension modules and Python packages.
- `_internal` includes audio/speech packages such as `speech_recognition`, `pyaudio`, `pocketsphinx` and PortAudio binaries.
- Top-level `.tts_cache` contains generated `.mp3` files, which strongly suggests text-to-speech output with caching.
- Top-level `.env` contains variable names for `GROQ_API_KEY`, `OPENWEATHER_API_KEY` and `CITY`.
- `_internal` includes `googleapiclient` plus many Google API discovery JSON files, including speech/text-to-speech related discovery files.

No app-specific readable source files were found outside the bundled runtime. The only `.py` files found were IPython dependency extension/test files. This means useful information should be treated as architecture clues, not reusable application code.

## 2. Safety notes

- `ProjectPixel.exe` is not digitally signed.
- Do not run `ProjectPixel.exe` as admin.
- Do not run `ProjectPixel.exe` during Jarvis-clean development unless the user explicitly asks for sandboxed testing later.
- Treat `.env` as sensitive. Only variable names were inspected; values were not copied.
- Keep `F:\Free Jarvis` reference-only unless license and ownership are clear.
- Do not copy direct code if ownership/license is unclear.
- Do not copy secrets, API keys, tokens, passwords, private keys or user data.
- Do not copy uncontrolled action routing into Jarvis-clean.
- Any later feature inspired by Free Jarvis must go through `CommandRouterV1`, `CommandValidatorV1`, `ToolRegistryV1` and `PendingApprovalV1`.

## 3. File structure

Top-level structure observed:

```text
F:\Free Jarvis
  .env
  .tts_cache
  ProjectPixel.exe
  _internal
```

Important items:

- `ProjectPixel.exe`: main bundled executable, unsigned and treated as untrusted.
- `.env`: contains variable names for Groq, OpenWeather and a city setting. Values were not inspected or copied.
- `.tts_cache`: contains many `.mp3` files with hash-like names. Likely cached TTS output.
- `_internal`: bundled Python runtime/dependencies.

Important `_internal` evidence:

- Python runtime: `python311.dll`, `python3.dll`, `base_library.zip`.
- Native modules: many `.pyd` files such as `_socket.pyd`, `_ssl.pyd`, `_sqlite3.pyd`, `_tkinter.pyd`.
- GUI/runtime data: `_tcl_data`, `_tk_data`, `tcl86t.dll`, `tk86t.dll`.
- Audio: `_sounddevice_data`, `pyaudio`, `pocketsphinx`, `speech_recognition`.
- API/networking: `aiohttp`, `httplib2`, `googleapiclient`, `tornado`, `gevent`.
- Data/science/UI-adjacent packages: `numpy`, `matplotlib`, `PIL`, `IPython`, `jedi`, `nbformat`.
- Security/crypto: `cryptography`, `Crypto`, `bcrypt`, `libcrypto-3.dll`, `libssl-3.dll`.
- Storage: `_sqlite3.pyd`, `sqlite3.dll`.

Readable files found:

- `.env` at top level, inspected only as redacted variable names.
- No readable `.py`, `.json`, `.txt`, `.md`, `.ini`, `.toml`, `.yaml`, `.yml` or `.log` files outside `_internal` except `.env`.
- `_internal` contains dependency metadata, license/readme files, IPython dependency `.py` files and Google API discovery JSON cache.
- Most JSON files are Google API discovery documents rather than app config.

## 4. Dependency analysis

### AI/LLM

Evidence:

- `.env` variable name: `GROQ_API_KEY`.
- Network/client libraries: `aiohttp`, `httplib2`, `googleapiclient`.
- Data validation package: `pydantic`.

Likely meaning:

- The app may call Groq or another hosted LLM API.
- No local Ollama-style model directory was observed.
- No clear `groq` package directory was visible, so the app may call Groq through raw HTTP or a bundled/hidden module.

### Speech-to-text

Evidence:

- `speech_recognition`.
- `pocketsphinx`.
- `pyaudio`.
- PortAudio binaries in `_sounddevice_data`.

Likely meaning:

- The app likely supports microphone voice input.
- It may support offline speech recognition through PocketSphinx, online recognition through APIs, or both.

### Text-to-speech

Evidence:

- `.tts_cache` with many `.mp3` files.
- Google discovery documents include `texttospeech`.
- Audio output/runtime packages are present.

Likely meaning:

- The app likely speaks responses and caches generated speech audio.
- Exact TTS provider is uncertain from static evidence alone.

### GUI

Evidence:

- `_tkinter.pyd`, `_tcl_data`, `_tk_data`, `tcl86t.dll`, `tk86t.dll`.
- `matplotlib`, `PIL`.

Likely meaning:

- The app may use Tkinter or a Tk-backed UI.
- It may display images or simple visual output.

### Web/server

Evidence:

- `aiohttp`, `tornado`, `gevent`, `httplib2`.

Likely meaning:

- The app may perform async HTTP calls or run a small local server/event loop.
- No server behavior was executed or confirmed.

### Google/API integrations

Evidence:

- `googleapiclient`.
- `google-api-core`.
- Google discovery cache documents, including many Google service descriptors.

Likely meaning:

- The bundle can call Google APIs.
- Presence of discovery documents does not prove every Google service is used.

### Crypto/security

Evidence:

- `cryptography`.
- `Crypto`.
- `bcrypt`.
- OpenSSL DLLs.

Likely meaning:

- The app or dependencies can handle HTTPS, hashing, encryption or authentication.
- Do not infer strong security design from package presence alone.

### Database/storage

Evidence:

- `_sqlite3.pyd`.
- `sqlite3.dll`.

Likely meaning:

- The app may store state locally in SQLite, though no app database file was observed at top level.

### Audio

Evidence:

- `pyaudio`.
- `_sounddevice_data`.
- PortAudio binaries.
- `.mp3` TTS cache.

Likely meaning:

- The app likely has voice input/output support.

### Networking

Evidence:

- `_socket.pyd`, `_ssl.pyd`.
- `aiohttp`, `httplib2`, `tornado`, `gevent`.
- `.env` API key variable names.

Likely meaning:

- The app likely depends on internet/API calls for at least some features.

### Packaging/runtime

Evidence:

- `_internal` folder.
- `python311.dll`.
- `python3.dll`.
- `base_library.zip`.
- many `.pyd` files.

Likely meaning:

- The app appears to be a Python one-folder bundle, likely PyInstaller-style.
- I did not decompile bytecode or inspect proprietary app internals.

## 5. Feature guesses

Based only on static evidence, Free Jarvis may support:

- voice input through microphone
- speech recognition through `speech_recognition`, `pyaudio` and possibly PocketSphinx
- TTS voice output with `.mp3` caching
- hosted LLM calls, likely Groq, because `.env` contains `GROQ_API_KEY`
- weather lookup, because `.env` contains `OPENWEATHER_API_KEY` and `CITY`
- Google API integration, though exact services are uncertain
- a desktop GUI, likely Python/Tkinter based
- local cache/storage
- network/API-backed assistant behavior

Uncertain:

- Whether it has safe local command routing.
- Whether it can control files, terminal, browser or desktop.
- Whether it has approval prompts for risky actions.
- Whether it has local memory, task workspaces or project-aware coding features.

## 6. Useful ideas for Jarvis-clean

Ideas worth rebuilding Jarvis-native:

- Voice Mode later, but only after command safety is stable.
- TTS/STT cache pattern for faster repeated spoken responses.
- Clear `.env` handling: variable names may be inspected, values must stay out of chat/logs/memory.
- A provider abstraction for voice/LLM/weather APIs.
- Audio input/output as another UI layer above the same safe command router.
- Weather/API tools only after InternetProbe and explicit tool policies exist.
- Optional voice feedback for status, build result and approval-needed notices.
- Offline-friendly packaging notes: bundled runtimes can work, but Jarvis-clean should keep dependencies transparent.
- TTS cache cleanup policy so audio files do not grow forever.

Jarvis-native rule:

Voice and API features should not become a second control system. They must feed the same `CommandRouterV1 -> CommandValidatorV1 -> ToolRegistryV1 -> PendingApprovalV1` path.

## 7. What NOT to copy

Do not copy:

- unclear-license application code
- secrets
- `.env` values
- API keys, tokens, passwords or private keys
- direct file execution patterns
- direct terminal execution patterns
- uncontrolled file write/delete behavior
- any routing that bypasses `PendingApprovalV1`
- unknown executable behavior
- dependency bundles wholesale
- UI behavior that hides risky actions from the user

## 8. Recommended Jarvis-native implementation plan

### Phase A: Research/documentation only

Status: this file.

Keep Free Jarvis as static reference. Do not run `ProjectPixel.exe`.

### Phase B: Voice mode design

Create `docs/VOICE_MODE_PLAN.md`.

Define:

- voice input scope
- voice output scope
- allowed providers
- where cache lives
- what gets logged
- what never gets logged

### Phase C: TTS/STT provider selection

Choose providers explicitly:

- offline STT option
- online STT option
- offline/local TTS option if practical
- online TTS option if user approves

All provider keys must be redacted from logs and memory.

### Phase D: Safe voice command routing through CommandRouterV1

Voice transcript becomes normal Jarvis input.

Examples:

- user says "visa terminal"
- speech-to-text produces text
- text goes through CommandRouter/Validator
- local command runs or normal chat goes to LLM

### Phase E: Voice approvals must use PendingApprovalV1

Risky voice actions must still show pending preview and require confirmation.

Examples:

- "skriv fil ..."
- "radera ..."
- "kör build"
- "öppna program"
- future desktop/browser actions

Voice must never bypass approvals.

### Phase F: Optional UI integration later

Add a visible Voice Mode toggle later:

- off by default
- shows listening state
- shows transcript before action
- allows quick cancel
- never reads secrets aloud

## 9. Concrete TODOs for Jarvis-clean

Prioritized TODOs:

- Create `docs/VOICE_MODE_PLAN.md` later.
- Add voice mode only after command safety and developer workspace are stable.
- Voice input should map to safe intents.
- Voice output should not read secrets aloud.
- `.env` values must never enter chat, logs or memory.
- Free Jarvis remains reference-only.
- Add TTS cache cleanup design before adding generated audio.
- Add provider config docs with redaction rules.
- Keep weather/API tools behind InternetProbe and explicit user opt-in.

## 10. Open questions for user

- Where did `F:\Free Jarvis` come from?
- Do you have permission/license to reuse its code?
- Do you want voice mode in Jarvis-clean?
- Should Free Jarvis remain reference-only?
- Should we test `ProjectPixel.exe` later in Windows Sandbox?
