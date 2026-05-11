using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JarvisClean;

internal enum DesktopActionKindV1
{
    Click,
    DoubleClick,
    RightClick,
    Hover,
    Drag,
    TypeText,
    Hotkey,
    Scroll,
    Wait,
    Finished
}

internal sealed record DesktopActionRequestV1
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DesktopActionKindV1 Kind { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int EndX { get; init; }
    public int EndY { get; init; }
    public string Text { get; init; } = string.Empty;
    public string KeyChord { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public int Amount { get; init; } = 1;
    public string Source { get; init; } = "manual";
    public string Instruction { get; init; } = string.Empty;
    public string ScreenshotPath { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string RawPrediction { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static DesktopActionRequestV1? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DesktopActionRequestV1>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public string Preview()
    {
        var target = Kind switch
        {
            DesktopActionKindV1.Click => $"klick @ ({X}, {Y})",
            DesktopActionKindV1.DoubleClick => $"dubbelklick @ ({X}, {Y})",
            DesktopActionKindV1.RightClick => $"högerklick @ ({X}, {Y})",
            DesktopActionKindV1.Hover => $"flytta mus @ ({X}, {Y})",
            DesktopActionKindV1.Drag => $"drag ({X}, {Y}) -> ({EndX}, {EndY})",
            DesktopActionKindV1.TypeText => $"skriv text ({Text.Length} tecken)",
            DesktopActionKindV1.Hotkey => $"hotkey: {KeyChord}",
            DesktopActionKindV1.Scroll => $"scroll {Direction} x{Math.Max(1, Amount)}",
            DesktopActionKindV1.Wait => $"vänta {Math.Max(1, Amount)} ms",
            DesktopActionKindV1.Finished => "markera desktop-task som klar",
            _ => Kind.ToString()
        };

        var lines = new List<string>
        {
            "Desktop-action preview:",
            "- Åtgärd: " + target,
            "- Källa: " + Source,
            "- Skapad: " + CreatedAt
        };

        if (!string.IsNullOrWhiteSpace(Instruction))
            lines.Add("- Instruktion: " + Instruction);
        if (!string.IsNullOrWhiteSpace(ScreenshotPath))
            lines.Add("- Screenshot: " + ScreenshotPath);
        if (Confidence > 0)
            lines.Add("- Confidence: " + Confidence.ToString("0.00"));
        if (Kind == DesktopActionKindV1.TypeText)
            lines.Add("- Text: " + Text);
        if (!string.IsNullOrWhiteSpace(RawPrediction))
            lines.Add("- UI-TARS prediction: " + RawPrediction);

        return string.Join(Environment.NewLine, lines);
    }

    public static bool TryCreateManual(string action, string payload, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        action = Normalize(action);
        payload = (payload ?? "").Trim();

        if (action is "klick" or "click")
            return TryPoint(payload, DesktopActionKindV1.Click, out request, out error);
        if (action is "dubbelklick" or "doubleclick" or "double-click")
            return TryPoint(payload, DesktopActionKindV1.DoubleClick, out request, out error);
        if (action is "hogerklick" or "högerklick" or "rightclick" or "right-click")
            return TryPoint(payload, DesktopActionKindV1.RightClick, out request, out error);
        if (action is "hovra" or "hover" or "flytta")
            return TryPoint(payload, DesktopActionKindV1.Hover, out request, out error);
        if (action is "drag" or "dra")
            return TryDrag(payload, out request, out error);
        if (action is "skriv" or "typ" or "type")
            return TryType(payload, out request, out error);
        if (action is "hotkey" or "tangent" or "kortkommando")
            return TryHotkey(payload, out request, out error);
        if (action is "scroll" or "skroll")
            return TryScroll(payload, out request, out error);

        error = "Okänd desktop-action. Exempel: klick, dubbelklick, högerklick, drag, skriv, hotkey, scroll.";
        return false;
    }

    public static bool TryParsePrediction(
        string prediction,
        int screenWidth,
        int screenHeight,
        string instruction,
        string screenshotPath,
        out DesktopActionRequestV1 request,
        out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        var raw = (prediction ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "UI-TARS returnerade tom action.";
            return false;
        }

        var lower = raw.ToLowerInvariant();
        var source = "ui-tars";

        if (lower.Contains("finished(") || lower == "finished")
        {
            request = new DesktopActionRequestV1
            {
                Kind = DesktopActionKindV1.Finished,
                Source = source,
                Instruction = instruction,
                ScreenshotPath = screenshotPath,
                RawPrediction = raw
            };
            return true;
        }

        if (lower.Contains("type("))
        {
            var text = ExtractNamedString(raw, "content") ?? ExtractNamedString(raw, "text") ?? "";
            return BuildPredictionText(text, source, instruction, screenshotPath, raw, out request, out error);
        }

        if (lower.Contains("hotkey("))
        {
            var key = ExtractNamedString(raw, "key") ?? ExtractNamedString(raw, "keys") ?? "";
            return BuildPredictionHotkey(key, source, instruction, screenshotPath, raw, out request, out error);
        }

        if (lower.Contains("scroll("))
        {
            var direction = ExtractNamedString(raw, "direction") ?? "down";
            var amountText = ExtractNamedString(raw, "amount") ?? "1";
            _ = int.TryParse(amountText, out var amount);
            request = new DesktopActionRequestV1
            {
                Kind = DesktopActionKindV1.Scroll,
                Direction = Normalize(direction) is "up" or "upp" ? "up" : "down",
                Amount = Math.Clamp(amount <= 0 ? 1 : amount, 1, 10),
                Source = source,
                Instruction = instruction,
                ScreenshotPath = screenshotPath,
                RawPrediction = raw
            };
            return true;
        }

        if (lower.Contains("drag("))
        {
            var points = ExtractPoints(raw);
            if (points.Count < 2)
            {
                error = "UI-TARS drag saknar start/end-koordinater.";
                return false;
            }

            var start = NormalizePoint(points[0], screenWidth, screenHeight);
            var end = NormalizePoint(points[1], screenWidth, screenHeight);
            request = new DesktopActionRequestV1
            {
                Kind = DesktopActionKindV1.Drag,
                X = start.x,
                Y = start.y,
                EndX = end.x,
                EndY = end.y,
                Source = source,
                Instruction = instruction,
                ScreenshotPath = screenshotPath,
                RawPrediction = raw
            };
            return true;
        }

        var clickKind =
            lower.Contains("double_click") || lower.Contains("doubleclick") ? DesktopActionKindV1.DoubleClick :
            lower.Contains("right_click") || lower.Contains("rightclick") ? DesktopActionKindV1.RightClick :
            lower.Contains("hover") ? DesktopActionKindV1.Hover :
            DesktopActionKindV1.Click;

        if (lower.Contains("click(") || lower.Contains("hover(") || lower.Contains("tap("))
        {
            var points = ExtractPoints(raw);
            if (points.Count == 0)
            {
                error = "UI-TARS action saknar koordinater.";
                return false;
            }

            var point = NormalizePoint(points[0], screenWidth, screenHeight);
            request = new DesktopActionRequestV1
            {
                Kind = clickKind,
                X = point.x,
                Y = point.y,
                Source = source,
                Instruction = instruction,
                ScreenshotPath = screenshotPath,
                RawPrediction = raw
            };
            return true;
        }

        error = "UI-TARS action kunde inte tolkas: " + raw;
        return false;
    }

    private static bool TryPoint(string payload, DesktopActionKindV1 kind, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        var nums = Numbers(payload);
        if (nums.Count < 2)
        {
            error = "Skriv koordinater: /desktop klick 100 200";
            return false;
        }

        request = new DesktopActionRequestV1 { Kind = kind, X = nums[0], Y = nums[1] };
        return true;
    }

    private static bool TryDrag(string payload, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        var nums = Numbers(payload);
        if (nums.Count < 4)
        {
            error = "Skriv: /desktop drag 100 200 300 400";
            return false;
        }

        request = new DesktopActionRequestV1 { Kind = DesktopActionKindV1.Drag, X = nums[0], Y = nums[1], EndX = nums[2], EndY = nums[3] };
        return true;
    }

    private static bool TryType(string payload, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Skriv: /desktop skriv texten som ska matas in";
            return false;
        }

        request = new DesktopActionRequestV1 { Kind = DesktopActionKindV1.TypeText, Text = payload };
        return true;
    }

    private static bool TryHotkey(string payload, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Skriv: /desktop hotkey ctrl+l";
            return false;
        }

        request = new DesktopActionRequestV1 { Kind = DesktopActionKindV1.Hotkey, KeyChord = payload.Trim() };
        return true;
    }

    private static bool TryScroll(string payload, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";

        var parts = payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var direction = parts.Length > 0 ? Normalize(parts[0]) : "down";
        var amount = 1;
        if (parts.Length > 1)
            _ = int.TryParse(parts[1], out amount);

        if (direction is not ("up" or "upp" or "down" or "ner" or "ned"))
        {
            error = "Scroll-riktning måste vara up/upp eller down/ner.";
            return false;
        }

        request = new DesktopActionRequestV1
        {
            Kind = DesktopActionKindV1.Scroll,
            Direction = direction is "up" or "upp" ? "up" : "down",
            Amount = Math.Clamp(amount <= 0 ? 1 : amount, 1, 10)
        };
        return true;
    }

    private static bool BuildPredictionText(string text, string source, string instruction, string screenshotPath, string raw, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "UI-TARS type-action saknar content.";
            return false;
        }

        request = new DesktopActionRequestV1
        {
            Kind = DesktopActionKindV1.TypeText,
            Text = text,
            Source = source,
            Instruction = instruction,
            ScreenshotPath = screenshotPath,
            RawPrediction = raw
        };
        return true;
    }

    private static bool BuildPredictionHotkey(string key, string source, string instruction, string screenshotPath, string raw, out DesktopActionRequestV1 request, out string error)
    {
        request = new DesktopActionRequestV1();
        error = "";
        if (string.IsNullOrWhiteSpace(key))
        {
            error = "UI-TARS hotkey-action saknar key.";
            return false;
        }

        request = new DesktopActionRequestV1
        {
            Kind = DesktopActionKindV1.Hotkey,
            KeyChord = key,
            Source = source,
            Instruction = instruction,
            ScreenshotPath = screenshotPath,
            RawPrediction = raw
        };
        return true;
    }

    private static List<(double x, double y)> ExtractPoints(string raw)
    {
        var points = new List<(double x, double y)>();
        foreach (Match match in Regex.Matches(raw, @"\(([-+]?\d+(?:\.\d+)?)\s*,\s*([-+]?\d+(?:\.\d+)?)(?:\s*,\s*[-+]?\d+(?:\.\d+)?\s*,\s*[-+]?\d+(?:\.\d+)?)?\)"))
        {
            if (double.TryParse(match.Groups[1].Value, out var x) &&
                double.TryParse(match.Groups[2].Value, out var y))
                points.Add((x, y));
        }

        return points;
    }

    private static (int x, int y) NormalizePoint((double x, double y) point, int width, int height)
    {
        var x = (int)Math.Round(point.x);
        var y = (int)Math.Round(point.y);
        return (Math.Clamp(x, 0, Math.Max(0, width - 1)), Math.Clamp(y, 0, Math.Max(0, height - 1)));
    }

    private static string? ExtractNamedString(string raw, string name)
    {
        var match = Regex.Match(raw, name + @"\s*=\s*(['""])(?<v>.*?)\1", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups["v"].Value : null;
    }

    private static List<int> Numbers(string raw) =>
        Regex.Matches(raw ?? "", @"-?\d+")
            .Select(m => int.Parse(m.Value))
            .ToList();

    private static string Normalize(string value) =>
        string.Join(" ", (value ?? "").Trim().ToLowerInvariant()
            .Replace("å", "a")
            .Replace("ä", "a")
            .Replace("ö", "o")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
