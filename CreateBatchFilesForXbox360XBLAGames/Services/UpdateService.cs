using System.Net.Http;
using System.Net.Http.Json;
using CreateBatchFilesForXbox360XBLAGames.Models;
using Serilog;

namespace CreateBatchFilesForXbox360XBLAGames.Services;

public class UpdateService
{
    internal static HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static UpdateService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("CreateBatchFilesForXbox360XBLAGames");
    }

    private static CancellationTokenSource _globalCts = new();
    private static readonly Lock CtsLock = new();

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

    public async Task<UpdateCheckResult?> CheckForUpdateAsync()
    {
        CancellationToken token;
        lock (CtsLock)
        {
            token = _globalCts.Token;
        }

        try
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";

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
        catch (Exception ex)
        {
            Log.Error(ex, "CheckForUpdateAsync failed");
        }

        return null;
    }

    internal static Version? NormalizeVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        var trimmed = version.Trim();

        if (trimmed.StartsWith("release_", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["release_".Length..];
        }

        trimmed = trimmed.TrimStart('v', 'V').Trim();

        return Version.TryParse(trimmed, out var parsed) ? parsed : null;
    }
}
