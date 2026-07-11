using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForXbox360XBLAGames;

public class BugReportService
{
    internal static HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static CancellationTokenSource _globalCts = new();
    private static readonly object CtsLock = new();

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

    public static void CancelAll()
    {
        CancellationTokenSource oldCts;
        lock (CtsLock)
        {
            oldCts = _globalCts;
            _globalCts = new CancellationTokenSource();
        }

        try { oldCts.Cancel(); }
        catch (ObjectDisposedException) { }

        try { oldCts.Dispose(); }
        catch (ObjectDisposedException) { }
    }

    public static async Task SendAsync(string message, string applicationName, string version, string? environment, string? stackTrace)
    {
        CancellationToken token;
        lock (CtsLock)
        {
            token = _globalCts.Token;
        }

        try
        {
            var payload = new
            {
                message,
                applicationName,
                version,
                environment,
                stackTrace
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, App.BugReportApiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("X-API-KEY", App.BugReportApiKey);

            await HttpClient.SendAsync(request, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[BugReportService] SendAsync failed: {ex}");
        }
    }
}
