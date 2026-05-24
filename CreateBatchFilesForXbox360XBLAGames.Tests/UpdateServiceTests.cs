using System.Net;
using System.Reflection;
using System.Text.Json;
using Moq;
using Moq.Protected;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class UpdateServiceTests
{
    private const string TestRepoOwner = "testowner";
    private const string TestRepoName = "testrepo";
    private const string TestCurrentVersion = "1.0.0";
    private const string TestApiUrl = "https://api.github.com/repos/testowner/testrepo/releases/latest";

    [Fact]
    public void Constructor_ShouldStoreRepoOwner()
    {
        var service = new UpdateService(TestRepoOwner, TestRepoName, TestCurrentVersion);
        var field = typeof(UpdateService).GetField("_repoOwner", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestRepoOwner, value);
    }

    [Fact]
    public void Constructor_ShouldStoreRepoName()
    {
        var service = new UpdateService(TestRepoOwner, TestRepoName, TestCurrentVersion);
        var field = typeof(UpdateService).GetField("_repoName", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestRepoName, value);
    }

    [Fact]
    public void Constructor_ShouldStoreCurrentVersion()
    {
        var service = new UpdateService(TestRepoOwner, TestRepoName, TestCurrentVersion);
        var field = typeof(UpdateService).GetField("_currentVersion", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestCurrentVersion, value);
    }

    [Fact]
    public void HttpClient_ShouldHaveThirtySecondTimeout()
    {
        var field = typeof(UpdateService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        var httpClient = field?.GetValue(null) as HttpClient;
        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldSendGetRequest()
    {
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.CheckForUpdateAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldSendToCorrectUrl()
    {
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.CheckForUpdateAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.RequestUri != null && m.RequestUri.ToString() == TestApiUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenLatestVersionIsNewer_ShouldReturnUpdateAvailableTrue()
    {
        var handlerMock = CreateMockReleaseHandler("v2.0.0", "Release 2.0.0", "https://github.com/test/releases/tag/v2.0.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("2.0.0", result.LatestVersion);
        Assert.Equal("https://github.com/test/releases/tag/v2.0.0", result.ReleaseUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenLatestVersionIsSame_ShouldReturnUpdateAvailableFalse()
    {
        var handlerMock = CreateMockReleaseHandler("v1.0.0", "Release 1.0.0", "https://github.com/test/releases/tag/v1.0.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.False(result.UpdateAvailable);
        Assert.Equal("1.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenLatestVersionIsOlder_ShouldReturnUpdateAvailableFalse()
    {
        var handlerMock = CreateMockReleaseHandler("v0.9.0", "Release 0.9.0", "https://github.com/test/releases/tag/v0.9.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.False(result.UpdateAvailable);
        Assert.Equal("0.9.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithVersionTagWithoutPrefix_ShouldCompareCorrectly()
    {
        var handlerMock = CreateMockReleaseHandler("2.0.0", "Release 2.0.0", "https://github.com/test/releases/tag/2.0.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("2.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithVersionTagCapitalV_ShouldCompareCorrectly()
    {
        var handlerMock = CreateMockReleaseHandler("V2.0.0", "Release 2.0.0", "https://github.com/test/releases/tag/V2.0.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("2.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithPatchVersionBump_ShouldDetectUpdate()
    {
        var handlerMock = CreateMockReleaseHandler("v1.0.1", "Release 1.0.1", "https://github.com/test/releases/tag/v1.0.1");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("1.0.1", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithMinorVersionBump_ShouldDetectUpdate()
    {
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("1.1.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithMajorVersionBump_ShouldDetectUpdate()
    {
        var handlerMock = CreateMockReleaseHandler("v3.0.0", "Release 3.0.0", "https://github.com/test/releases/tag/v3.0.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("3.0.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithEmptyTagName_ShouldReturnNull()
    {
        var handlerMock = CreateMockReleaseHandler("", "Release", "https://github.com/test/releases/tag/");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithNullTagName_ShouldReturnNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"tag_name\":null,\"name\":\"Release\",\"html_url\":\"https://github.com/test\"}",
                    System.Text.Encoding.UTF8, "application/json")
            });
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithInvalidJson_ShouldReturnNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("not json", System.Text.Encoding.UTF8, "application/json")
            });
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithInvalidVersionFormat_ShouldReturnNull()
    {
        var handlerMock = CreateMockReleaseHandler("not-a-version", "Release", "https://github.com/test/releases/tag/not-a-version");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldSetUserAgentHeader()
    {
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.CheckForUpdateAsync();

        var field = typeof(UpdateService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        var httpClient = field?.GetValue(null) as HttpClient;
        Assert.NotNull(httpClient);
        Assert.Contains(
            httpClient.DefaultRequestHeaders.UserAgent,
            static ua => ua.Product?.Name == "CreateBatchFilesForXbox360XBLAGames");
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldReturnEarlyWhenGlobalCtsIsCancelled()
    {
        UpdateService.CancelAll();
        var service = CreateServiceWithMockHandler(new Mock<HttpMessageHandler>().Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
        ResetGlobalCts();
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldHandleOperationCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldHandleGeneralException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Network error"));
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldHandleServerError()
    {
        var handlerMock = CreateMockReleaseHandler(HttpStatusCode.InternalServerError, "Internal error");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldHandleNotFound()
    {
        var handlerMock = CreateMockReleaseHandler(HttpStatusCode.NotFound, "Not found");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldHandleHttpRequestException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldHandleTaskCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public void CancelAll_ShouldReplaceGlobalCts()
    {
        ResetGlobalCts();

        var ctsField = typeof(UpdateService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
        var oldCts = ctsField?.GetValue(null) as CancellationTokenSource;

        UpdateService.CancelAll();

        var newCts = ctsField?.GetValue(null) as CancellationTokenSource;
        Assert.NotNull(oldCts);
        Assert.NotNull(newCts);
        Assert.NotSame(oldCts, newCts);
        Assert.False(newCts.IsCancellationRequested);

        ResetGlobalCts();
    }

    [Fact]
    public void CancelAll_ShouldHandleMultipleCalls()
    {
        ResetGlobalCts();

        UpdateService.CancelAll();
        UpdateService.CancelAll();
        UpdateService.CancelAll();

        ResetGlobalCts();
    }

    [Fact]
    public async Task CancelAll_ShouldAllowSubsequentRequests()
    {
        ResetGlobalCts();
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        UpdateService.CancelAll();
        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        ResetGlobalCts();
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldSendWithCancellationToken()
    {
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.CheckForUpdateAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.Is<CancellationToken>(static ct => ct.CanBeCanceled));
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithCurrentVersionNull_ShouldReturnNull()
    {
        var handlerMock = CreateMockReleaseHandler("v1.1.0", "Release 1.1.0", "https://github.com/test/releases/tag/v1.1.0");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(30) });
        var service = new UpdateService(TestRepoOwner, TestRepoName, "invalid");

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ShouldIncludeReleaseUrlInResult()
    {
        const string releaseUrl = "https://github.com/testowner/testrepo/releases/tag/v2.0.0";
        var handlerMock = CreateMockReleaseHandler("v2.0.0", "Release 2.0.0", releaseUrl);
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Equal(releaseUrl, result.ReleaseUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithNullHtmlUrl_ShouldReturnNullUrl()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"tag_name\":\"v2.0.0\",\"name\":\"Release 2.0.0\",\"html_url\":null}",
                    System.Text.Encoding.UTF8, "application/json")
            });
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.Null(result.ReleaseUrl);
    }

    [Fact]
    public void NormalizeVersion_WithValidVersion_ShouldReturnVersionObject()
    {
        var result = UpdateService.NormalizeVersion("1.2.3");

        Assert.NotNull(result);
        Assert.Equal(1, result.Major);
        Assert.Equal(2, result.Minor);
        Assert.Equal(3, result.Build);
    }

    [Fact]
    public void NormalizeVersion_WithVPrefix_ShouldReturnVersionObject()
    {
        var result = UpdateService.NormalizeVersion("v1.2.3");

        Assert.NotNull(result);
        Assert.Equal(1, result.Major);
        Assert.Equal(2, result.Minor);
        Assert.Equal(3, result.Build);
    }

    [Fact]
    public void NormalizeVersion_WithCapitalVPrefix_ShouldReturnVersionObject()
    {
        var result = UpdateService.NormalizeVersion("V1.2.3");

        Assert.NotNull(result);
        Assert.Equal(1, result.Major);
        Assert.Equal(2, result.Minor);
        Assert.Equal(3, result.Build);
    }

    [Fact]
    public void NormalizeVersion_WithNullInput_ShouldReturnNull()
    {
        var result = UpdateService.NormalizeVersion(null!);

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeVersion_WithEmptyInput_ShouldReturnNull()
    {
        var result = UpdateService.NormalizeVersion(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeVersion_WithWhitespaceInput_ShouldReturnNull()
    {
        var result = UpdateService.NormalizeVersion("   ");

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeVersion_WithOnlyVPrefix_ShouldReturnNull()
    {
        var result = UpdateService.NormalizeVersion("v");

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeVersion_WithInvalidFormat_ShouldReturnNull()
    {
        var result = UpdateService.NormalizeVersion("not.a.version");

        Assert.Null(result);
    }

    [Fact]
    public void NormalizeVersion_WithPrefixSpaces_ShouldReturnVersionObject()
    {
        var result = UpdateService.NormalizeVersion("  v1.2.3  ");

        Assert.NotNull(result);
        Assert.Equal(1, result.Major);
        Assert.Equal(2, result.Minor);
        Assert.Equal(3, result.Build);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithFourPartVersion_ShouldCompareCorrectly()
    {
        var handlerMock = CreateMockReleaseHandler("v1.0.0.1", "Release 1.0.0.1", "https://github.com/test/releases/tag/v1.0.0.1");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(30) });
        var service = new UpdateService(TestRepoOwner, TestRepoName, "1.0.0.0");

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("1.0.0.1", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithMajorMinorOnly_ShouldCompareCorrectly()
    {
        var handlerMock = CreateMockReleaseHandler("v2.0", "Release 2.0", "https://github.com/test/releases/tag/v2.0");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.NotNull(result);
        Assert.True(result.UpdateAvailable);
        Assert.Equal("2.0", result.LatestVersion);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WithJsonMissingTagNameField_ShouldReturnNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"name\":\"Release\",\"html_url\":\"https://github.com/test\"}",
                    System.Text.Encoding.UTF8, "application/json")
            });
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        var result = await service.CheckForUpdateAsync();

        Assert.Null(result);
    }

    private static Mock<HttpMessageHandler> CreateMockReleaseHandler(string tagName, string name, string htmlUrl)
    {
        var json = JsonSerializer.Serialize(new
        {
            tag_name = tagName,
            name,
            html_url = htmlUrl
        });

        return CreateMockReleaseHandler(HttpStatusCode.OK, json);
    }

    private static Mock<HttpMessageHandler> CreateMockReleaseHandler(HttpStatusCode statusCode, string responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }

    private static UpdateService CreateServiceWithMockHandler(HttpMessageHandler handler)
    {
        SetStaticHttpClient(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });
        return new UpdateService(TestRepoOwner, TestRepoName, TestCurrentVersion);
    }

    private static void SetStaticHttpClient(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("CreateBatchFilesForXbox360XBLAGames");
        var field = typeof(UpdateService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, httpClient);
    }

    private static void ResetGlobalCts()
    {
        var ctsField = typeof(UpdateService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
        var oldCts = ctsField?.GetValue(null) as CancellationTokenSource;
        try
        {
            oldCts?.Dispose();
        }
        catch
        {
            // ignored
        }

        ctsField?.SetValue(null, new CancellationTokenSource());
    }
}
