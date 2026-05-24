using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CreateBatchFilesForXbox360XBLAGames;

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }
}

public class UpdateCheckResult
{
    public bool UpdateAvailable { get; set; }
    public string? LatestVersion { get; set; }
    public string? ReleaseUrl { get; set; }
}

public class UpdateService
{
    internal static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly CancellationTokenSource GlobalCts = new();

    private readonly string _repoOwner;
    private readonly string _repoName;
    private readonly string _currentVersion;

    public UpdateService(string repoOwner, string repoName, string currentVersion)
    {
        _repoOwner = repoOwner;
        _repoName = repoName;
        _currentVersion = currentVersion;
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

    public async Task<UpdateCheckResult?> CheckForUpdateAsync()
    {
        CancellationToken token;
        try
        {
            if (GlobalCts.IsCancellationRequested)
                return null;

            token = GlobalCts.Token;
        }
        catch (ObjectDisposedException)
        {
            return null;
        }

        try
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("CreateBatchFilesForXbox360XBLAGames");

            var release = await HttpClient.GetFromJsonAsync<GitHubRelease>(url, token);

            if (release?.TagName == null)
                return null;

            var latestVersion = NormalizeVersion(release.TagName);
            var currentVersion = NormalizeVersion(_currentVersion);

            if (latestVersion == null || currentVersion == null)
                return null;

            return new UpdateCheckResult
            {
                UpdateAvailable = latestVersion > currentVersion,
                LatestVersion = latestVersion.ToString(),
                ReleaseUrl = release.HtmlUrl
            };
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // ignored
        }

        return null;
    }

    internal static Version? NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        var trimmed = version.Trim().TrimStart('v', 'V').Trim();

        return Version.TryParse(trimmed, out var parsed) ? parsed : null;
    }
}
