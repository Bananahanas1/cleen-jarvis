namespace JarvisClean;

internal static class VoiceAssetManagerV1
{
    public const string WhisperFolder = "whisper";
    public const string PiperFolder = "piper";
    public const string PiperExecutableName = "piper.exe";

    public static string WhisperModelPath(string projectRoot, string sttModel)
    {
        var name = (sttModel ?? "small").Trim().ToLowerInvariant();
        if (name is not ("small" or "medium" or "tiny" or "base" or "large"))
            name = "small";
        return Path.Combine(projectRoot, "data", "voice", WhisperFolder, $"ggml-{name}.bin");
    }

    public static string PiperVoicePath(string projectRoot, string ttsVoice)
    {
        var voice = string.IsNullOrWhiteSpace(ttsVoice) ? "sv_SE-nst-medium" : ttsVoice.Trim();
        return Path.Combine(projectRoot, "data", "voice", PiperFolder, voice + ".onnx");
    }

    public static string PiperVoiceConfigPath(string projectRoot, string ttsVoice)
    {
        var voice = string.IsNullOrWhiteSpace(ttsVoice) ? "sv_SE-nst-medium" : ttsVoice.Trim();
        return Path.Combine(projectRoot, "data", "voice", PiperFolder, voice + ".onnx.json");
    }

    public static string PiperExecutablePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "data", "voice", PiperFolder, PiperExecutableName);
    }

    public static VoiceAssetReportV1 BuildReport(string projectRoot, VoiceConfigV1 config)
    {
        var whisperPath = WhisperModelPath(projectRoot, config.SttModel);
        var piperVoice = PiperVoicePath(projectRoot, config.TtsVoice);
        var piperVoiceConfig = PiperVoiceConfigPath(projectRoot, config.TtsVoice);
        var piperExe = PiperExecutablePath(projectRoot);

        return new VoiceAssetReportV1(
            WhisperModelPath: whisperPath,
            WhisperModelExists: File.Exists(whisperPath),
            PiperVoicePath: piperVoice,
            PiperVoiceExists: File.Exists(piperVoice),
            PiperVoiceConfigPath: piperVoiceConfig,
            PiperVoiceConfigExists: File.Exists(piperVoiceConfig),
            PiperExecutablePath: piperExe,
            PiperExecutableExists: File.Exists(piperExe));
    }

    public static List<string> ListInstalledPiperVoices(string projectRoot)
    {
        var dir = Path.Combine(projectRoot, "data", "voice", PiperFolder);
        if (!Directory.Exists(dir)) return new List<string>();
        var voices = new List<string>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(dir, "*.onnx"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(name)) continue;
                // En röst räknas som installerad om både .onnx och .onnx.json finns.
                if (File.Exists(Path.Combine(dir, name + ".onnx.json")))
                    voices.Add(name);
            }
        }
        catch { }
        voices.Sort(StringComparer.OrdinalIgnoreCase);
        return voices;
    }

    public static List<string> ListInstalledWhisperModels(string projectRoot)
    {
        var dir = Path.Combine(projectRoot, "data", "voice", WhisperFolder);
        if (!Directory.Exists(dir)) return new List<string>();
        var models = new List<string>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(dir, "ggml-*.bin"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (name is { Length: > 5 } && name.StartsWith("ggml-", StringComparison.Ordinal))
                    models.Add(name.Substring(5));
            }
        }
        catch { }
        models.Sort(StringComparer.OrdinalIgnoreCase);
        return models;
    }

    public static string MissingAssetsMessage(VoiceAssetReportV1 report)
    {
        var missing = new List<string>();

        if (!report.WhisperModelExists)
            missing.Add("• STT-modell saknas: " + report.WhisperModelPath +
                        "\n  Ladda ner från https://huggingface.co/ggerganov/whisper.cpp och spara där.");

        if (!report.PiperExecutableExists)
            missing.Add("• Piper-binär saknas: " + report.PiperExecutablePath +
                        "\n  Ladda Piper Windows-release från https://github.com/rhasspy/piper/releases och packa upp där.");

        if (!report.PiperVoiceExists)
            missing.Add("• Piper-röst saknas: " + report.PiperVoicePath +
                        "\n  Ladda svensk röst (sv_SE-nst-medium.onnx + .onnx.json) från https://huggingface.co/rhasspy/piper-voices");

        if (!report.PiperVoiceConfigExists)
            missing.Add("• Piper-röstkonfig saknas: " + report.PiperVoiceConfigPath);

        if (missing.Count == 0)
            return "Alla röst-assets finns på plats.";

        return "Röst-assets saknas:\n" + string.Join("\n", missing);
    }
}

internal sealed record VoiceAssetReportV1(
    string WhisperModelPath,
    bool WhisperModelExists,
    string PiperVoicePath,
    bool PiperVoiceExists,
    string PiperVoiceConfigPath,
    bool PiperVoiceConfigExists,
    string PiperExecutablePath,
    bool PiperExecutableExists)
{
    public bool AllPresent => WhisperModelExists && PiperVoiceExists && PiperVoiceConfigExists && PiperExecutableExists;
    public bool SttReady => WhisperModelExists;
    public bool TtsReady => PiperVoiceExists && PiperVoiceConfigExists && PiperExecutableExists;
}
