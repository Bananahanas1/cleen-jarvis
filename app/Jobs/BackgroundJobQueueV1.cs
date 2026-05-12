using System.Text.Json;

namespace JarvisClean;

internal enum BackgroundJobStateV1
{
    Queued,
    Running,
    Completed,
    Cancelled,
    Failed
}

internal sealed class BackgroundJobRecordV1
{
    public string Id { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public BackgroundJobStateV1 State { get; set; } = BackgroundJobStateV1.Queued;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ResultPath { get; set; } = string.Empty;
    public DateTime StartedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

internal static class BackgroundJobQueueV1
{
    private static readonly object Gate = new();
    private static readonly List<BackgroundJobRecordV1> Jobs = new();
    private static readonly Dictionary<string, CancellationTokenSource> Cancellations = new();

    public static string StartProjectIndexJob(string projectRoot)
    {
        var id = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var job = new BackgroundJobRecordV1
        {
            Id = id,
            Kind = "project-index",
            Title = "Project Index scan",
            Message = "Köad för projektindexering."
        };

        var cts = new CancellationTokenSource();
        lock (Gate)
        {
            Jobs.Add(job);
            Cancellations[id] = cts;
        }

        _ = Task.Run(() => RunProjectIndexJobAsync(projectRoot, job, cts.Token));
        PersistStatus(projectRoot, job);

        return "Jag börjar läsa och indexera projektet i bakgrunden. Du kan fortsätta skriva under tiden.\n\n" +
               "Jobb: " + id + "\n" +
               "Status: /jobb status\n" +
               "Avbryt: /jobb avbryt";
    }

    public static string FormatStatus()
    {
        BackgroundJobRecordV1? latest;
        lock (Gate)
            latest = Jobs.LastOrDefault();

        if (latest is null)
            return "Inga bakgrundsjobb har startats ännu.";

        return FormatJob(latest);
    }

    public static string FormatList()
    {
        List<BackgroundJobRecordV1> snapshot;
        lock (Gate)
            snapshot = Jobs.ToList();

        if (snapshot.Count == 0)
            return "Inga bakgrundsjobb har startats ännu.";

        return "Bakgrundsjobb:\n" + string.Join("\n", snapshot.Select(FormatJob));
    }

    public static string Cancel()
    {
        BackgroundJobRecordV1? job;
        CancellationTokenSource? cts;
        lock (Gate)
        {
            job = Jobs.LastOrDefault(j => j.State is BackgroundJobStateV1.Queued or BackgroundJobStateV1.Running);
            if (job is null)
                return "Det finns inget aktivt bakgrundsjobb att avbryta.";

            Cancellations.TryGetValue(job.Id, out cts);
            job.State = BackgroundJobStateV1.Cancelled;
            job.Message = "Avbryt begärt.";
            job.UpdatedAt = DateTime.Now;
        }

        cts?.Cancel();
        return "Avbryter bakgrundsjobb " + job.Id + ".";
    }

    private static async Task RunProjectIndexJobAsync(string projectRoot, BackgroundJobRecordV1 job, CancellationToken token)
    {
        try
        {
            Update(job, BackgroundJobStateV1.Running, "Skannar projektfiler.", 0, 0);
            PersistStatus(projectRoot, job);

            var result = await ProjectIndexServiceV1.BuildAsync(projectRoot, (processed, total, message) =>
            {
                Update(job, BackgroundJobStateV1.Running, message, processed, total);
                PersistStatus(projectRoot, job);
            }, token);

            Update(job, BackgroundJobStateV1.Completed, "Projektindex klart: " + result.FileCount + " filer.", result.FileCount, result.FileCount);
            job.ResultPath = result.IndexPath;
            PersistStatus(projectRoot, job);
        }
        catch (OperationCanceledException)
        {
            Update(job, BackgroundJobStateV1.Cancelled, "Projektindexering avbröts.", job.ProcessedItems, job.TotalItems);
            PersistStatus(projectRoot, job);
        }
        catch (Exception ex)
        {
            Update(job, BackgroundJobStateV1.Failed, "Projektindexering failade: " + ex.Message, job.ProcessedItems, job.TotalItems);
            PersistStatus(projectRoot, job);
        }
    }

    private static void Update(BackgroundJobRecordV1 job, BackgroundJobStateV1 state, string message, int processed, int total)
    {
        lock (Gate)
        {
            job.State = state;
            job.Message = message;
            job.ProcessedItems = processed;
            job.TotalItems = total;
            job.UpdatedAt = DateTime.Now;
        }
    }

    private static string FormatJob(BackgroundJobRecordV1 job)
    {
        var progress = job.TotalItems > 0 ? $"{job.ProcessedItems}/{job.TotalItems}" : $"{job.ProcessedItems}";
        return "- " + job.Id + " [" + job.State + "] " + job.Title + " (" + progress + ") - " + job.Message;
    }

    private static void PersistStatus(string projectRoot, BackgroundJobRecordV1 job)
    {
        try
        {
            var dir = Path.Combine(projectRoot, "data", "jobs", job.Id);
            Directory.CreateDirectory(dir);
            var statusPath = Path.Combine(dir, "status.json");
            var json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(statusPath, json);

            var historyDir = Path.Combine(projectRoot, "data", "jobs");
            Directory.CreateDirectory(historyDir);
            File.AppendAllText(Path.Combine(historyDir, "jobs.jsonl"), JsonSerializer.Serialize(job) + Environment.NewLine);
        }
        catch
        {
            // Job-status får aldrig krascha Jarvis-chatten.
        }
    }
}
