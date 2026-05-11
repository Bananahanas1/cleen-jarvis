using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JarvisClean;

internal static class DesktopActionExecutor
{
    private const uint MouseMove = 0x0001;
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;
    private const uint RightDown = 0x0008;
    private const uint RightUp = 0x0010;
    private const uint Wheel = 0x0800;

    public static string Execute(DesktopActionRequestV1 action)
    {
        if (!DesktopActionGate.CanExecute(out var gateError))
            return gateError;

        if (!Validate(action, out var validationError))
            return validationError;

        try
        {
            switch (action.Kind)
            {
                case DesktopActionKindV1.Click:
                    Move(action.X, action.Y);
                    MouseClick(LeftDown, LeftUp);
                    break;

                case DesktopActionKindV1.DoubleClick:
                    Move(action.X, action.Y);
                    MouseClick(LeftDown, LeftUp);
                    Thread.Sleep(80);
                    MouseClick(LeftDown, LeftUp);
                    break;

                case DesktopActionKindV1.RightClick:
                    Move(action.X, action.Y);
                    MouseClick(RightDown, RightUp);
                    break;

                case DesktopActionKindV1.Hover:
                    Move(action.X, action.Y);
                    break;

                case DesktopActionKindV1.Drag:
                    Drag(action.X, action.Y, action.EndX, action.EndY);
                    break;

                case DesktopActionKindV1.TypeText:
                    SendKeys.SendWait(EscapeSendKeys(action.Text));
                    break;

                case DesktopActionKindV1.Hotkey:
                    SendKeys.SendWait(ToSendKeysChord(action.KeyChord));
                    break;

                case DesktopActionKindV1.Scroll:
                    var delta = (action.Direction.Equals("up", StringComparison.OrdinalIgnoreCase) ? 120 : -120) * Math.Max(1, action.Amount);
                    mouse_event(Wheel, 0, 0, delta, UIntPtr.Zero);
                    break;

                case DesktopActionKindV1.Wait:
                    Thread.Sleep(Math.Clamp(action.Amount, 1, 5000));
                    break;

                case DesktopActionKindV1.Finished:
                    break;
            }

            DesktopActionGate.RecordAction(action);
            return "Desktop-action körd: " + action.Preview();
        }
        catch (Exception ex)
        {
            return "Desktop-action misslyckades: " + ex.Message;
        }
    }

    private static bool Validate(DesktopActionRequestV1 action, out string error)
    {
        error = "";

        var screen = Screen.PrimaryScreen;
        if (screen is null)
        {
            error = "Ingen primär skärm hittades.";
            return false;
        }

        var bounds = screen.Bounds;
        bool PointOk(int x, int y) => x >= bounds.Left && y >= bounds.Top && x < bounds.Right && y < bounds.Bottom;

        if (action.Kind is DesktopActionKindV1.Click or DesktopActionKindV1.DoubleClick or DesktopActionKindV1.RightClick or DesktopActionKindV1.Hover)
        {
            if (!PointOk(action.X, action.Y))
            {
                error = "Koordinater utanför skärmen: " + action.X + ", " + action.Y;
                return false;
            }
        }

        if (action.Kind == DesktopActionKindV1.Drag)
        {
            if (!PointOk(action.X, action.Y) || !PointOk(action.EndX, action.EndY))
            {
                error = "Drag-koordinater utanför skärmen.";
                return false;
            }
        }

        if (action.Kind == DesktopActionKindV1.TypeText && action.Text.Length > 500)
        {
            error = "Blockerat: desktop-typing max 500 tecken per approval.";
            return false;
        }

        if (action.Kind == DesktopActionKindV1.Hotkey && string.IsNullOrWhiteSpace(action.KeyChord))
        {
            error = "Hotkey saknas.";
            return false;
        }

        return true;
    }

    private static void Move(int x, int y)
    {
        _ = SetCursorPos(x, y);
        Thread.Sleep(50);
    }

    private static void MouseClick(uint down, uint up)
    {
        mouse_event(down, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(45);
        mouse_event(up, 0, 0, 0, UIntPtr.Zero);
    }

    private static void Drag(int x, int y, int endX, int endY)
    {
        Move(x, y);
        mouse_event(LeftDown, 0, 0, 0, UIntPtr.Zero);
        for (var i = 1; i <= 12; i++)
        {
            var nx = x + ((endX - x) * i / 12);
            var ny = y + ((endY - y) * i / 12);
            _ = SetCursorPos(nx, ny);
            Thread.Sleep(20);
        }
        mouse_event(LeftUp, 0, 0, 0, UIntPtr.Zero);
    }

    private static string EscapeSendKeys(string text)
    {
        return (text ?? "")
            .Replace("{", "{{}")
            .Replace("}", "{}}")
            .Replace("+", "{+}")
            .Replace("^", "{^}")
            .Replace("%", "{%}")
            .Replace("~", "{~}")
            .Replace("(", "{(}")
            .Replace(")", "{)}")
            .Replace("[", "{[}")
            .Replace("]", "{]}");
    }

    private static string ToSendKeysChord(string chord)
    {
        var parts = (chord ?? "")
            .ToLowerInvariant()
            .Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var prefix = "";
        var key = "";

        foreach (var part in parts)
        {
            if (part is "ctrl" or "control") prefix += "^";
            else if (part is "alt") prefix += "%";
            else if (part is "shift") prefix += "+";
            else key = part;
        }

        if (string.IsNullOrWhiteSpace(key))
            return prefix;

        return key switch
        {
            "enter" => prefix + "{ENTER}",
            "tab" => prefix + "{TAB}",
            "esc" or "escape" => prefix + "{ESC}",
            "backspace" => prefix + "{BACKSPACE}",
            "delete" or "del" => prefix + "{DELETE}",
            "space" => prefix + " ",
            "left" => prefix + "{LEFT}",
            "right" => prefix + "{RIGHT}",
            "up" => prefix + "{UP}",
            "down" => prefix + "{DOWN}",
            _ when key.Length == 1 => prefix + key,
            _ => prefix + "{" + key.ToUpperInvariant() + "}"
        };
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);
}
