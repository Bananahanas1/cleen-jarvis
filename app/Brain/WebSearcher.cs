using System.Net.Http;
using System.Text.RegularExpressions;

namespace JarvisClean;

// Web-sök via Google + Opera (per användarens val 2026-05-10).
// /sök <q> → bygger Google-search-URL och öppnar i Opera (ingen scraping).
// /läs <url> → hämtar sidan och returnerar text för Jarvis att läsa/sammanfatta.
//
// Säkerhet:
// - InternetProbe-koll innan request — offline → tydligt fel istället för hängning
// - Opera är whitelistad i SafeAppLauncher
// - Fetch har 12s timeout
public static class WebSearcher
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };
    static WebSearcher()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Jarvis-clean");
    }

    // Bygger Google-search-URL och öppnar i Opera. Ingen scraping = ingen risk för
    // CAPTCHA, blockning, eller stale data. Användaren ser SERP direkt i sin webbläsare.
    public static string OpenSearchInOpera(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Skriv: /sök <query>. Exempel: /sök ollama tutorial";
        var url = "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
        var res = SafeAppLauncher.TryOpenUrlInOpera(url);
        return res.Ok
            ? "Sökning öppnad i Opera: " + query + "\nURL: " + url
            : "Kunde inte öppna Opera: " + res.Message;
    }

    public static async Task<string> FetchTextAsync(string url, int maxChars = 4000)
    {
        if (string.IsNullOrWhiteSpace(url)) return "Ingen URL angavs.";
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;

        var online = await JarvisForm.IsInternetOnlineCachedAsync().ConfigureAwait(false);
        if (!online) return "Internet saknas, kan inte hämta sidan.";

        try
        {
            var html = await Http.GetStringAsync(url).ConfigureAwait(false);
            var text = StripHtml(html);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            if (text.Length > maxChars) text = text.Substring(0, maxChars) + "\n[...trimmed...]";
            return text;
        }
        catch (Exception ex)
        {
            return "Kunde inte hämta " + url + ": " + ex.Message;
        }
    }

    private static string StripHtml(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = Regex.Replace(s, @"<[^>]+>", " ");
        s = s.Replace("&nbsp;", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"");
        return s.Trim();
    }
}
