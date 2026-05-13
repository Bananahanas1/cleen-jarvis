using System.Text.Json;

namespace JarvisClean;

internal sealed record TaskItemV1(
    string Id,
    string Title,
    string Priority,
    string Status,
    string CreatedAt,
    string? CompletedAt
);

internal sealed record TaskChangeV1(
    string Action,
    string Id,
    string Title,
    string Priority
);

internal sealed class TaskDatabaseV1
{
    public List<TaskItemV1> Tasks { get; set; } = new();
}

internal static class TaskStoreV1
{
    public const string DefaultRelativePath = "data/tasks/tasks.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static TaskItemV1 Add(string projectRoot, string title, string priority)
    {
        var cleanTitle = CleanTitle(title);
        if (string.IsNullOrWhiteSpace(cleanTitle))
            throw new ArgumentException("Task title is required.", nameof(title));

        var db = Load(projectRoot);
        var item = new TaskItemV1(
            Id: CreateId(),
            Title: cleanTitle,
            Priority: NormalizePriority(priority),
            Status: "open",
            CreatedAt: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            CompletedAt: null
        );

        db.Tasks.Add(item);
        Save(projectRoot, db);
        return item;
    }

    public static bool Complete(string projectRoot, string id)
    {
        var cleanId = (id ?? "").Trim();
        if (string.IsNullOrWhiteSpace(cleanId))
            return false;

        var db = Load(projectRoot);
        var index = db.Tasks.FindIndex(task =>
            task.Id.Equals(cleanId, StringComparison.OrdinalIgnoreCase) &&
            task.Status.Equals("open", StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return false;

        var current = db.Tasks[index];
        db.Tasks[index] = current with
        {
            Status = "done",
            CompletedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        Save(projectRoot, db);
        return true;
    }

    public static List<TaskItemV1> ListOpen(string projectRoot, int limit = 20)
    {
        return Load(projectRoot).Tasks
            .Where(task => task.Status.Equals("open", StringComparison.OrdinalIgnoreCase))
            .OrderBy(task => PriorityRank(task.Priority))
            .ThenBy(task => task.CreatedAt, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    public static List<TaskItemV1> Search(string projectRoot, string query, int limit = 20)
    {
        var cleanQuery = (query ?? "").Trim();
        if (cleanQuery.Length == 0)
            return new List<TaskItemV1>();

        return Load(projectRoot).Tasks
            .Where(task => task.Title.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                           task.Id.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase) ||
                           task.Priority.Contains(cleanQuery, StringComparison.OrdinalIgnoreCase))
            .OrderBy(task => task.Status.Equals("open", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(task => PriorityRank(task.Priority))
            .ThenBy(task => task.CreatedAt, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    public static (int Open, int Done, int Red, int Orange, int Blue) Status(string projectRoot)
    {
        var tasks = Load(projectRoot).Tasks;
        var open = tasks.Where(task => task.Status.Equals("open", StringComparison.OrdinalIgnoreCase)).ToList();
        return (
            Open: open.Count,
            Done: tasks.Count(task => task.Status.Equals("done", StringComparison.OrdinalIgnoreCase)),
            Red: open.Count(task => task.Priority.Equals("red", StringComparison.OrdinalIgnoreCase)),
            Orange: open.Count(task => task.Priority.Equals("orange", StringComparison.OrdinalIgnoreCase)),
            Blue: open.Count(task => task.Priority.Equals("blue", StringComparison.OrdinalIgnoreCase))
        );
    }

    public static bool TryParseAddInput(string raw, out string title, out string priority)
    {
        priority = "orange";
        title = "";

        var parts = (raw ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        if (parts.Count == 0)
            return false;

        for (var i = parts.Count - 1; i >= 0; i--)
        {
            if (TryNormalizePriorityToken(parts[i], out var parsed))
            {
                priority = parsed;
                parts.RemoveAt(i);
            }
        }

        title = CleanTitle(string.Join(" ", parts));
        return title.Length > 0;
    }

    public static string CreatePendingAddJson(string title, string priority)
    {
        return JsonSerializer.Serialize(
            new TaskChangeV1("add", "", CleanTitle(title), NormalizePriority(priority)),
            JsonOptions
        );
    }

    public static string CreatePendingCompleteJson(string id)
    {
        return JsonSerializer.Serialize(
            new TaskChangeV1("complete", (id ?? "").Trim(), "", ""),
            JsonOptions
        );
    }

    public static bool TryReadPendingChange(string json, out TaskChangeV1 change, out string error)
    {
        try
        {
            change = JsonSerializer.Deserialize<TaskChangeV1>(json ?? "", JsonOptions)
                ?? new TaskChangeV1("", "", "", "");
            error = "";
            return !string.IsNullOrWhiteSpace(change.Action);
        }
        catch (Exception ex)
        {
            change = new TaskChangeV1("", "", "", "");
            error = ex.Message;
            return false;
        }
    }

    public static string FormatList(IEnumerable<TaskItemV1> tasks, string emptyText = "Inga öppna tasks.")
    {
        var lines = tasks.Select(FormatLine).ToList();
        return lines.Count == 0 ? emptyText : string.Join(Environment.NewLine, lines);
    }

    public static string FormatStatus(string projectRoot)
    {
        var status = Status(projectRoot);
        return "Tasks status:" + Environment.NewLine +
               "- Öppna: " + status.Open + Environment.NewLine +
               "- Klara: " + status.Done + Environment.NewLine +
               "- Röd/orange/blå: " + status.Red + "/" + status.Orange + "/" + status.Blue + Environment.NewLine +
               "- Fil: " + DefaultRelativePath;
    }

    public static string NormalizePriority(string priority)
    {
        var normalized = Normalize(priority);
        return normalized switch
        {
            "red" or "rod" or "röd" or "p1" or "prio1" or "urgent" or "akut" => "red",
            "orange" or "gul" or "yellow" or "p2" or "prio2" or "normal" => "orange",
            "blue" or "bla" or "blå" or "p3" or "prio3" or "low" or "lag" or "låg" => "blue",
            _ => "orange"
        };
    }

    private static TaskDatabaseV1 Load(string projectRoot)
    {
        var path = StorePath(projectRoot);
        if (!File.Exists(path))
            return new TaskDatabaseV1();

        try
        {
            return JsonSerializer.Deserialize<TaskDatabaseV1>(File.ReadAllText(path), JsonOptions)
                ?? new TaskDatabaseV1();
        }
        catch
        {
            return new TaskDatabaseV1();
        }
    }

    private static void Save(string projectRoot, TaskDatabaseV1 db)
    {
        var path = StorePath(projectRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(db, JsonOptions));
    }

    private static string StorePath(string projectRoot)
    {
        return Path.Combine(projectRoot, "data", "tasks", "tasks.json");
    }

    private static string FormatLine(TaskItemV1 task)
    {
        var status = task.Status.Equals("done", StringComparison.OrdinalIgnoreCase) ? "klar" : "öppen";
        return "- " + task.Id + " [" + task.Priority + "] " + task.Title + " (" + status + ")";
    }

    private static bool TryNormalizePriorityToken(string token, out string priority)
    {
        var clean = Normalize(token).Trim('!', '#', '[', ']', '(', ')', ':');
        if (clean.StartsWith("prio:", StringComparison.OrdinalIgnoreCase))
            clean = clean[5..];
        if (clean.StartsWith("priority:", StringComparison.OrdinalIgnoreCase))
            clean = clean[9..];

        var normalized = NormalizePriority(clean);
        if (normalized == "orange" && clean is not "orange" and not "gul" and not "yellow" and not "p2" and not "prio2" and not "normal")
        {
            priority = "orange";
            return false;
        }

        priority = normalized;
        return true;
    }

    private static string CleanTitle(string title)
    {
        return (title ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string CreateId()
    {
        return "T" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")[..4];
    }

    private static int PriorityRank(string priority)
    {
        return NormalizePriority(priority) switch
        {
            "red" => 0,
            "orange" => 1,
            "blue" => 2,
            _ => 3
        };
    }

    private static string Normalize(string value)
    {
        return (value ?? "").Trim().ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");
    }
}
