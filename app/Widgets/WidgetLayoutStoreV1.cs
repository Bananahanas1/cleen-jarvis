using System.Text.Json;

namespace JarvisClean;

internal sealed record WidgetPlacementV1(
    string Type,
    int GridX,
    int GridY,
    int GridW,
    int GridH,
    Dictionary<string, string>? Options);

internal sealed record WidgetLayoutV1(
    string Id,
    string Name,
    List<WidgetPlacementV1> Widgets);

/// <summary>
/// Widget V2 - lagrar named layouts (Work, Play, Brief, user-saved).
/// User-driven save (klick), ingen PendingApproval (samma som TaskStoreV1).
/// </summary>
internal static class WidgetLayoutStoreV1
{
    private static readonly object Lock = new();

    public static string LayoutsFilePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "data", "widgets", "layouts.json");
    }

    public static List<WidgetLayoutV1> LoadAll(string projectRoot)
    {
        var path = LayoutsFilePath(projectRoot);
        if (!File.Exists(path)) return SeedDefault();
        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<WidgetLayoutV1>>(json);
            return list ?? SeedDefault();
        }
        catch
        {
            return SeedDefault();
        }
    }

    public static WidgetLayoutV1? Get(string projectRoot, string id)
    {
        var all = LoadAll(projectRoot);
        return all.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static void Save(string projectRoot, WidgetLayoutV1 layout)
    {
        lock (Lock)
        {
            var all = LoadAll(projectRoot);
            var idx = all.FindIndex(l => string.Equals(l.Id, layout.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) all[idx] = layout;
            else all.Add(layout);
            WriteAll(projectRoot, all);
        }
    }

    public static bool Delete(string projectRoot, string id)
    {
        lock (Lock)
        {
            var all = LoadAll(projectRoot);
            var removed = all.RemoveAll(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                WriteAll(projectRoot, all);
                return true;
            }
            return false;
        }
    }

    public static string ToClientJson(List<WidgetLayoutV1> layouts)
    {
        return JsonSerializer.Serialize(layouts.Select(l => new
        {
            id = l.Id,
            name = l.Name,
            widgets = l.Widgets.Select(w => new
            {
                type = w.Type,
                gridX = w.GridX, gridY = w.GridY,
                gridW = w.GridW, gridH = w.GridH,
                options = w.Options
            }).ToList()
        }).ToList());
    }

    private static void WriteAll(string projectRoot, List<WidgetLayoutV1> all)
    {
        var path = LayoutsFilePath(projectRoot);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static List<WidgetLayoutV1> SeedDefault()
    {
        return new List<WidgetLayoutV1>
        {
            new("default", "Default", new List<WidgetPlacementV1>()),
            new("work", "Work", new List<WidgetPlacementV1>
            {
                new("text", 0, 0, 3, 2, new() { ["content"] = "Work tasks" }),
                new("chat-mini", 9, 0, 3, 4, null)
            }),
            new("play", "Play", new List<WidgetPlacementV1>()),
            new("brief", "Brief", new List<WidgetPlacementV1>())
        };
    }
}
