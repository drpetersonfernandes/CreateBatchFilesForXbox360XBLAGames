using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Serilog;

namespace CreateBatchFilesForXbox360XBLAGames.Services;

public static class ScreenshotService
{
    private static readonly string ScreenshotFolder = Path.Combine(AppContext.BaseDirectory, "Screenshot");

    private static void EnsureFolderExists()
    {
        if (!Directory.Exists(ScreenshotFolder))
        {
            Directory.CreateDirectory(ScreenshotFolder);
        }
    }

    public static void CaptureWindow(Window window)
    {
        try
        {
            EnsureFolderExists();

            var width = (int)window.ActualWidth;
            var height = (int)window.ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);

            var filename = $"screenshot_{DateTime.Now:yyyy-MM-dd_HHmmss}.png";
            var filePath = Path.Combine(ScreenshotFolder, filename);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new FileStream(filePath, FileMode.Create);
            encoder.Save(stream);

            Log.Information("Screenshot saved: {Path}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to capture screenshot");
        }
    }
}