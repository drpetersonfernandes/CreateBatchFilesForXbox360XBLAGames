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

    private static readonly CancellationTokenSource GlobalCts = new();

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
        try
        {
            GlobalCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async Task SendStatsAsync()
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
