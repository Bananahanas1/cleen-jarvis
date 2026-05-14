using System.Text.Json;

namespace JarvisClean;

internal sealed record SceneCardV1(string Type, string Title, string Body, string? ImageUrl);

internal sealed record ScenePayloadV1(
    string Query,
    string Title,
    string Summary,
    List<SceneCardV1> Cards,
    DateTime CreatedAt);

/// <summary>
/// Cinematic Workspace V3 — minimal, proffsig layout:
///   [hero-bild]   (real bild från Wikipedia om möjligt, annars Pollinations som fallback)
///   [sammanfattning]   (Ollama summary, läses upp via TTS)
///   [video]   (om query nämner video/film/klipp/youtube)
///   [källor]   (Wikipedia/DDG från SceneResearchV1)
///
/// Inga 3 dekorativa AI-bilder. Ingen demo-chart. Ingen demo-map.
/// Vi vill ha **en** bild som hör ihop med ämnet, inte fyllnadsmaterial.
/// </summary>
internal static class SceneServiceV1
{
    public static ScenePayloadV1 BuildSkeleton(string query)
    {
        var q = string.IsNullOrWhiteSpace(query) ? "(tom fråga)" : query.Trim();
        var cards = new List<SceneCardV1>
        {
            // Hero-bild placeholder — fylls med riktig bild när research returnerar.
            new("hero", "Bild", "Söker en relevant bild...", null),
            new("summary", "Sammanfattning", "", null)
        };

        var lower = q.ToLowerInvariant();
        if (lower.Contains("video") || lower.Contains("klipp") || lower.Contains("youtube")
            || lower.Contains("film") || lower.Contains("titta"))
        {
            cards.Add(new SceneCardV1(
                "video",
                "Relaterad video",
                "Söker: " + q,
                "https://www.youtube.com/embed?listType=search&list=" + Uri.EscapeDataString(q)));
        }

        return new ScenePayloadV1(q, "Scen: " + q, "", cards, DateTime.Now);
    }

    // Bevarad för bakåtkompatibilitet — kallas av "all in one"-flödet om streaming-API inte används.
    public static ScenePayloadV1 Build(string query, string summary)
    {
        var p = BuildSkeleton(query);
        return new ScenePayloadV1(
            p.Query, p.Title,
            string.IsNullOrWhiteSpace(summary) ? "" : summary.Trim(),
            p.Cards, p.CreatedAt);
    }

    public static string PollinationsFallbackUrl(string query)
    {
        return "https://image.pollinations.ai/prompt/" + Uri.EscapeDataString(query.Trim())
            + "?width=900&height=520&nologo=true";
    }

    public static string ToJson(ScenePayloadV1 payload)
    {
        return JsonSerializer.Serialize(new
        {
            query = payload.Query,
            title = payload.Title,
            summary = payload.Summary,
            createdAt = payload.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            cards = payload.Cards.Select(c => new
            {
                type = c.Type,
                title = c.Title,
                body = c.Body,
                imageUrl = c.ImageUrl
            }).ToList()
        });
    }
}
