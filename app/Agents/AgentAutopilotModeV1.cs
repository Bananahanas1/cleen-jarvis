namespace JarvisClean;

internal enum AgentAutopilotLevelV1
{
    Safe,
    Approval,
    BrowserAutopilot,
    DesktopAutopilot,
    BuildAgent
}

internal sealed record AgentAutopilotStateV1(
    AgentAutopilotLevelV1 Level,
    string Mission,
    DateTime StartedAt,
    DateTime UpdatedAt);

internal static class AgentAutopilotModeV1
{
    // Safety anchors: BrowserPolicyV1, F:\New project read-only, kill-switch,
    // betalningar/secrets/publicering stoppas av scope-reglerna.
    // Desktop mode is BroadDesktopControl: nastan alla normala appar, with denylist.
    private static readonly object Lock = new();
    private static AgentAutopilotStateV1 Current = new(
        AgentAutopilotLevelV1.Safe,
        "",
        DateTime.Now,
        DateTime.Now);

    public static AgentAutopilotStateV1 State
    {
        get
        {
            lock (Lock)
                return Current;
        }
    }

    public static bool TryParseLevel(string raw, out AgentAutopilotLevelV1 level)
    {
        var value = Normalize(raw);
        level = AgentAutopilotLevelV1.Safe;

        if (value is "" or "status")
            return false;

        if (value is "safe" or "saker" or "säker" or "niva 1" or "nivå 1" or "1")
        {
            level = AgentAutopilotLevelV1.Safe;
            return true;
        }

        if (value is "approval" or "godkannande" or "godkännande" or "approve" or "niva 2" or "nivå 2" or "2")
        {
            level = AgentAutopilotLevelV1.Approval;
            return true;
        }

        if (value is "browser" or "browserautopilot" or "browser autopilot" or "webb" or "opera" or "niva 3" or "nivå 3" or "3")
        {
            level = AgentAutopilotLevelV1.BrowserAutopilot;
            return true;
        }

        if (value is "desktop" or "desktopautopilot" or "desktop autopilot" or "dator" or "niva 4" or "nivå 4" or "4")
        {
            level = AgentAutopilotLevelV1.DesktopAutopilot;
            return true;
        }

        if (value is "build" or "buildagent" or "build agent" or "kod" or "bygg" or "niva 5" or "nivå 5" or "5")
        {
            level = AgentAutopilotLevelV1.BuildAgent;
            return true;
        }

        return false;
    }

    public static string SetMode(AgentAutopilotLevelV1 level, string mission, out bool changed)
    {
        mission = (mission ?? "").Trim();
        changed = false;

        if (RequiresMission(level) && string.IsNullOrWhiteSpace(mission))
            return "Autopilot-uppdrag saknas. Skriv t.ex. /autopilot browser hitta dokumentation om WebView2.";

        lock (Lock)
        {
            Current = new AgentAutopilotStateV1(level, mission, DateTime.Now, DateTime.Now);
            changed = true;
        }

        return "Autopilot-läge ändrat till: " + Label(level) + "\n\n" +
               DescribeLevel(level) +
               (string.IsNullOrWhiteSpace(mission) ? "" : "\n\nUppdrag: " + mission) +
               "\n\nKill-switch: Ctrl+Shift+Alt+J eller /autopilot stop.";
    }

    public static string Stop(out AgentAutopilotStateV1 previous)
    {
        lock (Lock)
        {
            previous = Current;
            Current = new AgentAutopilotStateV1(
                AgentAutopilotLevelV1.Safe,
                "",
                DateTime.Now,
                DateTime.Now);
        }

        return "Autopilot stoppad. Läge: Safe. Pending desktop-action rensas av Jarvis om en sådan finns.";
    }

