const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const router = fs.readFileSync(path.join(root, "app", "CommandRouterV1.cs"), "utf8");
const program = fs.readFileSync(path.join(root, "app", "Program.cs"), "utf8");
const dashboard = fs.readFileSync(path.join(root, "dashboard", "index.html"), "utf8");
const csproj = fs.readFileSync(path.join(root, "app", "JarvisClean.csproj"), "utf8");
const voiceConfig = fs.readFileSync(path.join(root, "app", "Audio", "VoiceConfigV1.cs"), "utf8");
const voiceState = fs.readFileSync(path.join(root, "app", "Audio", "VoiceStateV1.cs"), "utf8");
const voiceHandler = fs.readFileSync(path.join(root, "app", "Audio", "VoiceCommandHandlerV1.cs"), "utf8");
const voiceTts = fs.readFileSync(path.join(root, "app", "Audio", "VoiceTtsServiceV1.cs"), "utf8");
const voiceStt = fs.readFileSync(path.join(root, "app", "Audio", "VoiceSttServiceV1.cs"), "utf8");
const voiceCapture = fs.readFileSync(path.join(root, "app", "Audio", "VoiceCaptureV1.cs"), "utf8");
const voiceHotkey = fs.readFileSync(path.join(root, "app", "Audio", "VoiceHotkeyV1.cs"), "utf8");
const voiceAssets = fs.readFileSync(path.join(root, "app", "Audio", "VoiceAssetManagerV1.cs"), "utf8");

let failures = 0;
function check(name, condition) {
  if (!condition) { failures += 1; console.log("FAIL " + name); }
  else { console.log("PASS " + name); }
}

// 1. NuGet packages added
check("csproj references NAudio", csproj.includes("NAudio"));
check("csproj references Whisper.net", csproj.includes("Whisper.net"));
check("csproj references Whisper.net.Runtime", csproj.includes("Whisper.net.Runtime"));

// 2. Router enum + parsing
const voiceIntents = ["VoiceStatus", "VoiceOn", "VoiceOff", "VoiceMute", "VoiceUnmute", "VoiceMicTest", "VoiceModelSet"];
check("Router defines all 7 Voice intents", voiceIntents.every(x => router.includes(x)));
check("Router has slash dispatch for /voice", router.includes('command == "voice"') || router.includes('command.StartsWith("voice ")'));
check("Router has NL pattern aktivera rost", router.includes("aktivera rost"));
check("Router has NL pattern tysta dig", router.includes("tysta dig"));
check("Router has NL pattern prata igen", router.includes("prata igen"));
check("Router has NL pattern mick test", router.includes("mick test"));
check("Router has NL pattern byt rostmodell", router.includes("byt rostmodell"));
check("Router builds CommandRisk.SafeUi for voice", router.includes("VoiceResult"));
check("Voice intents stay local (ShouldSendToOllama = false)",
  voiceIntents.every(x => !new RegExp("Intent = CommandIntent\\." + x + "[\\s\\S]{0,200}ShouldSendToOllama = true").test(router)));

// 3. Config schema
check("VoiceConfigV1 defines Default()", voiceConfig.includes("public static VoiceConfigV1 Default()"));
check("VoiceConfigV1 default sttModel small", voiceConfig.includes("SttModel: \"small\""));
check("VoiceConfigV1 default ttsVoice sv_SE-nst-medium", voiceConfig.includes("sv_SE-nst-medium"));
check("VoiceConfigV1 default Enabled false", voiceConfig.includes("Enabled: false"));
check("VoiceConfigV1 default pttHotkey Ctrl+Space", voiceConfig.includes("Ctrl+Space"));
check("VoiceConfigV1 env override JARVIS_VOICE_ENABLED", voiceConfig.includes("JARVIS_VOICE_ENABLED"));
check("VoiceConfigV1 env override JARVIS_VOICE_MODEL", voiceConfig.includes("JARVIS_VOICE_MODEL"));
check("VoiceConfigV1 env override JARVIS_VOICE_GPU", voiceConfig.includes("JARVIS_VOICE_GPU"));

