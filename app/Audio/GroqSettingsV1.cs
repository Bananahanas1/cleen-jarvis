using System.Text.Json;
using System.Text.Json.Serialization;

namespace JarvisClean;

internal sealed class GroqSettingsV1
{
    public const string ConfigFileName = "groq.json";
    public const string ApiKeyEnvironmentName = "GROQ_API_KEY";

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = "";

    [JsonPropertyName("model")]
    public string Model { get; init; } = "whisper-large-v3-turbo";

    public string ResolvedApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey)) return ApiKey;
        return Environment.GetEnvironmentVariable(ApiKeyEnvironmentName) ?? "";
    }

    public static GroqSettingsV1 Load(string configDirectory)
    {
        var path = Path.Combine(configDirectory, ConfigFileName);
        if (!File.Exists(path)) return new GroqSettingsV1();
        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<GroqSettingsV1>(json, JsonOptions);
            return loaded ?? new GroqSettingsV1();
        }
        catch
        {
            return new GroqSettingsV1();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