    public static string Status()
    {
        var state = State;
        return "Agent Autopilot Modes V1\n" +
               "Aktivt läge: " + Label(state.Level) + "\n" +
               "Uppdrag: " + (string.IsNullOrWhiteSpace(state.Mission) ? "(inget aktivt uppdrag)" : state.Mission) + "\n" +
               "Startad: " + state.StartedAt.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n" +
               DescribeLevel(state.Level) + "\n\n" +
               "Nivåer:\n" +
               "- Nivå 1 Safe: läs, sök, analysera och öppna säkra filer.\n" +
               "- Nivå 2 Approval: föreslå click/type/terminal/file-write; varje riskmoment kräver pending approval.\n" +
               "- Nivå 3 Browser Autopilot: " + BrowserPolicyV1.DisplayName + " synligt, " + BrowserPolicyV1.InternalAutomationEngine + " internt.\n" +
               "- Nivå 4 Desktop Autopilot: BroadDesktopControl för nastan alla normala appar, med denylist och kill-switch.\n" +
               "- Nivå 5 Build Agent: får jobba i F:\\Jarvis-clean men aldrig skriva i F:\\New project eller hela F-disken.";
    }

    public static string OverviewLine()
    {
        var state = State;
        var mission = string.IsNullOrWhiteSpace(state.Mission) ? "(inget uppdrag)" : Limit(state.Mission, 120);
        return Label(state.Level) + "\n" + mission + "\n" + SafetyLine(state.Level);
    }

    public static bool RequiresMission(AgentAutopilotLevelV1 level)
    {
        return level is AgentAutopilotLevelV1.BrowserAutopilot
            or AgentAutopilotLevelV1.DesktopAutopilot
            or AgentAutopilotLevelV1.BuildAgent;
    }

    public static string Label(AgentAutopilotLevelV1 level)
    {
        return level switch
        {
            AgentAutopilotLevelV1.Safe => "Nivå 1 Safe",
            AgentAutopilotLevelV1.Approval => "Nivå 2 Approval",
            AgentAutopilotLevelV1.BrowserAutopilot => "Nivå 3 Browser Autopilot",
            AgentAutopilotLevelV1.DesktopAutopilot => "Nivå 4 Desktop Autopilot",
            AgentAutopilotLevelV1.BuildAgent => "Nivå 5 Build Agent",
            _ => "Safe"
        };
    }

    public static string DescribeLevel(AgentAutopilotLevelV1 level)
    {
        return level switch
        {
            AgentAutopilotLevelV1.Safe =>
                "Safe: Jarvis läser, söker, analyserar och öppnar filer. Ingen fri kontroll.",
            AgentAutopilotLevelV1.Approval =>
                "Approval: Jarvis får föreslå click/type/terminal/file-write, men varje riskmoment går via PendingApprovalV1.",
            AgentAutopilotLevelV1.BrowserAutopilot =>
                "Browser Autopilot: synlig browser är " + BrowserPolicyV1.DisplayName + ". Intern automation får använda " +
                BrowserPolicyV1.InternalAutomationEngine + ". Stoppar vid login, betalningar, secrets, skickande och publicering.",
            AgentAutopilotLevelV1.DesktopAutopilot =>
                "Desktop Autopilot: BroadDesktopControl för nastan alla normala appar. Det är inte en liten whitelist; en denylist blockerar admin/system/credential/password/betalningar och kill-switch finns.",
            AgentAutopilotLevelV1.BuildAgent =>
                "Build Agent: Jarvis får arbeta i F:\\Jarvis-clean med kod/build/test. F:\\New project är read-only reference och hela F-disken får inte bli fri skrivyta.",
            _ => ""
        };
    }

    private static string SafetyLine(AgentAutopilotLevelV1 level)
    {
        return level switch
        {
            AgentAutopilotLevelV1.Safe => "Ingen automation aktiv.",
            AgentAutopilotLevelV1.Approval => "Riskmoment kräver pending approval.",
            AgentAutopilotLevelV1.BrowserAutopilot => "OperaGX/Opera only; stoppar vid känsliga steg.",
            AgentAutopilotLevelV1.DesktopAutopilot => "Broad desktop med denylist och Ctrl+Shift+Alt+J.",
            AgentAutopilotLevelV1.BuildAgent => "Scope: F:\\Jarvis-clean, build/test via säkra flöden.",
            _ => ""
        };
    }

    private static string Normalize(string value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o");
        return string.Join(" ", normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Limit(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return clean.Length <= max ? clean : clean[..Math.Max(0, max - 3)] + "...";
    }
}
