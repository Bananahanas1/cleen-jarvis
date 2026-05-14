using System.Text.Json;

namespace JarvisClean;

internal sealed record MapNoteV1(
    string Id,
    double Lat,
    double Lon,
    string Name,
    string Note,
    string CreatedAt);

/// <summary>
/// Kartan V1 - lagrar användarens noter på platser på jorden.
/// User-driven writes (klick på "Spara") - inte LLM-driven, så ingen PendingApproval
/// behövs (samma mönster som TaskStoreV1 + Settings-panelen).
/// </summary>
internal static class MapNoteStoreV1
{
    private static readonly object Lock = new();

    public static string NotesFilePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "data", "karta", "notes.json");
    }

    public static List<MapNoteV1> LoadAll(string projectRoot)
    {
        var path = NotesFilePath(projectRoot);
        if (!File.Exists(path)) return new List<MapNoteV1>();
        try
        {
            var json = File.ReadAllText(path);
            var notes = JsonSerializer.Deserialize<List<MapNoteV1>>(json);
            return notes ?? new List<MapNoteV1>();
        }
        catch
        {
            return new List<MapNoteV1>();
        }
    }

    public static MapNoteV1 Add(string projectRoot, double lat, double lon, string name, string note)
    {
        var entry = new MapNoteV1(
            Id: DateTime.Now.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..6],
            Lat: lat,
            Lon: lon,
            Name: string.IsNullOrWhiteSpace(name) ? "Plats" : name.Trim(),
            Note: (note ?? string.Empty).Trim(),
            CreatedAt: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        lock (Lock)
        {
            var all = LoadAll(projectRoot);
            all.Add(entry);
            Save(projectRoot, all);
        }
        return entry;
    }

    public static bool Delete(string projectRoot, string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        lock (Lock)
        {
            var all = LoadAll(projectRoot);
            var removed = all.RemoveAll(n => n.Id == id);
            if (removed > 0)
            {
                Save(projectRoot, all);
                return true;
            }
            return false;
        }
    }

    private static void Save(string projectRoot, List<MapNoteV1> all)
    {
        var path = NotesFilePath(projectRoot);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public static string ToClientJson(List<MapNoteV1> notes)
    {
        return JsonSerializer.Serialize(notes.Select(n => new
        {
            id = n.Id,
            lat = n.Lat,
            lon = n.Lon,
            name = n.Name,
            note = n.Note,
            createdAt = n.CreatedAt
        }).ToList());
    }
}
