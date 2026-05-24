using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace CreateBatchFilesForXbox360XBLAGames;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        AppVersionTextBlock.Text = $"Version: {GetApplicationVersion()}";
    }

    internal static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            // Notify developer
            if (App.BugReportService != null)
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.6.0";
                var env = new StringBuilder();
                env.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now}");
                env.AppendLine("Application Name: CreateBatchFilesForXbox360XBLAGames");
                env.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {version}");
                env.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {Environment.OSVersion}");
                env.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {RuntimeInformation.ProcessArchitecture}");
                env.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                env.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {RuntimeInformation.OSDescription}");
                env.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {Environment.ProcessorCount}");
                env.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {AppContext.BaseDirectory}");
                env.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {Path.GetTempPath()}");
                var environment = env.ToString();

                var report = new StringBuilder();
                report.AppendLine("=== Environment Details ===");
                report.Append(environment);
                report.AppendLine();
                report.AppendLine("=== Error Details ===");
                report.AppendLine(CultureInfo.InvariantCulture, $"Error opening URL: {e.Uri.AbsoluteUri}");
                report.AppendLine();
                report.AppendLine("=== Exception Details ===");
                report.AppendLine(CultureInfo.InvariantCulture, $"Type: {ex.GetType().FullName}");
                report.AppendLine(CultureInfo.InvariantCulture, $"Message: {ex.Message}");
                report.AppendLine(CultureInfo.InvariantCulture, $"Source: {ex.Source}");
                report.AppendLine("Stack Trace:");
                report.AppendLine(ex.StackTrace);

                _ = App.BugReportService.SendBugReportAsync(report.ToString(), version, environment, ex.StackTrace);
            }

            // Notify user
            MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            e.Handled = true;
        }
    }
}