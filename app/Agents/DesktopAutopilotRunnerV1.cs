namespace JarvisClean;

internal sealed record DesktopAutopilotRunV1(
    bool Ok,
    string Message,
    string Mission,
    string Instruction,
    int Step);

internal static class DesktopAutopilotRunnerV1
{
    // BroadDesktopControl runner: it may plan the next UI step, but it never
    // executes clicks/types itself. Program.cs turns Instruction into a
    // PendingApprovalV1 desktop-action through DesktopActionGate + UI-TARS.
    // kill-switch stays Ctrl+Shift+Alt+J or /autopilot stop.
    private const int MaxStepsPerMission = 12;
    private static readonly object Lock = new();
    private static string ActiveMission = "";
    private static int Step;

    private static readonly string[] BlockedTerms =
    {
        "login",
        "logga in",
        "password",
        "passord",
        "losenord",
        "betal",
        "kop",
        "checkout",
        "bankid",
        "bank-id",
        "secret",
        "api key",
        "token",
        "admin",
        "administrator",
        "powershell",
        "pwsh",
        "cmd",
        "terminal",
        "regedit",
        "registry",
        "taskmgr",
        "task manager",
        "credential",
        "delete",
        "radera",
        "format",
        "diskpart"
    };

    public static DesktopAutopilotRunV1 StartOrContinue(string mission)
    {
        mission = (mission ?? "").Trim();
        if (string.IsNullOrWhiteSpace(mission))
            return Block("Desktop Autopilot kraver ett tydligt uppdrag.");

        lock (Lock)
        {
            if (IsContinueAlias(mission) && !string.IsNullOrWhiteSpace(ActiveMission))
                mission = ActiveMission;

            if (IsBlockedMission(mission, out var blocked))
            {
                return Block(
                    "Desktop Autopilot blockerat: uppdraget innehaller kansligt steg (" + blocked + ").\n" +
                    "V1 stoppar vid login, betalning, secrets, admin/system/terminal och destruktiva steg.");
            }

            if (!mission.Equals(ActiveMission, StringComparison.OrdinalIgnoreCase))
            {
                ActiveMission = mission;
                Step = 0;
            }

            if (Step >= MaxStepsPerMission)
            {
                return Block(
                    "Desktop Autopilot pausad: max " + MaxStepsPerMission + " steg for samma uppdrag.\n" +
                    "Starta ett nytt tydligt uppdrag eller skriv /autopilot stop.");
            }

            Step += 1;
            var instruction = BuildVisionInstruction(ActiveMission, Step);
            return new DesktopAutopilotRunV1(
                true,
                "Desktop Autopilot Runner V1\n" +
                "Uppdrag: " + ActiveMission + "\n" +
                "Steg: " + Step + "/" + MaxStepsPerMission + "\n" +
                "Varje UI-action blir pending approval. Kill-switch: Ctrl+Shift+Alt+J eller /autopilot stop.",
                ActiveMission,
                instruction,
                Step);
        }
    }

    public static void Stop()
    {
        lock (Lock)
        {
            ActiveMission = "";
            Step = 0;
        }
    }

    public static string BuildVisionInstruction(string mission, int step)
    {
        mission = (mission ?? "").Trim();
        return
            "Desktop Autopilot Runner V1\n" +
            "Mission: " + mission + "\n" +
            "Step: " + Math.Max(1, step) + "/" + MaxStepsPerMission + "\n" +
            "You control BroadDesktopControl only by proposing ONE next desktop UI action.\n" +
            "Do not perform login, payment, password/secret entry, admin/system changes, terminal commands, deletion or publishing.\n" +
            "If the task is already complete, return finished().\n" +
            "Otherwise return exactly one safe UI action for PendingApprovalV1 through DesktopActionGate.";
    }

    public static bool IsBlockedMission(string mission, out string blockedTerm)
    {
        var lower = (mission ?? "").ToLowerInvariant();
        blockedTerm = BlockedTerms.FirstOrDefault(term => lower.Contains(term)) ?? "";
        return !string.IsNullOrWhiteSpace(blockedTerm);
    }

    private static bool IsContinueAlias(string mission)
    {
        var value = NormalizeContinueAlias(mission);
        return value is "fortsatt" or "fortsatt steg" or "continue" or "next" or "nasta" or "nasta steg";
    }

    private static string NormalizeContinueAlias(string mission)
    {
        var value = (mission ?? "").Trim().ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");
        return string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static DesktopAutopilotRunV1 Block(string message)
    {
        return new DesktopAutopilotRunV1(false, message, "", "", 0);
    }
}
