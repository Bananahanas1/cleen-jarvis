namespace JarvisClean;

internal static class ContextBudgetEstimatorV1
{
    private const int ApproxCharsPerToken = 4;

    public static int EstimateTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return Math.Max(1, (int)Math.Ceiling(text.Length / (double)ApproxCharsPerToken));
    }

    public static int EstimateMessages(IEnumerable<object> messages)
    {
        var total = 0;
        foreach (var message in messages)
            total += EstimateTokens(message?.ToString());

        return total;
    }

    public static string FormatCompact(int tokens)
    {
        if (tokens <= 0)
            return "n/a";

        if (tokens < 1000)
            return tokens + " tok";

        return (tokens / 1000.0).ToString("0.0") + "k tok";
    }

    public static int EstimateLocalJobContextTokens(string kind, int processedItems, string message)
    {
        var perItem = kind switch
        {
            "project-audit" => 48,
            "project-index" => 32,
            _ => 16
        };

        var messageTokens = EstimateTokens(message);
        var itemTokens = Math.Max(0, processedItems) * perItem;
        return Math.Min(160000, messageTokens + itemTokens);
    }
}
