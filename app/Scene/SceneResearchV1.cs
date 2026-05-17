using System.Net;
using System.Text.Json;

namespace JarvisClean;

internal sealed record SceneResearchHitV1(string Title, string Snippet, string Url, string ThumbnailUrl = "");

internal sealed record SceneImageHitV1(string Url, string Caption, string SourceUrl);

/// <summary>
/// Cinematic Workspace V2 — riktig research via robusta JSON-API:er.
///
/// Källor (alla gratis, ingen API-nyckel):
///   1. Wikipedia opensearch + summary REST (svenska först, fallback engelska)
///   2. DuckDuckGo Instant Answer JSON API (Abstract + RelatedTopics)
///
/// Båda är fail-silent. Returnerar union (Wikipedia först, sedan DDG).
/// Vi använder INTE DDG HTML-scrape (lite/html.duckduckgo.com) — DDG returnerar 403 för automatiserade
/// requests, så det är opålitligt.
/// </summary>
internal static class SceneResearchV1
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    static SceneResearchV1()
    {
        Http.DefaultRequestHeaders.UserAgent.TryParseAdd(
            "JarvisClean/1.0 (https://github.com/local; cinematic-workspace)");
        Http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("sv-SE,sv;q=0.9,en;q=0.7");
    }

    public static async Task<List<SceneResearchHitV1>> SearchAsync(string query, int maxResults = 5)
    {
        var result = new List<SceneResearchHitV1>();
        if (string.IsNullOrWhiteSpace(query)) return result;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var wikiSv = TrySearchWikipediaAsync(query, "sv", 3);
        var wikiEn = TrySearchWikipediaAsync(query, "en", 2);
        var ddg = TryDuckDuckGoInstantAsync(query, 3);

        // Wikipedia svenska först — högst relevans för svensk användare.
        AppendUnique(seen, result, await wikiSv, maxResults);
        if (result.Count < maxResults)
            AppendUnique(seen, result, await wikiEn, maxResults);
        if (result.Count < maxResults)
            AppendUnique(seen, result, await ddg, maxResults);

        return result;
    }

    private static void AppendUnique(HashSet<string> seen, List<SceneResearchHitV1> sink, List<SceneResearchHitV1> hits, int max)
    {
        foreach (var hit in hits)
        {
            if (sink.Count >= max) return;
            if (string.IsNullOrWhiteSpace(hit.Url)) continue;
            if (!seen.Add(hit.Url)) continue;
            sink.Add(hit);
        }
    }

    private static async Task<List<SceneResearchHitV1>> TrySearchWikipediaAsync(string query, string lang, int max)
    {
        var result = new List<SceneResearchHitV1>();
        try
        {
            // 1. opensearch hämtar top-N matchande titlar
            var openSearchUrl =
                "https://" + lang + ".wikipedia.org/w/api.php?action=opensearch&limit=" + max +
                "&namespace=0&format=json&search=" + Uri.EscapeDataString(query);

            using var openResp = await Http.GetAsync(openSearchUrl);
            if (!openResp.IsSuccessStatusCode) return result;
            using var openJson = JsonDocument.Parse(await openResp.Content.ReadAsStringAsync());
            var root = openJson.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 4) return result;

            var titles = root[1];
            if (titles.ValueKind != JsonValueKind.Array) return result;

            // 2. För varje titel — hämta rest_v1 summary (kortare snippet + canonical URL)
            foreach (var titleEl in titles.EnumerateArray())
            {
                if (result.Count >= max) break;
                var title = titleEl.GetString();
                if (string.IsNullOrWhiteSpace(title)) continue;

                try
                {
                    var summaryUrl = "https://" + lang + ".wikipedia.org/api/rest_v1/page/summary/" +
                                     Uri.EscapeDataString(title.Replace(' ', '_'));
                    using var sumResp = await Http.GetAsync(summaryUrl);
                    if (!sumResp.IsSuccessStatusCode) continue;
                    using var sumJson = JsonDocument.Parse(await sumResp.Content.ReadAsStringAsync());
                    var sumRoot = sumJson.RootElement;

                    var extract = sumRoot.TryGetProperty("extract", out var extractEl) ? extractEl.GetString() ?? "" : "";
                    var pageUrl = "";
                    if (sumRoot.TryGetProperty("content_urls", out var urls) &&
                        urls.TryGetProperty("desktop", out var desktop) &&
                        desktop.TryGetProperty("page", out var pageEl))
                    {
                        pageUrl = pageEl.GetString() ?? "";
                    }
                    if (string.IsNullOrEmpty(pageUrl))
                    {
                        pageUrl = "https://" + lang + ".wikipedia.org/wiki/" + Uri.EscapeDataString(title.Replace(' ', '_'));
                    }

                    // Wikipedia REST returnerar thumbnail.source (~320 px). Använder vi som hero-bild.
                    var thumbUrl = "";
                    if (sumRoot.TryGetProperty("originalimage", out var origImg) &&
                        origImg.TryGetProperty("source", out var origSrc))
                    {
                        thumbUrl = origSrc.GetString() ?? "";
                    }
                    if (string.IsNullOrEmpty(thumbUrl) &&
                        sumRoot.TryGetProperty("thumbnail", out var thumbImg) &&
                        thumbImg.TryGetProperty("source", out var thumbSrc))
                    {
                        thumbUrl = thumbSrc.GetString() ?? "";
                    }

                    var displayTitle = sumRoot.TryGetProperty("title", out var dispEl) ? dispEl.GetString() ?? title : title;
                    var snippet = Truncate(extract, 220);
                    if (string.IsNullOrWhiteSpace(snippet))
                        snippet = "Wikipedia-artikel om " + displayTitle + ".";

                    result.Add(new SceneResearchHitV1(displayTitle, snippet, pageUrl, thumbUrl));
                }
                catch
                {
                    // En misslyckad artikel ska inte stoppa resten.
                }
            }
        }
        catch
        {
            // best-effort
        }
        return result;
    }

    private static async Task<List<SceneResearchHitV1>> TryDuckDuckGoInstantAsync(string query, int max)
    {
        var result = new List<SceneResearchHitV1>();
        try
        {
            var url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query) +
                      "&format=json&no_redirect=1&no_html=1&skip_disambig=1&t=jarvis-clean";
            using var resp = await Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return result;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            // Abstract (huvudartikel)
            var heading = root.TryGetProperty("Heading", out var h) ? h.GetString() ?? "" : "";
            var abstractText = root.TryGetProperty("AbstractText", out var at) ? at.GetString() ?? "" : "";
            var abstractUrl = root.TryGetProperty("AbstractURL", out var au) ? au.GetString() ?? "" : "";
            if (!string.IsNullOrWhiteSpace(abstractUrl) && !string.IsNullOrWhiteSpace(heading))
            {
                result.Add(new SceneResearchHitV1(heading, Truncate(abstractText, 220), abstractUrl));
            }

            // RelatedTopics — många är listor av {Text, FirstURL}
            if (root.TryGetProperty("RelatedTopics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            {
                foreach (var topic in topics.EnumerateArray())
                {
                    if (result.Count >= max) break;
                    if (topic.TryGetProperty("FirstURL", out var furl) &&
                        topic.TryGetProperty("Text", out var text))
                    {
                        var u = furl.GetString() ?? "";
                        var t = text.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(u) && !string.IsNullOrWhiteSpace(t))
                        {
                            // DDG-format: "Topic name - Beskrivning..."
                            var dash = t.IndexOf(" - ", StringComparison.Ordinal);
                            var topicTitle = dash > 0 ? t.Substring(0, dash) : t;
                            var topicSnippet = dash > 0 ? t.Substring(dash + 3) : "";
                            result.Add(new SceneResearchHitV1(Truncate(topicTitle, 80), Truncate(topicSnippet, 220), u));
                        }
                    }
                }
            }
        }
        catch
        {
            // best-effort
        }
        return result;
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim();
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    /// <summary>
    /// Sprint 1 (2026-05-17) — hämtar fler bilder per query via Wikipedia media-list.
    /// Returnerar upp till `maxResults` bilder, sorterade efter relevans (svenska top-1
    /// först, sen engelska top-1, sedan top-2 etc.).
    /// </summary>
    public static async Task<List<SceneImageHitV1>> CollectImagesAsync(string query, int maxResults = 5)
    {
        var result = new List<SceneImageHitV1>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query)) return result;

        // 3 källor parallellt: Wikipedia svenska, Wikipedia engelska, Wikimedia Commons.
        var svImgs = TryCollectWikipediaImagesAsync(query, "sv", maxResults);
        var enImgs = TryCollectWikipediaImagesAsync(query, "en", maxResults);
        var commonsImgs = TryCollectCommonsImagesAsync(query, maxResults);

        AppendUniqueImages(seen, result, await svImgs, maxResults);
        if (result.Count < maxResults)
            AppendUniqueImages(seen, result, await enImgs, maxResults);
        if (result.Count < maxResults)
            AppendUniqueImages(seen, result, await commonsImgs, maxResults);

        return result;
    }

    /// <summary>
    /// Sprint 1+ (2026-05-17) — Wikimedia Commons är en stor bilddatabas med fria bilder.
    /// API:t är samma MediaWiki som Wikipedia. Anropet returnerar både filnamn och direkta URLs i ett svep.
    /// </summary>
    private static async Task<List<SceneImageHitV1>> TryCollectCommonsImagesAsync(string query, int max)
    {
        var result = new List<SceneImageHitV1>();
        try
        {
            // generator=search hittar filer som matchar query.
            // gsrnamespace=6 = File namespace (= bilder).
            // prop=imageinfo + iiurlwidth=800 → returnerar URL till 800px-versionen direkt.
            var url =
                "https://commons.wikimedia.org/w/api.php?action=query" +
                "&generator=search&gsrsearch=" + Uri.EscapeDataString(query) +
                "&gsrnamespace=6&gsrlimit=" + Math.Min(max * 2, 10) +
                "&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=800" +
                "&format=json&formatversion=2";

            using var resp = await Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return result;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("query", out var q)) return result;
            if (!q.TryGetProperty("pages", out var pages) || pages.ValueKind != JsonValueKind.Array) return result;

            foreach (var page in pages.EnumerateArray())
            {
                if (result.Count >= max) break;
                var title = page.TryGetProperty("title", out var ttl) ? ttl.GetString() ?? "" : "";
                if (!title.StartsWith("File:", StringComparison.OrdinalIgnoreCase)) continue;
                if (title.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) continue;
                if (title.Contains("logo", StringComparison.OrdinalIgnoreCase)) continue;

                if (!page.TryGetProperty("imageinfo", out var infoArr) || infoArr.ValueKind != JsonValueKind.Array) continue;
                if (infoArr.GetArrayLength() == 0) continue;
                var info = infoArr[0];

                // Föredra thumb (begränsad bredd) framför full-res original.
                var src = info.TryGetProperty("thumburl", out var tu) ? tu.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(src))
                    src = info.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(src)) continue;

                var descriptionUrl = info.TryGetProperty("descriptionurl", out var du) ? du.GetString() ?? "" : "";

                var caption = title;
                if (caption.StartsWith("File:", StringComparison.OrdinalIgnoreCase)) caption = caption.Substring(5);
                var dot = caption.LastIndexOf('.');
                if (dot > 0) caption = caption.Substring(0, dot);
                caption = caption.Replace('_', ' ').Trim();

                result.Add(new SceneImageHitV1(src, caption, descriptionUrl));
            }
        }
        catch
        {
            // best-effort
        }
        return result;
    }

    private static void AppendUniqueImages(HashSet<string> seen, List<SceneImageHitV1> sink, List<SceneImageHitV1> hits, int max)
    {
        foreach (var img in hits)
        {
            if (sink.Count >= max) return;
            if (string.IsNullOrWhiteSpace(img.Url)) continue;
            if (!seen.Add(img.Url)) continue;
            sink.Add(img);
        }
    }

    private static async Task<List<SceneImageHitV1>> TryCollectWikipediaImagesAsync(string query, string lang, int max)
    {
        var result = new List<SceneImageHitV1>();
        try
        {
            // Hämta top-1 article-title via opensearch.
            var openSearchUrl =
                "https://" + lang + ".wikipedia.org/w/api.php?action=opensearch&limit=1" +
                "&namespace=0&format=json&search=" + Uri.EscapeDataString(query);
            using var openResp = await Http.GetAsync(openSearchUrl);
            if (!openResp.IsSuccessStatusCode) return result;
            using var openJson = JsonDocument.Parse(await openResp.Content.ReadAsStringAsync());
            var root = openJson.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 4) return result;
            var titles = root[1];
            if (titles.ValueKind != JsonValueKind.Array || titles.GetArrayLength() == 0) return result;
            var title = titles[0].GetString();
            if (string.IsNullOrWhiteSpace(title)) return result;

            var encodedTitle = Uri.EscapeDataString(title.Replace(' ', '_'));
            var sourcePage = "https://" + lang + ".wikipedia.org/wiki/" + encodedTitle;

            // Hämta media-list för artikeln. Endpoint listar alla bilder/video som ingår i artikeln.
            var mediaListUrl = "https://" + lang + ".wikipedia.org/api/rest_v1/page/media-list/" + encodedTitle;
            using var mediaResp = await Http.GetAsync(mediaListUrl);
            if (!mediaResp.IsSuccessStatusCode) return result;
            using var mediaJson = JsonDocument.Parse(await mediaResp.Content.ReadAsStringAsync());
            var mediaRoot = mediaJson.RootElement;
            if (!mediaRoot.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return result;

            foreach (var item in items.EnumerateArray())
            {
                if (result.Count >= max) break;
                var itemType = item.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                if (!string.Equals(itemType, "image", StringComparison.OrdinalIgnoreCase)) continue;

                // Filtrera bort SVG-logos och små icon-bilder
                var itemTitle = item.TryGetProperty("title", out var tt) ? tt.GetString() ?? "" : "";
                if (itemTitle.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) continue;
                if (itemTitle.Contains("logo", StringComparison.OrdinalIgnoreCase)) continue;
                if (itemTitle.Contains("icon", StringComparison.OrdinalIgnoreCase)) continue;

                // srcset[].src ger bild-URL (oftast //upload.wikimedia.org/...)
                if (!item.TryGetProperty("srcset", out var srcset) || srcset.ValueKind != JsonValueKind.Array) continue;
                if (srcset.GetArrayLength() == 0) continue;

                // Välj högsta-resolution-varianten (sista i listan eller den med största scale).
                JsonElement bestSrc = default;
                bool foundSrc = false;
                foreach (var s in srcset.EnumerateArray())
                {
                    if (s.TryGetProperty("src", out var srcEl) && srcEl.ValueKind == JsonValueKind.String)
                    {
                        bestSrc = s;
                        foundSrc = true;
                    }
                }
                if (!foundSrc) continue;
                if (!bestSrc.TryGetProperty("src", out var bestSrcEl)) continue;
                var src = bestSrcEl.GetString() ?? "";
                if (string.IsNullOrWhiteSpace(src)) continue;

                // Wikipedia URLs är ibland protokoll-relativa (//upload.wikimedia.org/...).
                if (src.StartsWith("//", StringComparison.Ordinal)) src = "https:" + src;

                // Caption: använd file-title utan "File:"-prefix och utan extension.
                var caption = itemTitle;
                if (caption.StartsWith("File:", StringComparison.OrdinalIgnoreCase)) caption = caption.Substring(5);
                var dot = caption.LastIndexOf('.');
                if (dot > 0) caption = caption.Substring(0, dot);
                caption = caption.Replace('_', ' ').Trim();

                result.Add(new SceneImageHitV1(src, caption, sourcePage));
            }
        }
        catch
        {
            // best-effort
        }
        return result;
    }
}
