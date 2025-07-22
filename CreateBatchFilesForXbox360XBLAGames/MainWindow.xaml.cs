using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace CreateBatchFilesForXbox360XBLAGames;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        LogMessage("Welcome to the Batch File Creator for Xbox 360 XBLA Games.");
        LogMessage("");
        LogMessage("This program creates batch files to launch your Xbox 360 XBLA games.");
        LogMessage("Please follow these steps:");
        LogMessage("1. Select the Xenia executable file (xenia.exe)");
        LogMessage("2. Select the root folder containing your Xbox 360 XBLA game folders");
        LogMessage("3. Click 'Create Batch Files' to generate the batch files");
        LogMessage("");
        UpdateStatusBarMessage("Ready");
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
        // The application will shut down automatically when the main window closes.
        // No extra code is needed here.
    }

    private void LogMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        });
    }

    private async void BrowseXeniaButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var xeniaExePath = SelectFile();
            if (string.IsNullOrEmpty(xeniaExePath)) return;

            XeniaPathTextBox.Text = xeniaExePath;
            LogMessage($"Xenia executable selected: {xeniaExePath}");
            UpdateStatusBarMessage("Xenia executable selected.");

            // Validate the Xenia executable
            if (!File.Exists(xeniaExePath))
            {
                LogMessage("Warning: The selected Xenia executable file does not exist.");
                await ReportBugAsync("Selected Xenia executable does not exist: " + xeniaExePath);
            }
            else if (!Path.GetFileName(xeniaExePath).Contains("xenia", StringComparison.OrdinalIgnoreCase))
            {
                LogMessage("Warning: The selected file does not appear to be a Xenia executable.");
                await ReportBugAsync("Selected file may not be Xenia executable: " + xeniaExePath);
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error in method BrowseXeniaButton_Click", ex);
        }
    }

    private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var rootFolder = SelectFolder();
            if (string.IsNullOrEmpty(rootFolder)) return;

            GameFolderTextBox.Text = rootFolder;
            LogMessage($"Game folder selected: {rootFolder}");
            UpdateStatusBarMessage("Game folder selected.");

            // Validate the game folder
            if (!Directory.Exists(rootFolder))
            {
                LogMessage("Warning: The selected game folder does not exist.");
                await ReportBugAsync("Selected game folder does not exist: " + rootFolder);
            }
            else
            {
                var subDirectories = Directory.GetDirectories(rootFolder);
                if (subDirectories.Length != 0) return;

                LogMessage("Warning: The selected game folder has no subdirectories.");
                await ReportBugAsync("Selected game folder has no subdirectories: " + rootFolder);
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error in method BrowseFolderButton_Click", ex);
        }
    }

    private async void CreateBatchFilesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var xeniaExePath = XeniaPathTextBox.Text;
            var rootFolder = GameFolderTextBox.Text;

            if (string.IsNullOrEmpty(xeniaExePath))
            {
                LogMessage("Error: No Xenia executable selected.");
                ShowError("Please select the Xenia executable file (xenia.exe).");
                UpdateStatusBarMessage("Error: Xenia executable not selected.");
                return;
            }

            if (!File.Exists(xeniaExePath))
            {
                LogMessage($"Error: Xenia executable not found at path: {xeniaExePath}");
                ShowError("The selected Xenia executable file does not exist.");
                await ReportBugAsync("Xenia executable not found", new FileNotFoundException("The Xenia executable was not found", xeniaExePath));
                UpdateStatusBarMessage("Error: Xenia executable not found.");
                return;
            }

            if (string.IsNullOrEmpty(rootFolder))
            {
                LogMessage("Error: No game folder selected.");
                ShowError("Please select the root folder containing your Xbox 360 XBLA game folders.");
                UpdateStatusBarMessage("Error: Game folder not selected.");
                return;
            }

            if (!Directory.Exists(rootFolder))
            {
                LogMessage($"Error: Game folder not found at path: {rootFolder}");
                ShowError("The selected game folder does not exist.");
                await ReportBugAsync("Game folder not found", new DirectoryNotFoundException($"Game folder not found: {rootFolder}"));
                UpdateStatusBarMessage("Error: Game folder not found.");
                return;
            }

            try
            {
                await CreateBatchFilesForXboxXblaGames(rootFolder, xeniaExePath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating batch files: {ex.Message}");
                ShowError($"An error occurred while creating batch files: {ex.Message}");
                await ReportBugAsync("Error creating batch files", ex);
                UpdateStatusBarMessage("Process failed with an error.");
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error creating batch files", ex);
            UpdateStatusBarMessage("An unexpected error occurred.");
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

    private string? SelectFile()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Please select the Xenia executable file (xenia.exe)",
            Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        return ofd.ShowDialog() == true ? ofd.FileName : null;
    }

    private async Task CreateBatchFilesForXboxXblaGames(string rootFolder, string xeniaExePath)
    {
        try
        {
            var gameDirectories = Directory.GetDirectories(rootFolder);
            var filesCreated = 0;
            var directoriesProcessed = 0;
            var directoriesSkipped = 0;

            LogMessage("");
            LogMessage("Starting batch file creation process...");
            UpdateStatusBarMessage("Creating batch files...");

            foreach (var gameDirectory in gameDirectories)
            {
                directoriesProcessed++;
                try
                {
                    var gameFolderName = Path.GetFileName(gameDirectory);
                    var batchFilePath = Path.Combine(rootFolder, gameFolderName + ".bat");

                    var gameFilePath = await FindGameFile(gameDirectory);

                    if (string.IsNullOrEmpty(gameFilePath))
                    {
                        LogMessage($"No game file found in {gameFolderName}. Skipping...");
                        directoriesSkipped++;
                        await ReportBugAsync($"No game file found in directory: {gameFolderName}",
                            new FileNotFoundException("No game file found in XBLA directory structure", gameDirectory));
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

                        LogMessage($"Batch file created: {batchFilePath}");
                        filesCreated++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error creating batch file for {gameFolderName}: {ex.Message}");
                        await ReportBugAsync($"Error creating batch file for {gameFolderName}", ex);
                        directoriesSkipped++;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error processing directory {Path.GetFileName(gameDirectory)}: {ex.Message}");
                    await ReportBugAsync($"Error processing directory: {Path.GetFileName(gameDirectory)}", ex);
                    directoriesSkipped++;
                }
            }

            LogMessage("");
            LogMessage($"Processed {directoriesProcessed} directories.");
            LogMessage($"Skipped {directoriesSkipped} directories.");
            UpdateStatusBarMessage($"Process complete. Created {filesCreated} files, skipped {directoriesSkipped}.");

            if (filesCreated > 0)
            {
                LogMessage($"{filesCreated} batch files have been successfully created.");
                LogMessage("They are located in the root folder of your Xbox 360 XBLA games.");

                ShowMessageBox($"{filesCreated} batch files have been successfully created.\n\n" +
                               "They are located in the root folder of your Xbox 360 XBLA games.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                const string errorMessage = "No valid game folders found. No batch files were created.";
                LogMessage(errorMessage);
                ShowError(errorMessage);

                var ex = new Exception($"Processed {directoriesProcessed} directories but created 0 batch files");
                await ReportBugAsync(errorMessage, ex);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error scanning game folders: {ex.Message}");
            UpdateStatusBarMessage("Error scanning game folders.");
            await ReportBugAsync("Error scanning game folders", ex);
            throw;
        }
    }

    private async Task<string?> FindGameFile(string gameDirectory)
    {
        try
        {
            var directories = Directory.GetDirectories(gameDirectory, "000D0000", SearchOption.AllDirectories);

            if (directories.Length > 0)
            {
                var files = Directory.GetFiles(directories[0]);
                return files.Length > 0 ? files[0] : null;
            }

            // If we couldn't find the 000D0000 directory, report the structure for debugging.
            var directoryStructure = new StringBuilder();
            directoryStructure.AppendLine(CultureInfo.InvariantCulture, $"Directory structure for {Path.GetFileName(gameDirectory)}:");
            try
            {
                var allDirs = Directory.GetDirectories(gameDirectory, "*", SearchOption.AllDirectories);
                foreach (var dir in allDirs.Take(10)) // Limit report size
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

            await ReportBugAsync($"No 000D0000 directory found for game: {Path.GetFileName(gameDirectory)}", new DirectoryNotFoundException(directoryStructure.ToString()));
        }
        catch (Exception ex)
        {
            LogMessage($"Error finding game file in {Path.GetFileName(gameDirectory)}: {ex.Message}");
            await ReportBugAsync($"Error finding game file in {Path.GetFileName(gameDirectory)}", ex);
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

    private async Task ReportBugAsync(string message, Exception? exception = null)
    {
        if (App.BugReportService == null) return;

        try
        {
            var fullReport = new StringBuilder();
            var assemblyName = GetType().Assembly.GetName();

            // Add system information
            fullReport.AppendLine("=== Bug Report ===");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Application: {assemblyName.Name}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Version: {assemblyName.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Date/Time: {DateTime.Now}");
            fullReport.AppendLine();

            // Add a message
            fullReport.AppendLine("=== Error Message ===");
            fullReport.AppendLine(message);
            fullReport.AppendLine();

            // Add exception details if available
            if (exception != null)
            {
                fullReport.AppendLine("=== Exception Details ===");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.GetType().FullName}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Source: {exception.Source}");
                fullReport.AppendLine("Stack Trace:");
                fullReport.AppendLine(exception.StackTrace);

                // Add inner exception if available
                if (exception.InnerException != null)
                {
                    fullReport.AppendLine("Inner Exception:");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.InnerException.GetType().FullName}");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.InnerException.Message}");
                    fullReport.AppendLine("Stack Trace:");
                    fullReport.AppendLine(exception.InnerException.StackTrace);
                }
            }

            // Add log contents if available
            if (LogTextBox != null)
            {
                var logContent = await Dispatcher.InvokeAsync(() => LogTextBox.Text);
                if (!string.IsNullOrEmpty(logContent))
                {
                    fullReport.AppendLine().AppendLine("=== Application Log ===").Append(logContent);
                }
            }

            // Add Xenia and game folder paths if available
            if (XeniaPathTextBox != null && GameFolderTextBox != null)
            {
                var (xeniaPath, gameFolderPath) = await Dispatcher.InvokeAsync(() => (XeniaPathTextBox.Text, GameFolderTextBox.Text));
                fullReport.AppendLine().AppendLine("=== Configuration ===").AppendLine(CultureInfo.InvariantCulture, $"Xenia Path: {xeniaPath}").AppendLine(CultureInfo.InvariantCulture, $"Game Folder Path: {gameFolderPath}");
            }

            // Silently send the report
            await App.BugReportService.SendBugReportAsync(fullReport.ToString());
        }
        catch
        {
            // Silently fail if error reporting itself fails
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
            LogMessage($"Error opening About window: {ex.Message}");
            _ = ReportBugAsync("Error opening About window", ex);
        }
    }
}
