using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForXbox360XBLAGames;

/// <summary>
/// Service responsible for silently sending bug reports to the BugReport API.
/// This class is designed to be used as a singleton via the App class.
/// </summary>
public class BugReportService
{
    // Use a single, static HttpClient instance for the application's lifetime
    // to prevent socket exhaustion and improve performance.
    internal static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly CancellationTokenSource GlobalCts = new();

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;
    private readonly string _applicationVersion;

    public BugReportService(string apiUrl, string apiKey, string applicationName, string applicationVersion)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
        _applicationVersion = applicationVersion;
    }

    /// <summary>
    /// Cancels all pending HTTP requests and prevents new ones from starting.
    /// Call this when the application is shutting down.
    /// </summary>
    public static void CancelAll()
    {
        try
        {
            GlobalCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>
    /// Silently sends a bug report to the API.
    /// </summary>
    /// <param name="message">The error message or bug report.</param>
    /// <param name="version">Application version override.</param>
    /// <param name="environment">Environment details string.</param>
    /// <param name="stackTrace">Exception stack trace.</param>
    public async Task SendBugReportAsync(string message, string? version = null, string? environment = null, string? stackTrace = null)
    {
        CancellationToken token;
        try
        {
            if (GlobalCts.IsCancellationRequested)
                return;

            token = GlobalCts.Token;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            var payload = new
            {
                message,
                applicationName = _applicationName,
                version = version ?? _applicationVersion,
                environment,
                stackTrace
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("X-API-KEY", _apiKey);

            await HttpClient.SendAsync(request, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // Silently fail if there's an exception
        }
    }
}