using System.Diagnostics;
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
        return App.ApplicationVersion;
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
                var environment = App.BuildEnvironmentDetails();
                var report = App.BuildExceptionReport(ex, $"Error opening URL: {e.Uri.AbsoluteUri}", environment);

                App.BugReportService.SendBugReportAsync(report, App.ApplicationVersion, environment, ex.StackTrace).FireAndForget();
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