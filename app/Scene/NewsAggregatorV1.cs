using System.Text.RegularExpressions;
using System.Xml;

namespace JarvisClean;

internal sealed record NewsHeadlineV1(string Title, string Summary, string Url, string Source, DateTime? Published, string ImageUrl);

/// <summary>
/// Sprint 1+ kallkritik (2026-05-17) — aggregerar headlines fran etablerade nyhetsbyraer
/// via RSS/Atom. Anvands for "senaste nytt"-fragor istallet for Wikipedia.
///
/// Alla feeds ar gratis och offentliga. Inga API-keys kravs.
/// Failar tyst per source — en source som ar nere stoppar inte andra.
/// </summary>
internal static class NewsAggregatorV1
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static NewsAggregatorV1()
    {
        Http.DefaultRequestHeaders.UserAgent.TryParseAdd(
            "JarvisClean/1.0 (https://github.com/local; news-aggregator)");
    }

    // Whitelist av betrodda nyhetskallor. Lagg till per region/sprak vid behov.
    private static readonly (string Name, string Url)[] DefaultFeeds = new[]
    {
        ("SVT Nyheter", "https://www.svt.se/nyheter/rss.xml"),
        ("BBC World", "http://feeds.bbci.co.uk/news/world/rss.xml"),
        ("BBC Technology", "http://feeds.bbci.co.uk/news/technology/rss.xml"),
        ("Reuters World", "https://feeds.reuters.com/Reuters/worldNews"),
        ("AP Top News", "https://feeds.apnews.com/rss/apf-topnews"),
        ("Al Jazeera", "https://www.aljazeera.com/xml/rss/all.xml")
    };

    public static async Task<List<NewsHeadlineV1>> FetchHeadlinesAsync(string? topicFilter = null, int maxPerFeed = 3, int totalMax = 12)
    {
        var allHeadlines = new List<NewsHeadlineV1>();
        var tasks = DefaultFeeds.Select(f => TryFetchFeedAsync(f.Name, f.Url, maxPerFeed)).ToArray();
        var results = await Task.WhenAll(tasks);
        foreach (var batch in results) allHeadlines.AddRange(batch);

        // Filtrera pa topic om angivet
        if (!string.IsNullOrWhiteSpace(topicFilter))
        {
            var topic = topicFilter.ToLowerInvariant();
            var keywords = topic.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            allHeadlines = allHeadlines
                .Where(h =>
                {
                    var blob = (h.Title + " " + h.Summary).ToLowerInvariant();
                    return keywords.Any(k => blob.Contains(k));
                })
                .ToList();
        }

        // Sortera per publicering (nyaste forst), sen ta upp till totalMax
        return allHeadlines
            .OrderByDescending(h => h.Published ?? DateTime.MinValue)
            .Take(totalMax)
            .ToList();
    }

    private static async Task<List<NewsHeadlineV1>> TryFetchFeedAsync(string source, string url, int max)
    {
        var result = new List<NewsHeadlineV1>();
        try
        {
            using var resp = await Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return result;
            var xml = await resp.Content.ReadAsStringAsync();
            using var stringReader = new StringReader(xml);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreWhitespace = true };
            using var reader = XmlReader.Create(stringReader, settings);

            var doc = new XmlDocument();
            doc.Load(reader);
            // Stoder RSS 2.0 (<item>) och Atom (<entry>).
            var items = doc.GetElementsByTagName("item");
            if (items.Count == 0) items = doc.GetElementsByTagName("entry");

            foreach (XmlNode item in items)
            {
                if (result.Count >= max) break;
                var title = ExtractInner(item, "title");
                var link = ExtractInner(item, "link");
                if (string.IsNullOrWhiteSpace(link))
                {
                    // Atom: <link href="...">
                    var linkEl = item.SelectSingleNode("link");
                    if (linkEl?.Attributes?["href"] is { Value: { } href }) link = href;
                }
                var desc = ExtractInner(item, "description");
                if (string.IsNullOrWhiteSpace(desc)) desc = ExtractInner(item, "summary");
                desc = StripHtml(desc);

                var pubStr = ExtractInner(item, "pubDate");
                if (string.IsNullOrWhiteSpace(pubStr)) pubStr = ExtractInner(item, "published");
                DateTime? pub = null;
                if (!string.IsNullOrWhiteSpace(pubStr) && DateTime.TryParse(pubStr, out var dt)) pub = dt;

                // Forsok plocka media:thumbnail eller enclosure for bild
                var imgUrl = "";
                var mediaThumb = item.SelectSingleNode("*[local-name()='thumbnail']");
                if (mediaThumb?.Attributes?["url"] is { Value: { } murl }) imgUrl = murl;
                if (string.IsNullOrWhiteSpace(imgUrl))
                {
                    var enclosure = item.SelectSingleNode("enclosure");
                    if (enclosure?.Attributes?["url"] is { Value: { } eurl } &&
                        enclosure?.Attributes?["type"]?.Value is { } etype &&
                        etype.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        imgUrl = eurl;
                }

                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link)) continue;
                result.Add(new NewsHeadlineV1(
                    title.Trim(),
                    Truncate(desc, 220),
                    link.Trim(),
                    source,
                    pub,
                    imgUrl));
            }
        }
        catch
        {
            // best-effort
        }
        return result;
    }

    private static string ExtractInner(XmlNode node, string name)
    {
        var el = node.SelectSingleNode(name);
        if (el is null) return "";
        return el.InnerText ?? "";
    }

    private static string StripHtml(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = Regex.Replace(s, "<[^>]+>", " ");
        s = System.Net.WebUtility.HtmlDecode(s);
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim();
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
