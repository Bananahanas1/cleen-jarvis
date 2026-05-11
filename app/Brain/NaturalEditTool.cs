using System.Text.RegularExpressions;

namespace JarvisClean;

internal sealed record NaturalEditRequest(string RelativePath, string Instruction);

internal static class NaturalEditTool
{
    public const int MaxEditableChars = 24000;

    private static readonly Regex NaturalEditRx = new(
        @"^\s*(?:gå\s+in\s+i|ga\s+in\s+i|öppna|oppna|ändra|andra|fixa|uppdatera|refaktorera)\s+(?:filen?\s+)?(?<path>[\w./\\-]+\.(?:md|txt|json|cs|html|css|js|ps1))\s*(?:och|att|så\s+att|sa\s+att|för\s+att|for\s+att|:|-)?\s*(?<instruction>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TryParseNaturalLanguage(string input, out NaturalEditRequest request)
    {
        request = new NaturalEditRequest("", "");

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var match = NaturalEditRx.Match(input.Trim());
        if (!match.Success)
            return false;

        var path = match.Groups["path"].Value.Trim().Replace("\\", "/");
        var instruction = match.Groups["instruction"].Value.Trim();

        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(instruction))
            return false;

        request = new NaturalEditRequest(path, instruction);
        return true;
    }

    public static string BuildPrompt(string relativePath, string currentContent, string instruction)
    {
        return
            "Målfil: " + relativePath + "\n\n" +
            "Ändringsinstruktion:\n" + instruction + "\n\n" +
            "Nuvarande filinnehåll:\n" +
            "<<<FILE_START>>>\n" +
            currentContent +
            "\n<<<FILE_END>>>\n\n" +
            "Returnera hela nya filinnehållet. Returnera ingen förklaring.";
    }

    public static string CleanFullFileContent(string raw)
    {
        var text = (raw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var fenced = Regex.Match(text, @"^```[a-zA-Z0-9_+\-.]*\s*\n(?<body>[\s\S]*?)\n```\s*$");
        if (fenced.Success)
            return fenced.Groups["body"].Value.TrimEnd();

        var firstFence = Regex.Match(text, @"```[a-zA-Z0-9_+\-.]*\s*\n(?<body>[\s\S]*?)\n```");
        if (firstFence.Success)
            return firstFence.Groups["body"].Value.TrimEnd();

        return text;
    }
}
