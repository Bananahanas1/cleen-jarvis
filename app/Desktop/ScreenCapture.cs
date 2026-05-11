using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace JarvisClean;

internal sealed record ScreenCaptureResultV1(bool Ok, string Path, int Width, int Height, string Error);

internal static class ScreenCapture
{
    private static readonly string ScreenshotDir = Path.Combine(@"F:\Jarvis-clean", "data", "screenshots");

    public static ScreenCaptureResultV1 CapturePrimaryToFile()
    {
        try
        {
            var screen = Screen.PrimaryScreen;
            if (screen is null)
                return new ScreenCaptureResultV1(false, "", 0, 0, "Ingen primär skärm hittades.");

            var bounds = screen.Bounds;
            Directory.CreateDirectory(ScreenshotDir);
            var path = Path.Combine(ScreenshotDir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");

            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);

            bmp.Save(path, ImageFormat.Png);
            return new ScreenCaptureResultV1(true, path, bounds.Width, bounds.Height, "");
        }
        catch (Exception ex)
        {
            return new ScreenCaptureResultV1(false, "", 0, 0, ex.Message);
        }
    }
}
