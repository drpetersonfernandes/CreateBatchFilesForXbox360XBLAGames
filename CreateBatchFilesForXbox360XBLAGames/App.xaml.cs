using System.Windows.Threading;
using CreateBatchFilesForXbox360XBLAGames.Services;
using Serilog;

namespace CreateBatchFilesForXbox360XBLAGames;

public partial class App
{
    internal const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    internal const string BugReportApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    internal const string ApplicationName = "CreateBatchFilesForXbox360XBLAGames";

    private const string StatsApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private const string GitHubRepoOwner = "drpetersonfernandes";
    private const string GitHubRepoName = "CreateBatchFilesForXbox360XBLAGames";

    internal static readonly string ApplicationVersion =
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.6.0";

    public static BugReportService? BugReportService { get; private set; }
    public static StatsService? StatsService { get; private set; }
    public static UpdateService? UpdateService { get; private set; }

    public App()
    {
        ConfigureSerilog();

        BugReportService = new BugReportService(BugReportApiUrl, BugReportApiKey, ApplicationName, ApplicationVersion);
        StatsService = new StatsService(StatsApiUrl, BugReportApiKey, ApplicationName, ApplicationVersion);
        UpdateService = new UpdateService(GitHubRepoOwner, GitHubRepoName, ApplicationVersion);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        Exit += static (_, _) =>
        {
            BugReportService.CancelAll();
            StatsService.CancelAll();
            UpdateService.CancelAll();
            Log.CloseAndFlush();
        };

        _ = TrackApplicationUsageAsync();
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Debug()
            .WriteTo.Sink(new BugReportSink(BugReportApiUrl, BugReportApiKey, ApplicationName, ApplicationVersion))
            .WriteTo.Sink(new UiLogSink())
            .CreateLogger();
    }

    private static async Task TrackApplicationUsageAsync()
    {
        try
        {
            if (StatsService != null)
                await StatsService.SendStatsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "TrackApplicationUsageAsync failed");
        }
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            Log.Fatal(exception, "AppDomain.UnhandledException");
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Application.DispatcherUnhandledException");
        e.Handled = true;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }
}