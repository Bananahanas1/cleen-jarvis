using System.Text.RegularExpressions;

namespace JarvisClean;

// Auto-routing: enkla frågor → fast (qwen3:1.7b), komplexa → smart (qwen3:8b),
// kod-task → coder (qwen2.5-coder:7b), djupanalys → reason (deepseek-r1:7b).
//
// Användarens manuella val (via /modell byt X) prioriteras alltid om aktivt och
// inte är "fast" (fast = default → låt routern välja). Auto-upgrade fast→smart
// vid djup dialog (>5 turns).
public static class ModelRouter
{
    private static readonly Regex CodeRx = new(
        @"\b(klass|funktion|metod|fil|kod|fix|bug|implementera|refaktorera|debug|exception|stack|method|class|function|deploy|publish|build)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ReasonRx = new(
        @"\b(varför|analysera|jämför|utvärdera|bevisa|härled|orsak|root cause|root-cause)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PlanRx = new(
        @"\b(planera|design|arkitektur|tänk|borde|hur ska|strategi|approach|trade-off|jämfört med)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public sealed record Pick(string Model, string Reason);

    // activeModelOverride: aktuella _activeModel (null eller fast-modellen = låt routern välja).
    public static Pick PickModelForQuery(string query, int turnDepth, string? activeModelOverride)
    {
        // Manuellt val (annat än fast) respekteras alltid
        if (!string.IsNullOrEmpty(activeModelOverride) &&
            !activeModelOverride.Equals(ModelCatalog.Fast.Name, StringComparison.OrdinalIgnoreCase))
            return new Pick(activeModelOverride, "manuellt val");

        if (string.IsNullOrWhiteSpace(query))
            return new Pick(ModelCatalog.Fast.Name, "tom query");

        if (CodeRx.IsMatch(query)) return new Pick(ModelCatalog.Coder.Name, "kod-task");
        if (ReasonRx.IsMatch(query)) return new Pick(ModelCatalog.Reason.Name, "djupanalys");
        if (PlanRx.IsMatch(query) || query.Length > 200)
            return new Pick(ModelCatalog.Smart.Name, "komplext");
        if (turnDepth > 5)
            return new Pick(ModelCatalog.Smart.Name, "djup dialog");

        return new Pick(ModelCatalog.Fast.Name, "snabb");
    }

    // Kort badge för chat-svar: [fast], [smart], [code], [reason], [general]
    public static string BadgeForModel(string modelName)
    {
        if (modelName == ModelCatalog.Fast.Name) return "[fast]";
        if (modelName == ModelCatalog.Smart.Name) return "[smart]";
        if (modelName == ModelCatalog.Coder.Name) return "[code]";
        if (modelName == ModelCatalog.Reason.Name) return "[reason]";
        if (modelName == ModelCatalog.General.Name) return "[general]";
        return "[" + modelName + "]";
    }
}