// 4. State machine
check("VoiceStateV1 has 6 modes", ["Off", "Idle", "Listening", "Transcribing", "Sending", "Speaking"].every(x => voiceState.includes(x)));
check("VoiceStateV1 lock-guarded", voiceState.includes("private static readonly object Lock"));
check("VoiceStateV1 exposes ShouldSpeak", voiceState.includes("public static bool ShouldSpeak"));
check("VoiceStateV1 exposes OverviewLine", voiceState.includes("public static string OverviewLine"));

// 5. Handler dispatch in Program.cs
check("Program.cs dispatches Voice intents", program.includes("CommandIntent.VoiceStatus") && program.includes("VoiceCommandHandlerV1.Apply"));
check("Program.cs sends voice state in overview", program.includes("[\"voice\"] = VoiceStateV1.OverviewLine()"));
check("Program.cs handles voice_ptt_toggle message", program.includes('voice_ptt_toggle'));
check("Program.cs has HandleVoicePttToggleAsync", program.includes("HandleVoicePttToggleAsync"));
check("Program.cs hooks TTS into AddAssistantMessage", program.includes("VoiceTtsServiceV1.SpeakIfEnabled"));
check("Program.cs runs mic test via VoiceCaptureV1", program.includes("VoiceCaptureV1.RunMicTestAsync"));

// 6. Dashboard UI
check("Dashboard has micBtn", dashboard.includes('id="micBtn"'));
check("Dashboard has visualVoiceState cell", dashboard.includes('id="visualVoiceState"'));
check("Dashboard has micPulse listening indicator", dashboard.includes('id="micPulse"'));
check("Dashboard has Röst panel with voice quick-buttons",
  dashboard.includes('data-command="/voice"') &&
  dashboard.includes('data-command="/voice on"') &&
  dashboard.includes('data-command="/voice off"') &&
  dashboard.includes('data-command="/voice mute"') &&
  dashboard.includes('data-command="/voice mic test"'));
check("Dashboard mic button posts voice_ptt_press (hold-to-talk start)", dashboard.includes('"voice_ptt_press"'));
check("Dashboard mic button posts voice_ptt_release (hold-to-talk stop)", dashboard.includes('"voice_ptt_release"'));
check("Dashboard mic button uses mousedown/mouseup for hold-to-talk", dashboard.includes("mousedown") && dashboard.includes("mouseup") && dashboard.includes("mouseleave"));
check("Dashboard reads voice from overview", dashboard.includes("overview.voice") || dashboard.includes("overview.Voice"));

// 7. Safety: TTS never imports PendingApproval
check("VoiceTtsServiceV1 does NOT import PendingApprovalStoreV1", !voiceTts.includes("PendingApprovalStoreV1"));
check("VoiceTtsServiceV1 does NOT touch Bridges/Desktop namespaces",
  !voiceTts.includes("JarvisClean.Desktop") && !voiceTts.includes("BrowserAutopilotRunner") && !voiceTts.includes("DesktopAutopilotRunner"));
check("VoiceCommandHandlerV1 is pure (no _webView, no Process.Start)",
  !voiceHandler.includes("_webView") && !voiceHandler.includes("Process.Start"));
check("VoiceCommandHandlerV1 does NOT call PendingApprovalStoreV1.Set",
  !voiceHandler.includes("PendingApprovalStoreV1.Set"));
check("VoiceSttServiceV1 only calls Whisper.net",
  voiceStt.includes("Whisper.net") && !voiceStt.includes("Process.Start"));

// 8. Asset manager
check("VoiceAssetManagerV1 resolves whisper model path", voiceAssets.includes("WhisperModelPath") && voiceAssets.includes("ggml-"));
check("VoiceAssetManagerV1 resolves piper voice path", voiceAssets.includes("PiperVoicePath") && voiceAssets.includes(".onnx"));
check("VoiceAssetManagerV1 builds report with All/Stt/Tts ready flags",
  voiceAssets.includes("AllPresent") && voiceAssets.includes("SttReady") && voiceAssets.includes("TtsReady"));

