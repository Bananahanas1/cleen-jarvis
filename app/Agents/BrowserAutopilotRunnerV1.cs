using System.Text.RegularExpressions;

namespace JarvisClean;

internal static class BrowserAutopilotRunnerV1
{
    // NoClickOrTypeInV1: this first runner may open/search/read only.
    // Form filling, clicking and sending will be a later runner with stronger guards.
    private static readonly string[] BlockedTerms =
    {
        "login",
        "logga in",
        "lösenord",
        "password",
        "passord",
        "betal",
        "köp",
        "checkout",
        "bankid",
        "bank-id",
        "secret",
        "api key",
        "token",
        "skicka",
        "send",
        "publicer",
        "publish",
        "delete",
        "radera"
    };

    public static async Task<string> StartAsync(string mission)
    {
        mission = (mission ?? "").Trim();
        if (string.IsNullOrWhiteSpace(mission))
            return "Browser Autopilot kräver ett tydligt uppdrag.";

        if (IsBlockedMission(mission, out var blocked))
        {
            return "Browser Autopilot blockerat: uppdraget innehåller känsligt steg (" + blocked + ").\n" +
                   "V1 stoppar vid login, betalningar, password/secrets, skicka/publicer och destruktiva steg.";
        }

        var header =
            "Browser Autopilot Runner V1\n" +
            "Synlig browser: " + BrowserPolicyV1.DisplayName + "\n" +
            "Intern engine vid framtida automation: " + BrowserPolicyV1.InternalAutomationEngine + "\n" +
            "Scope: öppna/söka/läsa. Inga klick, ingen textinmatning och inget skickande i V1.\n\n";

        if (TryExtractUrl(mission, out var url))
        {
            var open = SafeAppLauncher.TryOpenUrlInOpera(url);
            var read = await WebSearcher.FetchTextAsync(url, 2200).ConfigureAwait(false);
            return header +
                   "Uppdrag: läs URL\n" +
                   "Open: " + open.Message + "\n\n" +
                   "Läsning:\n" + read;
        }

        var search = WebSearcher.OpenSearchInOpera(mission);
        return header +
               "Uppdrag: sök i webben\n" +
               search + "\n\n" +
               "Nästa V1-steg: välj/läs resultat manuellt eller ge Jarvis en URL att läsa.";
    }

    public static bool IsBlockedMission(string mission, out string blockedTerm)
    {
        var lower = (mission ?? "").ToLowerInvariant();
        blockedTerm = BlockedTerms.FirstOrDefault(term => lower.Contains(term)) ?? "";
        return !string.IsNullOrWhiteSpace(blockedTerm);
    }

    public static bool TryExtractUrl(string text, out string url)
    {
        url = "";
        var input = (text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var match = Regex.Match(input, @"https?://[^\s]+", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            url = match.Value.TrimEnd('.', ',', ')', ']');
            return true;
        }

        match = Regex.Match(input, @"\bwww\.[^\s]+", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            url = match.Value.TrimEnd('.', ',', ')', ']');
            return true;
        }

        return false;
    }
}
