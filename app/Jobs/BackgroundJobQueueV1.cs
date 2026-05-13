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
    public string CurrentStep { get; set; } = string.Empty;
    public string LastAction { get; set; } = string.Empty;
    public string NextAction { get; set; } = string.Empty;
    public int ContextEstimateTokens { get; set; }
    public string ResultPath { get; set; } = string.Empty;
    public DateTime StartedAt { get; init; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

internal static class BackgroundJobPathsV1
{
    public static string ProjectRoot { get; set; } = "";
}

internal static class BackgroundJobQueueV1
{
    private static readonly object Gate = new();
    private static readonly List<BackgroundJobRecordV1> Jobs = new();
    private static readonly Dictionary<string, CancellationTokenSource> Cancellations = new();

    public static string StartProjectIndexJob(string projectRoot)
    {
        BackgroundJobPathsV1.ProjectRoot = projectRoot;
        var id = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var job = new BackgroundJobRecordV1
        {
            Id = id,
            Kind = "project-index",
            Title = "Project Index scan",
            Message = "Köad för projektindexering.",
            CurrentStep = "köad",
            LastAction = "Jobb skapat.",
            NextAction = "starta indexering"
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
               "Token/context: n/a tills jobbet börjar rapportera.\n" +
               "Status: /jobb status\n" +
               "Avbryt: /jobb avbryt";
    }

    public static string StartProjectAuditJob(string projectRoot)
    {
        BackgroundJobPathsV1.ProjectRoot = projectRoot;
        var id = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-audit";
        var job = new BackgroundJobRecordV1
        {
            Id = id,
            Kind = "project-audit",
            Title = "Project Audit",
            Message = "Köad för projekt-audit.",
            CurrentStep = "köad",
            LastAction = "Jobb skapat.",
            NextAction = "säkerställa projektindex"
        };

        var cts = new CancellationTokenSource();
        lock (Gate)
        {
            Jobs.Add(job);
            Cancellations[id] = cts;
        }

        _ = Task.Run(() => RunProjectAuditJobAsync(projectRoot, job, cts.Token));
        PersistStatus(projectRoot, job);
        AppendLog(projectRoot, job, "Audit-jobb köat.");

        return "Jag börjar skapa en projekt-audit i bakgrunden. Du kan fortsätta skriva under tiden.\n\n" +
               "Jobb: " + id + "\n" +
               "Token/context: n/a tills jobbet börjar rapportera.\n" +
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

    public static BackgroundJobRecordV1? LatestSnapshot()
    {
        lock (Gate)
            return Jobs.LastOrDefault();
    }

    public static string FormatMonitorLine()
    {
        var latest = LatestSnapshot();
        if (latest is null)
            return "Inga bakgrundsjobb startade.";

        var progress = latest.TotalItems > 0 ? $"{latest.ProcessedItems}/{latest.TotalItems}" : $"{latest.ProcessedItems}";
        var step = string.IsNullOrWhiteSpace(latest.CurrentStep) ? "bakgrundsjobb" : latest.CurrentStep;
        var next = string.IsNullOrWhiteSpace(latest.NextAction) ? "väntar" : latest.NextAction;
        return latest.Title + " [" + latest.State + "] " + progress + "\n" +
               "Steg: " + step + "\n" +
               "Nu: " + latest.Message + "\n" +
               "Nästa: " + next + "\n" +
               "Token/context: " + FormatContextEstimate(latest.ContextEstimateTokens);
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
                AppendLog(projectRoot, job, message);
            }, token);

            Update(job, BackgroundJobStateV1.Completed, "Projektindex klart: " + result.FileCount + " filer.", result.FileCount, result.FileCount);
            job.ResultPath = WriteJobResult(projectRoot, job,
                "# Project Index Result\n\n" +
                "- Index: `" + result.IndexPath + "`\n" +
                "- Search index: `" + result.SearchIndexPath + "`\n" +
                "- Filer: " + result.FileCount + "\n" +
                "- Hashade: " + result.HashedFileCount + "\n" +
                "- Återanvända: " + result.ReusedFileCount + "\n" +
                "- Borttagna sedan förra scan: " + result.DeletedFileCount + "\n");
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Projektindex klart.");
        }
        catch (OperationCanceledException)
        {
            Update(job, BackgroundJobStateV1.Cancelled, "Projektindexering avbröts.", job.ProcessedItems, job.TotalItems);
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Projektindexering avbröts.");
        }
        catch (Exception ex)
        {
            Update(job, BackgroundJobStateV1.Failed, "Projektindexering failade: " + ex.Message, job.ProcessedItems, job.TotalItems);
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Projektindexering failade: " + ex);
        }
    }

    private static async Task RunProjectAuditJobAsync(string projectRoot, BackgroundJobRecordV1 job, CancellationToken token)
    {
        try
        {
            Update(job, BackgroundJobStateV1.Running, "Säkerställer färskt projektindex.", 0, 2);
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Startar audit och säkerställer index.");

            await ProjectIndexServiceV1.BuildAsync(projectRoot, (processed, total, message) =>
            {
                Update(job, BackgroundJobStateV1.Running, "Index: " + message, processed, Math.Max(total, 1) + 1);
                PersistStatus(projectRoot, job);
            }, token);

            token.ThrowIfCancellationRequested();
            Update(job, BackgroundJobStateV1.Running, "Skriver audit-rapport.", 1, 2);
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Skriver audit-rapport.");

            var resultPath = Path.Combine(projectRoot, "data", "jobs", job.Id, "result.md");
            var audit = await ProjectAuditServiceV1.WriteAuditAsync(projectRoot, resultPath, token);

            Update(job, BackgroundJobStateV1.Completed, "Projekt-audit klar: " + audit.FileCount + " filer, " + audit.TodoCount + " TODO/riskmarkörer.", 2, 2);
            job.ResultPath = audit.ReportPath;
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Projekt-audit klar: " + audit.ReportPath);
        }
        catch (OperationCanceledException)
        {
            Update(job, BackgroundJobStateV1.Cancelled, "Projekt-audit avbröts.", job.ProcessedItems, job.TotalItems);
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Projekt-audit avbröts.");
        }
        catch (Exception ex)
        {
            Update(job, BackgroundJobStateV1.Failed, "Projekt-audit failade: " + ex.Message, job.ProcessedItems, job.TotalItems);
            PersistStatus(projectRoot, job);
            AppendLog(projectRoot, job, "Projekt-audit failade: " + ex);
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
            UpdateWorkStatus(job, state, message);
        }
    }

    private static void UpdateWorkStatus(BackgroundJobRecordV1 job, BackgroundJobStateV1 state, string message)
    {
        job.CurrentStep = InferCurrentStep(job.Kind, message);
        job.LastAction = string.IsNullOrWhiteSpace(message) ? job.LastAction : message;
        job.NextAction = InferNextAction(job.Kind, state, message);
        job.ContextEstimateTokens = ContextBudgetEstimatorV1.EstimateLocalJobContextTokens(
            job.Kind,
            job.ProcessedItems,
            message);
    }

    private static string InferCurrentStep(string kind, string message)
    {
        var lower = (message ?? string.Empty).ToLowerInvariant();

        if (lower.Contains("indexerar") || lower.Contains("skannar"))
            return "indexerar filer";
        if (lower.Contains("audit"))
            return "projekt-audit";
        if (lower.Contains("rapport"))
            return "skriver rapport";
        if (lower.Contains("klart"))
            return "klart";
        if (lower.Contains("avbr"))
            return "avbrutet";
        if (lower.Contains("fail"))
            return "fel";

        return kind switch
        {
            "project-audit" => "projekt-audit",
            "project-index" => "projektindex",
            _ => "bakgrundsjobb"
        };
    }

    private static string InferNextAction(string kind, BackgroundJobStateV1 state, string message)
    {
        if (state == BackgroundJobStateV1.Completed)
            return "visa resultat eller starta nästa uppgift";
        if (state == BackgroundJobStateV1.Cancelled)
            return "vänta på nytt kommando";
        if (state == BackgroundJobStateV1.Failed)
            return "läs logg och felsök";

        var lower = (message ?? string.Empty).ToLowerInvariant();
        if (lower.Contains("rapport"))
            return "spara result.md";
        if (kind == "project-audit")
            return "skriva audit-rapport";
        if (kind == "project-index")
            return "uppdatera sökindex";

        return "fortsätta jobbet";
    }

    private static string FormatJob(BackgroundJobRecordV1 job)
    {
        var progress = job.TotalItems > 0 ? $"{job.ProcessedItems}/{job.TotalItems}" : $"{job.ProcessedItems}";
        var result = string.IsNullOrWhiteSpace(job.ResultPath) ? "" : " Resultat: " + FormatPathForChat(job.ResultPath);
        var step = string.IsNullOrWhiteSpace(job.CurrentStep) ? "bakgrundsjobb" : job.CurrentStep;
        var next = string.IsNullOrWhiteSpace(job.NextAction) ? "fortsätta" : job.NextAction;
        return "- " + job.Id + " [" + job.State + "] " + job.Title + " (" + progress + ") - " + job.Message + result +
               "\n  Steg: " + step +
               " | Token/context: " + FormatContextEstimate(job.ContextEstimateTokens) +
               " | Nästa: " + next;
    }

    private static string FormatContextEstimate(int tokens)
    {
        return tokens <= 0 ? "n/a" : "ca " + ContextBudgetEstimatorV1.FormatCompact(tokens);
    }

    internal static string FormatPathForChat(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        var projectRoot = BackgroundJobPathsV1.ProjectRoot;
        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            try
            {
                var full = Path.GetFullPath(path);
                var root = Path.GetFullPath(projectRoot);
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    return Path.GetRelativePath(root, full).Replace('\\', '/');
            }
            catch
            {
                // Fall through to normalized path.
            }
        }

        return path.Replace('\\', '/');
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

    private static void AppendLog(string projectRoot, BackgroundJobRecordV1 job, string message)
    {
        try
        {
            var dir = Path.Combine(projectRoot, "data", "jobs", job.Id);
            Directory.CreateDirectory(dir);
            var line = "- " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + job.State + "] " + message + Environment.NewLine;
            File.AppendAllText(Path.Combine(dir, "log.md"), line);
        }
        catch
        {
            // Jobblogg får aldrig krascha Jarvis-chatten.
        }
    }

    private static string WriteJobResult(string projectRoot, BackgroundJobRecordV1 job, string markdown)
    {
        var dir = Path.Combine(projectRoot, "data", "jobs", job.Id);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "result.md");
        File.WriteAllText(path, markdown);
        return path;
    }
}
