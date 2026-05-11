using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Diagnostics;
using System.Text.Json;

namespace JarvisClean;

internal sealed record UiTarsVisionResultV1(bool Ok, DesktopActionRequestV1? Action, string Message);

internal sealed class UiTarsBridge
{
    private const string UiTarsRoot = @"F:\UI-TARS-desktop-main";
    private const string ConfigPath = @"F:\Jarvis-clean\config\uitars.json";
    private Process? _process;

    public bool IsRunning => _process is not null && !_process.HasExited;

    public bool IsAvailable() =>
        Directory.Exists(UiTarsRoot) &&
        File.Exists(Path.Combine(UiTarsRoot, "apps", "ui-tars", "package.json"));

    public string Status()
    {
        var config = LoadConfig();
        return "UI-TARS bridge:\n" +
               "- Local source: " + (IsAvailable() ? "hittad" : "saknas") + " (" + UiTarsRoot + ")\n" +
               "- UI-TARS Desktop process: " + (IsRunning ? "kör" : "stoppad") + "\n" +
               "- Vision API config: " + (config is null ? "saknas" : "finns") + "\n" +
               "- Desktop-control: " + (DesktopActionGate.Enabled ? "PÅ" : "AV") + "\n" +
               "- Action space: click, double_click, right_click, drag, hover, type, hotkey, scroll, finished\n" +
               "- Config: env JARVIS_UITARS_BASE_URL/JARVIS_UITARS_API_KEY/JARVIS_UITARS_MODEL eller config\\uitars.json";
    }

    public async Task<string> StartAsync()
    {
        if (!IsAvailable())
            return "UI-TARS source saknas: " + UiTarsRoot;

        if (IsRunning)
            return "UI-TARS Desktop verkar redan köras (PID " + _process!.Id + ").";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pnpm.cmd",
                Arguments = "--dir \"" + UiTarsRoot + "\" --filter ui-tars-desktop start",
                WorkingDirectory = UiTarsRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _process = Process.Start(psi);
            if (_process is null)
                return "Kunde inte starta UI-TARS Desktop-processen.";

            await Task.Delay(1500);
            return "UI-TARS Desktop startad som subprocess (PID " + _process.Id + "). Om dependencies saknas, kör pnpm install i UI-TARS-projektet manuellt.";
        }
        catch (Exception ex)
        {
            return "Kunde inte starta UI-TARS Desktop. Kontrollera att pnpm finns på PATH. Fel: " + ex.Message;
        }
    }

    public async Task<string> StopAsync()
    {
        try
        {
            if (_process is not null && !_process.HasExited)
            {
                var pid = _process.Id;
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                _process = null;
                return "UI-TARS Desktop subprocess stoppad (PID " + pid + ").";
            }
        }
        catch (Exception ex)
        {
            return "Kunde inte stoppa UI-TARS Desktop subprocess: " + ex.Message;
        }

        _process = null;
        return "UI-TARS Desktop subprocess kördes inte.";
    }

    public async Task<UiTarsVisionResultV1> ProposeActionAsync(string instruction, ScreenCaptureResultV1 capture)
    {
        if (string.IsNullOrWhiteSpace(instruction))
            return new UiTarsVisionResultV1(false, null, "Instruktion saknas.");

        if (!capture.Ok || !File.Exists(capture.Path))
            return new UiTarsVisionResultV1(false, null, "Screenshot saknas: " + capture.Error);

        var config = LoadConfig();
        if (config is null)
        {
            return new UiTarsVisionResultV1(
                false,
                null,
                "UI-TARS vision API är inte konfigurerad.\n" +
                "Sätt env JARVIS_UITARS_BASE_URL, JARVIS_UITARS_API_KEY och JARVIS_UITARS_MODEL, eller skapa config\\uitars.json utan att logga nyckeln i chat/docs."
            );
        }

        try
        {
            var imageBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(capture.Path));
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

            var endpoint = config.BaseUrl.TrimEnd('/');
            if (!endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                endpoint += "/chat/completions";

            var request = new
            {
                model = config.Model,
                temperature = 0,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content =
                            "You are UI-TARS action proposer for Jarvis. Return exactly one action call and nothing else.\n" +
                            "Allowed actions:\n" +
                            "click(start_box='(x,y)')\n" +
                            "double_click(start_box='(x,y)')\n" +
                            "right_click(start_box='(x,y)')\n" +
                            "hover(start_box='(x,y)')\n" +
                            "drag(start_box='(x,y)', end_box='(x,y)')\n" +
                            "type(content='text')\n" +
                            "hotkey(key='ctrl+l')\n" +
                            "scroll(direction='down')\n" +
                            "finished()"
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Instruction: " + instruction },
                            new { type = "image_url", image_url = new { url = "data:image/png;base64," + imageBase64 } }
                        }
                    }
                }
            };

            using var response = await http.PostAsJsonAsync(endpoint, request);
            var responseText = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return new UiTarsVisionResultV1(false, null, "UI-TARS API-fel: HTTP " + (int)response.StatusCode);

            var prediction = ExtractAssistantContent(responseText);
            if (!DesktopActionRequestV1.TryParsePrediction(prediction, capture.Width, capture.Height, instruction, capture.Path, out var action, out var parseError))
                return new UiTarsVisionResultV1(false, null, parseError);

            return new UiTarsVisionResultV1(true, action, "UI-TARS föreslog action: " + prediction);
        }
        catch (Exception ex)
        {
            return new UiTarsVisionResultV1(false, null, "UI-TARS bridge-fel: " + ex.Message);
        }
    }

    private static string ExtractAssistantContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return root.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private sealed record UiTarsConfig(string BaseUrl, string ApiKey, string Model);

    private static UiTarsConfig? LoadConfig()
    {
        var baseUrl = Environment.GetEnvironmentVariable("JARVIS_UITARS_BASE_URL");
        var apiKey = Environment.GetEnvironmentVariable("JARVIS_UITARS_API_KEY");
        var model = Environment.GetEnvironmentVariable("JARVIS_UITARS_MODEL");

        if (!string.IsNullOrWhiteSpace(baseUrl) &&
            !string.IsNullOrWhiteSpace(apiKey) &&
            !string.IsNullOrWhiteSpace(model))
            return new UiTarsConfig(baseUrl.Trim(), apiKey.Trim(), model.Trim());

        if (!File.Exists(ConfigPath))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
            var root = doc.RootElement;
            baseUrl = root.TryGetProperty("baseUrl", out var b) ? b.GetString() : "";
            apiKey = root.TryGetProperty("apiKey", out var k) ? k.GetString() : "";
            model = root.TryGetProperty("model", out var m) ? m.GetString() : "";

            if (string.IsNullOrWhiteSpace(baseUrl) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(model))
                return null;

            return new UiTarsConfig(baseUrl.Trim(), apiKey.Trim(), model.Trim());
        }
        catch
        {
            return null;
        }
    }
}
