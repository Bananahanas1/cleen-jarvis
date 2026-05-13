using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace JarvisClean;

internal static class DesktopActionGate
{
    private const int MaxActionsPerMinute = 30;
    private const int MinMillisecondsBetweenActions = 200;
    private static readonly object Lock = new();
    private static readonly List<DateTime> ActionTimes = new();
    private static DateTime LastActionAt = DateTime.MinValue;

    public static bool Enabled { get; private set; }

    public static string Enable()
    {
        Enabled = true;
        Log("ENABLE", "desktop-control enabled");
        return "Desktop-control PÅ. Alla klick/typ/scroll-actions kräver fortfarande pending approval.";
    }

    public static string Disable()
    {
        Enabled = false;
        Log("DISABLE", "desktop-control disabled");
        return "Desktop-control AV. Inga desktop-actions kommer köras.";
    }

    public static string Status()
    {
        var foreground = GetForegroundDescription();
        return "Desktop-control: " + (Enabled ? "PÅ" : "AV") + "\n" +
               "UI-actions kräver pending approval.\n" +
               "BroadDesktopControl: styr nastan alla normala appar; denylist blockerar admin/system/credential/password.\n" +
               "Rate limit: " + MaxActionsPerMinute + "/min, minst " + MinMillisecondsBetweenActions + " ms mellan actions.\n" +
               "Foreground: " + foreground;
    }

    public static bool CanExecute(out string error)
    {
        error = "";

        if (!Enabled)
        {
            error = "Desktop-control är AV. Starta med: /desktop på";
            return false;
        }

        if (IsForegroundBlocked(out var blocked))
        {
            error = "Blockerat: foreground-fönstret är spärrat för desktop-control (" + blocked + ").";
            return false;
        }

        lock (Lock)
        {
            var now = DateTime.Now;
            ActionTimes.RemoveAll(t => (now - t).TotalSeconds > 60);

            if (ActionTimes.Count >= MaxActionsPerMinute)
            {
                error = "Rate limit: max " + MaxActionsPerMinute + " desktop-actions per minut.";
                return false;
            }

            if ((now - LastActionAt).TotalMilliseconds < MinMillisecondsBetweenActions)
            {
                error = "Rate limit: vänta minst " + MinMillisecondsBetweenActions + " ms mellan desktop-actions.";
                return false;
            }
        }

        return true;
    }

    public static void RecordAction(DesktopActionRequestV1 action)
    {
        lock (Lock)
        {
            var now = DateTime.Now;
            LastActionAt = now;
            ActionTimes.Add(now);
        }

        Log("ACTION", action.Kind + " " + action.Preview().Replace(Environment.NewLine, " | "));
    }

    public static void Log(string kind, string message)
    {
        try
        {
            var dir = Path.Combine(@"F:\Jarvis-clean", "data");
            Directory.CreateDirectory(dir);
            var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + kind + "  " + message;
            File.AppendAllText(Path.Combine(dir, "desktop_actions.log"), line + Environment.NewLine);
        }
        catch { }
    }

    private static bool IsForegroundBlocked(out string reason)
    {
        reason = "";
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return false;

        var title = GetWindowTitle(hwnd);
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        var processName = "";
        try { processName = Process.GetProcessById((int)pid).ProcessName; } catch { }

        var combined = (processName + " " + title).ToLowerInvariant();
        var blocked = new[]
        {
            "taskmgr",
            "registry editor",
            "regedit",
            "cmd",
            "powershell",
            "pwsh",
            "windows terminal",
            "administrator:",
            "credential",
            "password"
        };

        var hit = blocked.FirstOrDefault(b => combined.Contains(b));
        if (hit is null)
            return false;

        reason = string.IsNullOrWhiteSpace(processName) ? hit : processName + " / " + title;
        return true;
    }

    private static string GetForegroundDescription()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return "(okänt)";

        var title = GetWindowTitle(hwnd);
        _ = GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            var process = Process.GetProcessById((int)pid);
            return process.ProcessName + (string.IsNullOrWhiteSpace(title) ? "" : " / " + title);
        }
        catch
        {
            return string.IsNullOrWhiteSpace(title) ? "(okänt)" : title;
        }
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        _ = GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
