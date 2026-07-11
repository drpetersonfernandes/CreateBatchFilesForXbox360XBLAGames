using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CreateBatchFilesForXbox360XBLAGames.Services;
using Serilog.Core;
using Serilog.Events;

namespace CreateBatchFilesForXbox360XBLAGames;

public class BugReportSink : ILogEventSink
{
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;
    private readonly string _applicationVersion;

    public BugReportSink(string apiUrl, string apiKey, string applicationName, string applicationVersion)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
        _applicationVersion = applicationVersion;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error)
            return;

        try
        {
            var environment = BuildEnvironmentDetails();
            var errorMessage = logEvent.RenderMessage();
            var exception = logEvent.Exception;

            var sb = new StringBuilder();
            sb.AppendLine("=== Environment Details ===");
            sb.Append(environment);
            sb.AppendLine("=== Error Details ===");
            sb.AppendLine(errorMessage);

            if (exception != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== Exception Details ===");
                AppendExceptionDetails(sb, exception);
            }

            _ = BugReportService.SendAsync(sb.ToString(), _applicationName, _applicationVersion, environment, exception?.StackTrace);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[BugReportSink] Emit failed: {ex}");
        }
    }

    internal static string BuildEnvironmentDetails()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {App.ApplicationName}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {App.ApplicationVersion}");
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

    internal static void AppendExceptionDetails(StringBuilder sb, Exception exception)
    {
        var level = 0;
        while (true)
        {
            var indent = new string(' ', level * 2);

            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {exception.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {exception.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Source: {exception.Source}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}StackTrace:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{exception.StackTrace}");

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
