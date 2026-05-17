using System.Text.Json;
using System.Text.Json.Serialization;

namespace JarvisClean;

internal sealed class ElevenLabsSettingsV1
{
    public const string ConfigFileName = "elevenlabs.json";
    public const string ApiKeyEnvironmentName = "ELEVENLABS_API_KEY";

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = "";

    [JsonPropertyName("voiceId")]
    public string VoiceId { get; init; } = "";

    [JsonPropertyName("modelId")]
    public string ModelId { get; init; } = "eleven_multilingual_v2";

    [JsonPropertyName("stability")]
    public double Stability { get; init; } = 0.5;

    [JsonPropertyName("similarityBoost")]
    public double SimilarityBoost { get; init; } = 0.75;

    [JsonPropertyName("style")]
    public double Style { get; init; } = 0.0;

    [JsonPropertyName("useSpeakerBoost")]
    public bool UseSpeakerBoost { get; init; } = true;

    public bool IsReady => !string.IsNullOrWhiteSpace(ResolvedApiKey()) && !string.IsNullOrWhiteSpace(VoiceId);

    public string ResolvedApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey)) return ApiKey;
        return Environment.GetEnvironmentVariable(ApiKeyEnvironmentName) ?? "";
    }

    public static ElevenLabsSettingsV1 Load(string configDirectory)
    {
        var path = Path.Combine(configDirectory, ConfigFileName);
        if (!File.Exists(path))
            return new ElevenLabsSettingsV1();

        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<ElevenLabsSettingsV1>(json, JsonOptions);
            return loaded ?? new ElevenLabsSettingsV1();
        }
        catch
        {
            return new ElevenLabsSettingsV1();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
