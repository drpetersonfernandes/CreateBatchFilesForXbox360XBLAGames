using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace CreateBatchFilesForXbox360XBLAGames;

/// <inheritdoc cref="System.Windows.Application" />
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // Bug Report API configuration (centralized here)
    private const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    private const string BugReportApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private const string ApplicationName = "CreateBatchFilesForXbox360XBLAGames";

    // Stats API configuration
    private const string StatsApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";

    /// <summary>
    /// Provides a single, shared instance of the BugReportService for the entire application.
    /// </summary>
    public static BugReportService? BugReportService { get; private set; }

    /// <summary>
    /// Provides a single, shared instance of the StatsService for the entire application.
    /// </summary>
    public static StatsService? StatsService { get; private set; }

    public App()
    {
        // Initialize the single bug report service instance for the application.
        var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.6.0";
        BugReportService = new BugReportService(BugReportApiUrl, BugReportApiKey, ApplicationName, applicationVersion);

        // Initialize the stats service and report application usage.
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        StatsService = new StatsService(StatsApiUrl, BugReportApiKey, ApplicationName, version);

        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // Ensure all pending operations are cancelled when the app exits
        Exit += static (_, _) =>
        {
            BugReportService.CancelAll();
            StatsService.CancelAll();
        };

        // Fire and forget: track application usage on startup.
        _ = TrackApplicationUsageAsync();
    }

    private static async Task TrackApplicationUsageAsync()
    {
        try
        {
            if (StatsService != null)
            {
                await StatsService.SendStatsAsync();
            }
        }
        catch
        {
            // ignored
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ReportExceptionAsync(exception, "AppDomain.UnhandledException");
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportExceptionAsync(e.Exception, "Application.DispatcherUnhandledException");
        e.Handled = true;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportExceptionAsync(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static async void ReportExceptionAsync(Exception exception, string source)
    {
        try
        {
            var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.6.0";
            var environment = BuildEnvironmentDetails();
            var message = BuildExceptionReport(exception, source, environment);
            var stackTrace = exception.StackTrace;

            // Silently report the exception to our API using the shared service instance.
            if (BugReportService != null)
            {
                await BugReportService.SendBugReportAsync(message, applicationVersion, environment, stackTrace);
            }
        }
        catch
        {
            // Silently ignore any errors in the reporting process
        }
    }

    internal static string BuildEnvironmentDetails()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {ApplicationName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {Assembly.GetExecutingAssembly().GetName().Version}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {Environment.OSVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {RuntimeInformation.OSDescription}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {Environment.ProcessorCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {AppContext.BaseDirectory}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {Path.GetTempPath()}");
        return sb.ToString();
    }

    internal static string BuildExceptionReport(Exception exception, string source, string environment)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Environment Details ===");
        sb.Append(environment);
        sb.AppendLine();
        sb.AppendLine("=== Error Details ===");
        sb.AppendLine(source);
        sb.AppendLine();
        sb.AppendLine("=== Exception Details ===");
        AppendExceptionDetails(sb, exception);
        return sb.ToString();
    }

    internal static void AppendExceptionDetails(StringBuilder sb, Exception exception, int level = 0)
    {
        while (true)
        {
            var indent = new string(' ', level * 2);

            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {exception.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {exception.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Source: {exception.Source}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}StackTrace:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{exception.StackTrace}");

            // If there's an inner exception, include it too
            if (exception.InnerException != null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Inner Exception:");
                exception = exception.InnerException;
                level += 1;
                continue;
            }

            break;
        }
    }
}
