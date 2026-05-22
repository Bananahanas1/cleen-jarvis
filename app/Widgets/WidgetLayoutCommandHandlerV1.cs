namespace JarvisClean;

internal static class WidgetLayoutCommandHandlerV1
{
    public static string Apply(CommandResult command, string projectRoot)
    {
        return command.Intent switch
        {
            CommandIntent.WidgetLayoutSave => SaveCurrent(command, projectRoot),
            CommandIntent.WidgetLayoutLoad => Load(command, projectRoot),
            CommandIntent.WidgetLayoutList => List(projectRoot),
            CommandIntent.WidgetLayoutDelete => Delete(command, projectRoot),
            _ => "Okänt widget-layout-kommando."
        };
    }

    private static string SaveCurrent(CommandResult command, string projectRoot)
    {
        var name = command.Arguments.TryGetValue("name", out var n) ? (n ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(name))
            return "Ange ett namn för layouten. Exempel: /widget save Mitt-namn";
        return "Sparar nuvarande widget-layout som '" + name + "'... (klart om en stund)";
    }

    private static string Load(CommandResult command, string projectRoot)
    {
        var name = command.Arguments.TryGetValue("name", out var n) ? (n ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(name))
            return "Ange vilken layout att ladda. Lista: /widget list";
        var layout = WidgetLayoutStoreV1.Get(projectRoot, name);
        if (layout is null)
            return "Layout '" + name + "' hittades inte. Lista: /widget list";
        return "Laddar layout '" + layout.Name + "' (" + layout.Widgets.Count + " widgets).";
    }

    private static string List(string projectRoot)
    {
        var all = WidgetLayoutStoreV1.LoadAll(projectRoot);
        if (all.Count == 0) return "Inga sparade layouts.";
        var lines = all.Select(l => "• " + l.Name + " (" + l.Widgets.Count + " widgets) — id: " + l.Id);
        return "Widget-layouts:\n" + string.Join("\n", lines);
    }

    private static string Delete(CommandResult command, string projectRoot)
    {
        var name = command.Arguments.TryGetValue("name", out var n) ? (n ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(name))
            return "Ange vilken layout att radera.";
        var ok = WidgetLayoutStoreV1.Delete(projectRoot, name);
        return ok ? "Layout '" + name + "' raderad." : "Layout '" + name + "' hittades inte.";
    }
}
