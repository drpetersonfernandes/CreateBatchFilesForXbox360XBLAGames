using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Serilog;

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

    private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (App.UpdateService == null) return;

            var result = await App.UpdateService.CheckForUpdateAsync();

            if (result?.UpdateAvailable == true && !string.IsNullOrEmpty(result.ReleaseUrl))
            {
                var choice = MessageBox.Show(
                    this,
                    $"A new version ({result.LatestVersion}) is available.\n\nWould you like to visit the download page?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (choice == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
                }
            }
            else
            {
                MessageBox.Show(
                    this,
                    "You are running the latest version.",
                    "No Updates Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check for updates from About window");
            MessageBox.Show(
                this,
                $"Failed to check for updates: {ex.Message}",
                "Update Check Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening URL: {Url}", e.Uri.AbsoluteUri);
            MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            e.Handled = true;
        }
    }
}