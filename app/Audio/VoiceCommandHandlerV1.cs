namespace JarvisClean;

internal static class VoiceCommandHandlerV1
{
    public static string Apply(CommandResult command, string configDirectory)
    {
        var config = VoiceConfigV1.Load(configDirectory);

        return command.Intent switch
        {
            CommandIntent.VoiceStatus => RenderStatus(config),
            CommandIntent.VoiceOn => SetEnabled(true, config, configDirectory),
            CommandIntent.VoiceOff => SetEnabled(false, config, configDirectory),
            CommandIntent.VoiceMute => SetMuted(true, config, configDirectory),
            CommandIntent.VoiceUnmute => SetMuted(false, config, configDirectory),
            CommandIntent.VoiceMicTest => MicTestPlaceholder(),
            CommandIntent.VoiceModelSet => SetModel(command, config, configDirectory),
            CommandIntent.VoiceListVoices => ListVoices(config),
            CommandIntent.VoiceSetVoice => SetVoice(command, config, configDirectory),
            _ => "Okänt röstkommando."
        };
    }

    private static string ListVoices(VoiceConfigV1 config)
    {
        var projectRoot = ResolveProjectRoot();
        var voices = VoiceAssetManagerV1.ListInstalledPiperVoices(projectRoot);
        if (voices.Count == 0)
        {
            return "Inga Piper-röster hittades i data/voice/piper/. " +
                   "Ladda en svensk röst från https://huggingface.co/rhasspy/piper-voices " +
                   "(t.ex. sv_SE-nst-medium.onnx + sv_SE-nst-medium.onnx.json) och spara där.";
        }
        var lines = voices.Select(v => "• " + v + (v == config.TtsVoice ? "  (aktiv)" : "")).ToList();
        return "Installerade Piper-röster:\n" + string.Join("\n", lines) +
               "\n\nByt röst med: byt röst till <namn>  eller  /voice rost <namn>.";
    }

    private static string SetVoice(CommandResult command, VoiceConfigV1 config, string configDirectory)
    {
        var voice = command.Arguments.TryGetValue("voice", out var raw) ? (raw ?? string.Empty).Trim() : "";
        if (string.IsNullOrWhiteSpace(voice))
            return "Ange en röst. Lista installerade med /voice roster.";

        var projectRoot = ResolveProjectRoot();
        var installed = VoiceAssetManagerV1.ListInstalledPiperVoices(projectRoot);
        if (installed.Count > 0 && !installed.Contains(voice, StringComparer.OrdinalIgnoreCase))
        {
            return "Röst '" + voice + "' är inte installerad. Installerade: " +
                   (installed.Count == 0 ? "(inga)" : string.Join(", ", installed)) +
                   ". Spara röstfilen (.onnx + .onnx.json) i data/voice/piper/ och försök igen.";
        }

        var updated = config with { TtsVoice = voice };
        VoiceConfigV1.Save(configDirectory, updated);
        VoiceStateV1.SetModelLabels(updated.SttModel, updated.TtsVoice);

        return "TTS-röst ändrad till: " + voice + ". Nästa Jarvis-svar talas med den nya rösten.";
    }

    public static string RenderStatus(VoiceConfigV1 config)
    {
        VoiceStateV1.SetModelLabels(config.SttModel, config.TtsVoice);
        var projectRoot = ResolveProjectRoot();
        var report = VoiceAssetManagerV1.BuildReport(projectRoot, config);

        return VoiceStateV1.Status() +
               "\n\n" +
               "Autospeak: " + (config.TtsAutoSpeak ? "på" : "av") + "\n" +
               "Språk: " + config.Language + "\n" +
               "PTT-hotkey: " + config.PttHotkey + " (toggle) eller håll mic-knappen 🎙 (hold-to-talk)" + "\n" +
               "Mic-enhet: " + (config.MicDeviceId < 0 ? "default" : config.MicDeviceId.ToString()) + "\n\n" +
               VoiceAssetManagerV1.MissingAssetsMessage(report);
    }

    private static string ResolveProjectRoot()
    {
        // Heuristik: ConfigDir = F:\Jarvis-clean\config, ProjectRoot = F:\Jarvis-clean
        var configDir = Path.Combine("F:\\Jarvis-clean", "config");
        return Path.GetDirectoryName(configDir) ?? "F:\\Jarvis-clean";
    }

    private static string SetEnabled(bool enabled, VoiceConfigV1 config, string configDirectory)
    {
        var updated = config with { Enabled = enabled };
        VoiceConfigV1.Save(configDirectory, updated);

        if (enabled)
        {
            VoiceStateV1.TransitionTo(VoiceModeV1.Idle);
            VoiceStateV1.SetModelLabels(updated.SttModel, updated.TtsVoice);
            var projectRoot = ResolveProjectRoot();
            var report = VoiceAssetManagerV1.BuildReport(projectRoot, updated);
            var assetsBlock = report.AllPresent
                ? "Alla röst-assets finns på plats. Klart att tala."
                : VoiceAssetManagerV1.MissingAssetsMessage(report);

            return "Röst aktiverad. Läge: " + VoiceStateV1.Label(VoiceModeV1.Idle) +
                   ". STT-modell: " + updated.SttModel +
                   ". TTS-röst: " + updated.TtsVoice +
                   ". Push-to-talk: " + updated.PttHotkey + " (toggle) eller håll mic-knappen 🎙 (hold-to-talk).\n\n" +
                   assetsBlock;
        }

        VoiceStateV1.TransitionTo(VoiceModeV1.Off);
        return "Röst avstängd. Inget röstinput tas emot och Jarvis talar inte.";
    }

    private static string SetMuted(bool muted, VoiceConfigV1 config, string configDirectory)
    {
        var updated = config with { Muted = muted };
        VoiceConfigV1.Save(configDirectory, updated);
        VoiceStateV1.SetMuted(muted);
        return muted
            ? "Jarvis är nu tyst. Säg 'prata igen' för att slå på TTS."
            : "Jarvis pratar igen. TTS slås på när Jarvis svarar.";
    }

    private static string MicTestPlaceholder()
    {
        // Den faktiska 2-sekundersinspelningen körs av Program.cs-dispatchern via VoiceCaptureV1.RunMicTestAsync.
        return "Startar mikrofontest: spelar in 2 sek och spelar upp. Resultat kommer strax.";
    }

    private static string SetModel(CommandResult command, VoiceConfigV1 config, string configDirectory)
    {
        var model = command.Arguments.TryGetValue("model", out var raw) ? (raw ?? string.Empty).Trim().ToLowerInvariant() : "";

        if (model is not ("small" or "medium"))
        {
            return "Okänd STT-modell. Tillåtna värden: small, medium. Du skrev: '" + model + "'.";
        }

        var updated = config with { SttModel = model };
        VoiceConfigV1.Save(configDirectory, updated);
        VoiceStateV1.SetModelLabels(updated.SttModel, updated.TtsVoice);

        return "STT-modell ändrad till: " + model + ".\n" +
               "Modellfilen ska ligga i data/voice/whisper/ggml-" + model + ".bin innan första STT-anrop.";
    }
}
