using System.Text.Json;

namespace JarvisClean;

internal sealed record VoiceConfigV1(
    bool Enabled,
    string Language,
    string SttModel,
    string TtsVoice,
    string PttHotkey,
    bool TtsAutoSpeak,
    int MicDeviceId,
    bool Muted)
{
    public const string ConfigFileName = "voice.json";
    public const string EnabledEnvironmentName = "JARVIS_VOICE_ENABLED";
    public const string ModelEnvironmentName = "JARVIS_VOICE_MODEL";
    public const string GpuEnvironmentName = "JARVIS_VOICE_GPU";

    public static VoiceConfigV1 Default()
    {
        return new VoiceConfigV1(
            Enabled: false,
            Language: "sv",
            SttModel: "small",
            TtsVoice: "sv_SE-nst-medium",
            PttHotkey: "Ctrl+Space",
            TtsAutoSpeak: true,
            MicDeviceId: -1,
            Muted: false);
    }

    public static VoiceConfigV1 Load(string configDirectory)
    {
        var path = Path.Combine(configDirectory, ConfigFileName);
        var current = Default();

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<VoiceConfigV1>(json, JsonOptions);
                if (loaded is not null)
                    current = loaded;
            }
            catch
            {
                // Korrupt config -> falla tillbaka till defaults; tystnad i V1 (logg sker av anroparen).
            }
        }

        return ApplyEnvironmentOverrides(current);
    }

    public static void Save(string configDirectory, VoiceConfigV1 config)
    {
        Directory.CreateDirectory(configDirectory);
        var path = Path.Combine(configDirectory, ConfigFileName);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static bool IsGpuEnabled()
    {
        var raw = Environment.GetEnvironmentVariable(GpuEnvironmentName);
        return raw is "1" or "true" or "TRUE" or "yes";
    }

    private static VoiceConfigV1 ApplyEnvironmentOverrides(VoiceConfigV1 baseConfig)
    {
        var enabled = baseConfig.Enabled;
        var sttModel = baseConfig.SttModel;

        var enabledRaw = Environment.GetEnvironmentVariable(EnabledEnvironmentName);
        if (!string.IsNullOrWhiteSpace(enabledRaw))
            enabled = enabledRaw is "1" or "true" or "TRUE" or "yes";

        var modelRaw = Environment.GetEnvironmentVariable(ModelEnvironmentName);
        if (!string.IsNullOrWhiteSpace(modelRaw))
            sttModel = modelRaw.Trim();

        return baseConfig with { Enabled = enabled, SttModel = sttModel };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
