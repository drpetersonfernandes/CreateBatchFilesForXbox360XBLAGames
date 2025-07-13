using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForXbox360XBLAGames;

/// <inheritdoc />
/// <summary>
/// Service responsible for silently sending bug reports to the BugReport API
/// </summary>
public class BugReportService : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;

    public BugReportService(string apiUrl, string apiKey, string applicationName)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;

        // Add the API key to the headers once during initialization
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
    }

    /// <summary>
    /// Silently sends a bug report to the API
    /// </summary>
    /// <param name="message">The error message or bug report</param>
    /// <returns>A task representing the asynchronous operation</returns>
    // **FIXED**: Changed the return type from Task<bool> to Task to reflect fire-and-forget usage.
    public async Task SendBugReportAsync(string message)
    {
        try
        {
            // Create the request payload
            var content = JsonContent.Create(new
            {
                message,
                applicationName = _applicationName
            });

            // Send the request
            await _httpClient.PostAsync(_apiUrl, content);
        }
        catch
        {
            // Silently fail if there's an exception
        }
    }

    public void Dispose()
    {
        // Dispose the HttpClient to release any resources it holds
        _httpClient?.Dispose();

        // Suppress finalization since we've manually disposed resources
        GC.SuppressFinalize(this);
    }
}
