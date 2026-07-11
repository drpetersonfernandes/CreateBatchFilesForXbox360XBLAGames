using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Serilog;

namespace CreateBatchFilesForXbox360XBLAGames;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        UILogSink.Initialize(message =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(message + Environment.NewLine);
                LogTextBox.ScrollToEnd();
            });
        });

        Log.Information("Welcome to the Batch File Creator for Xbox 360 XBLA Games.");
        Log.Information("");
        Log.Information("This program creates batch files to launch your Xbox 360 XBLA games.");
        Log.Information("Please follow these steps:");
        Log.Information("1. Select the Xenia executable file (xenia.exe)");
        Log.Information("2. Select the root folder containing your Xbox 360 XBLA game folders");
        Log.Information("3. Click 'Create Batch Files' to generate the batch files");
        Log.Information("");
        UpdateStatusBarMessage("Ready");

        Loaded += async (_, _) => await CheckForUpdatesAsync();
    }

    private void UpdateStatusBarMessage(string message)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusBarMessage.Text = message;
        });
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        BugReportService.CancelAll();
    }

    private async void BrowseXeniaButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            var xeniaExePath = SelectFile();
            if (string.IsNullOrEmpty(xeniaExePath)) return;

            XeniaPathTextBox.Text = xeniaExePath;
            Log.Information("Xenia executable selected: {Path}", xeniaExePath);
            UpdateStatusBarMessage("Xenia executable selected.");

            if (!File.Exists(xeniaExePath))
            {
                Log.Warning("The selected Xenia executable file does not exist: {Path}", xeniaExePath);
            }
            else if (!Path.GetFileName(xeniaExePath).Contains("xenia", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("The selected file may not be a Xenia executable: {Path}", xeniaExePath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in method BrowseXeniaButton_ClickAsync");
        }
    }

    private async void BrowseFolderButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            var rootFolder = SelectFolder();
            if (string.IsNullOrEmpty(rootFolder)) return;

            GameFolderTextBox.Text = rootFolder;
            Log.Information("Game folder selected: {Path}", rootFolder);
            UpdateStatusBarMessage("Game folder selected.");

            if (!Directory.Exists(rootFolder))
            {
                Log.Warning("The selected game folder does not exist: {Path}", rootFolder);
            }
            else
            {
                var subDirectories = Directory.GetDirectories(rootFolder);
                if (subDirectories.Length != 0) return;

                Log.Warning("The selected game folder has no subdirectories: {Path}", rootFolder);
                ShowError("The selected folder has no subdirectories. Please select a folder that contains your Xbox 360 XBLA game folders.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in method BrowseFolderButton_ClickAsync");
        }
    }

    private async void CreateBatchFilesButton_ClickAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            var xeniaExePath = XeniaPathTextBox.Text;
            var rootFolder = GameFolderTextBox.Text;

            if (string.IsNullOrEmpty(xeniaExePath))
            {
                Log.Error("No Xenia executable selected");
                ShowError("Please select the Xenia executable file (xenia.exe).");
                UpdateStatusBarMessage("Error: Xenia executable not selected.");
                return;
            }

            if (!File.Exists(xeniaExePath))
            {
                Log.Error("Xenia executable not found at path: {Path}", xeniaExePath);
                ShowError("The selected Xenia executable file does not exist.");
                UpdateStatusBarMessage("Error: Xenia executable not found.");
                return;
            }

            if (string.IsNullOrEmpty(rootFolder))
            {
                Log.Error("No game folder selected");
                ShowError("Please select the root folder containing your Xbox 360 XBLA game folders.");
                UpdateStatusBarMessage("Error: Game folder not selected.");
                return;
            }

            if (!Directory.Exists(rootFolder))
            {
                Log.Error("Game folder not found at path: {Path}", rootFolder);
                ShowError("The selected game folder does not exist.");
                UpdateStatusBarMessage("Error: Game folder not found.");
                return;
            }

            if (!CheckWritePermission(rootFolder))
            {
                const string errorMessage = "No write permission for the selected folder. " +
                                            "Please try running the application as administrator or select a folder where you have write permission.";
                Log.Error(errorMessage);
                ShowError(errorMessage);
                UpdateStatusBarMessage("Error: Access denied.");
                return;
            }

            try
            {
                await CreateBatchFilesForXbox360XblaGamesAsync(rootFolder, xeniaExePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating batch files");
                ShowError($"An error occurred while creating batch files: {ex.Message}");
                UpdateStatusBarMessage("Process failed with an error.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating batch files");
            UpdateStatusBarMessage("An unexpected error occurred.");
        }
    }

    private async Task CheckForUpdatesAsync()
    {
        if (App.UpdateService == null) return;

        try
        {
            var result = await App.UpdateService.CheckForUpdateAsync();

            if (result?.UpdateAvailable != true || string.IsNullOrEmpty(result.ReleaseUrl))
                return;

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
        catch (Exception ex)
        {
            Log.Warning(ex, "CheckForUpdatesAsync failed");
        }
    }

    private static string? SelectFolder()
    {
        var fbd = new OpenFolderDialog
        {
            Title = "Please select the root folder where your Xbox 360 XBLA game folders are located."
        };

        return fbd.ShowDialog() == true ? fbd.FolderName : null;
    }

    private static string? SelectFile()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Please select the Xenia executable file (xenia.exe)",
            Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        return ofd.ShowDialog() == true ? ofd.FileName : null;
    }

    private async Task CreateBatchFilesForXbox360XblaGamesAsync(string rootFolder, string xeniaExePath)
    {
        try
        {
            var gameDirectories = Directory.GetDirectories(rootFolder);
            var filesCreated = 0;
            var directoriesProcessed = 0;
            var directoriesSkipped = 0;

            Log.Information("");
            Log.Information("Starting batch file creation process...");
            UpdateStatusBarMessage("Creating batch files...");

            foreach (var gameDirectory in gameDirectories)
            {
                directoriesProcessed++;
                try
                {
                    var gameFolderName = Path.GetFileName(gameDirectory);
                    var batchFilePath = Path.Combine(rootFolder, gameFolderName + ".bat");

                    var gameFilePath = await FindGameFileAsync(gameDirectory);

                    if (string.IsNullOrEmpty(gameFilePath))
                    {
                        Log.Warning("No game file found in {Folder}. Skipping...", gameFolderName);
                        directoriesSkipped++;
                        continue;
                    }

                    try
                    {
                        await using (StreamWriter sw = new(batchFilePath))
                        {
                            await sw.WriteLineAsync("@echo off");
                            await sw.WriteLineAsync($"cd /d \"{Path.GetDirectoryName(xeniaExePath)}\"");
                            await sw.WriteLineAsync($"start \"\" \"{Path.GetFileName(xeniaExePath)}\" \"{gameFilePath}\"");
                        }

                        Log.Information("Batch file created: {Path}", batchFilePath);
                        filesCreated++;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.Warning(ex, "Permission denied creating batch file for {Folder}", gameFolderName);
                        directoriesSkipped++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error creating batch file for {Folder}", gameFolderName);
                        directoriesSkipped++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing directory {Folder}", Path.GetFileName(gameDirectory));
                    directoriesSkipped++;
                }
            }

            Log.Information("");
            Log.Information("Processed {Count} directories", directoriesProcessed);
            Log.Information("Skipped {Count} directories", directoriesSkipped);
            UpdateStatusBarMessage($"Process complete. Created {filesCreated} files, skipped {directoriesSkipped}.");

            if (filesCreated > 0)
            {
                Log.Information("{Count} batch files have been successfully created", filesCreated);
                Log.Information("They are located in the root folder of your Xbox 360 XBLA games");

                ShowMessageBox($"{filesCreated} batch files have been successfully created.\n\n" +
                               "They are located in the root folder of your Xbox 360 XBLA games.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                const string errorMessage = "No valid game folders found. No batch files were created.";
                Log.Warning(errorMessage);
                ShowError(errorMessage);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error scanning game folders");
            UpdateStatusBarMessage("Error scanning game folders.");
            throw;
        }
    }

    private async Task<string?> FindGameFileAsync(string gameDirectory)
    {
        try
        {
            var directories = Directory.GetDirectories(gameDirectory, "000D0000", SearchOption.AllDirectories);
            if (directories.Length > 0)
            {
                var files = Directory.GetFiles(directories[0]);
                return files.Length > 0 ? files[0] : null;
            }

            var allFiles = Directory.GetFiles(gameDirectory, "*", SearchOption.AllDirectories);
            if (allFiles.Length > 0)
            {
                Log.Information("000D0000 directory not found for {Folder}, using first available file: {File}",
                    Path.GetFileName(gameDirectory), Path.GetFileName(allFiles[0]));
                return allFiles[0];
            }

            var directoryStructure = new StringBuilder();
            directoryStructure.AppendLine(CultureInfo.InvariantCulture, $"Directory structure for {Path.GetFileName(gameDirectory)}:");
            try
            {
                var allDirs = Directory.GetDirectories(gameDirectory, "*", SearchOption.AllDirectories);
                foreach (var dir in allDirs.Take(10))
                {
                    directoryStructure.AppendLine(CultureInfo.InvariantCulture, $"- {Path.GetRelativePath(gameDirectory, dir)}");
                }

                if (allDirs.Length > 10)
                {
                    directoryStructure.AppendLine(CultureInfo.InvariantCulture, $"- ... and {allDirs.Length - 10} more directories");
                }
            }
            catch (Exception ex)
            {
                directoryStructure.AppendLine(CultureInfo.InvariantCulture, $"Error accessing directory structure: {ex.Message}");
            }

            Log.Error("No files found for game: {Folder}\n{Structure}",
                Path.GetFileName(gameDirectory), directoryStructure.ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error finding game file in {Folder}", Path.GetFileName(gameDirectory));
        }

        return null;
    }

    private void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Dispatcher.Invoke(() =>
            MessageBox.Show(this, message, title, buttons, icon));
    }

    private void ShowError(string message)
    {
        ShowMessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    internal static bool CheckWritePermission(string directoryPath)
    {
        try
        {
            var testFilePath = Path.Combine(directoryPath, Path.GetRandomFileName());
            using (File.Create(testFilePath, 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        BugReportService.CancelAll();
        Application.Current.Shutdown();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening About window");
        }
    }
}