// 9. Capture format
check("VoiceCaptureV1 uses 16kHz mono 16-bit", voiceCapture.includes("16000") && voiceCapture.includes("CaptureBitsPerSample = 16"));
check("VoiceCaptureV1 has mic-test", voiceCapture.includes("RunMicTestAsync"));

// 10. Hotkey
check("VoiceHotkeyV1 uses Ctrl modifier and Space VK", voiceHotkey.includes("VkSpace") && voiceHotkey.includes("0x20"));
check("VoiceHotkeyV1 has unique hotkey id (not collide with DesktopKillHotkeyIdV1)",
  voiceHotkey.includes("0x4A02") && !program.includes("DesktopKillHotkeyIdV1 = 0x4A02"));
check("VoiceHotkeyV1 sets MOD_NOREPEAT (stops Ctrl+Space auto-repeat spam)",
  voiceHotkey.includes("ModNoRepeat") && voiceHotkey.includes("0x4000") &&
  /RegisterHotKey\([^)]*ModControl\s*\|\s*ModNoRepeat[^)]*\)/.test(voiceHotkey));

// 11. Bug fixes 2026-05-13
check("VoiceCaptureV1 exposes async StopAndGetPcmAsync", voiceCapture.includes("StopAndGetPcmAsync"));
check("VoiceCaptureV1 syncs RecordingStopped via TaskCompletionSource",
  voiceCapture.includes("TaskCompletionSource") && voiceCapture.includes("RecordingStopped"));
check("VoiceCaptureV1 guards DataAvailable with _writingActive flag",
  voiceCapture.includes("_writingActive"));
check("Program.cs has voice_ptt_press handler", program.includes('voice_ptt_press') && program.includes("HandleVoicePttPressAsync"));
check("Program.cs has voice_ptt_release handler", program.includes('voice_ptt_release') && program.includes("HandleVoicePttReleaseAsync"));
check("VoiceCommandHandlerV1 inlines asset status in /voice on response",
  voiceHandler.includes("BuildReport") && voiceHandler.includes("MissingAssetsMessage"));
check("VoiceCommandHandlerV1 does NOT suggest non-existent 'voice assets' command",
  !voiceHandler.includes("voice assets") && !voiceHandler.includes("'voice assets'"));

// 12. Bug fixes 2026-05-14
check("VoiceTtsServiceV1 uses --output_file (file-based, more robust than --output-raw streaming)",
  voiceTts.includes("--output_file"));
check("VoiceTtsServiceV1 plays via WaveFileReader (not BufferedWaveProvider streaming)",
  voiceTts.includes("WaveFileReader") && !voiceTts.includes("BufferedWaveProvider"));
check("VoiceTtsServiceV1 logs to data/voice/tts-debug.log for debugging",
  voiceTts.includes("tts-debug.log") && voiceTts.includes("private static void Log"));
check("VoiceTtsServiceV1 logs exception messages (no silent swallow of crashes)",
  /catch \(Exception ex\)[\s\S]{0,200}Log\(/.test(voiceTts));
check("Program.cs fills chat-input on STT release (no auto-route)",
  program.includes("jarvisFillChatInputV1") && /HandleVoicePttReleaseAsync[\s\S]{0,4000}jarvisFillChatInputV1/.test(program));
check("Program.cs no longer auto-routes STT transcript to CommandRouter",
  !/HandleVoicePttReleaseAsync[\s\S]{0,4000}TryHandleCommandRouterV1UiAsync\(sttResult\.Transcript\)/.test(program));
check("Dashboard exposes jarvisFillChatInputV1",
  dashboard.includes("jarvisFillChatInputV1") && dashboard.includes('document.getElementById("input")'));

if (failures > 0) {
  console.log("\n" + failures + " voice-io V1 failure(s)");
  process.exit(1);
}

console.log("\nVoice I/O Layer V1 checks passed.");
