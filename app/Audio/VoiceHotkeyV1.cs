using System.Runtime.InteropServices;

namespace JarvisClean;

internal static class VoiceHotkeyV1
{
    public const int HotkeyId = 0x4A02; // unik från DesktopKillHotkeyIdV1 (0x4A01)
    public const int WmHotkey = 0x0312;
    public const uint ModControl = 0x0002;
    public const uint ModNoRepeat = 0x4000; // hindrar key-auto-repeat från att spamma WM_HOTKEY
    public const uint VkSpace = 0x20; // Win32 Virtual-Key code för Space

    public static event Action? PttPressed;

    private static bool _registered;

    public static bool IsRegistered => _registered;

    public static void Register(IntPtr handle)
    {
        if (_registered) return;
        try
        {
            _registered = RegisterHotKey(handle, HotkeyId, ModControl | ModNoRepeat, VkSpace);
        }
        catch
        {
            _registered = false;
        }
    }

    public static void Unregister(IntPtr handle)
    {
        if (!_registered) return;
        try { UnregisterHotKey(handle, HotkeyId); } catch { }
        _registered = false;
    }

    public static bool TryHandleWmHotkey(int message, long wParam)
    {
        if (message != WmHotkey) return false;
        // wParam för WM_HOTKEY = hotkey ID (lågt heltal). Använd long-jämförelse så vi aldrig overflowar.
        if (wParam != HotkeyId) return false;
        PttPressed?.Invoke();
        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
