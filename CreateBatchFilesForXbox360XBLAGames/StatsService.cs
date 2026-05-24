using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace CreateBatchFilesForXbox360XBLAGames;

/// <summary>
/// Service responsible for silently sending application usage statistics to the Stats API.
/// This class is designed to be used as a singleton via the App class.
/// </summary>
public class StatsService
{
    internal static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static CancellationTokenSource _globalCts = new();
    private static readonly object CtsLock = new();

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationId;
    private readonly string _version;

    public StatsService(string apiUrl, string apiKey, string applicationId, string version)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationId = applicationId;
        _version = version;
    }

    public static void CancelAll()
    {
        CancellationTokenSource oldCts;
        lock (CtsLock)
        {
            oldCts = _globalCts;
            _globalCts = new CancellationTokenSource();
        }

        try
        {
            oldCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            oldCts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async Task SendStatsAsync()
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
                applicationId = _applicationId,
                version = _version
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            await HttpClient.SendAsync(request, token);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // ignored
        }
    }
}
