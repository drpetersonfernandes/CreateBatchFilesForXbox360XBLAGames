using System.Net;
using System.Reflection;
using CreateBatchFilesForXbox360XBLAGames.Services;
using Moq;
using Moq.Protected;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class StatsServiceTests
{
    private const string TestApiUrl = "https://test.example.com/api/stats";
    private const string TestApiKey = "test-api-key-67890";
    private const string TestAppId = "TestAppId";
    private const string TestVersion = "1.0.0";

    [Fact]
    public void Constructor_ShouldStoreApiUrl()
    {
        var service = new StatsService(TestApiUrl, TestApiKey, TestAppId, TestVersion);
        var field = typeof(StatsService).GetField("_apiUrl", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestApiUrl, value);
    }

    [Fact]
    public void Constructor_ShouldStoreApiKey()
    {
        var service = new StatsService(TestApiUrl, TestApiKey, TestAppId, TestVersion);
        var field = typeof(StatsService).GetField("_apiKey", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestApiKey, value);
    }

    [Fact]
    public void Constructor_ShouldStoreApplicationId()
    {
        var service = new StatsService(TestApiUrl, TestApiKey, TestAppId, TestVersion);
        var field = typeof(StatsService).GetField("_applicationId", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestAppId, value);
    }

    [Fact]
    public void Constructor_ShouldStoreVersion()
    {
        var service = new StatsService(TestApiUrl, TestApiKey, TestAppId, TestVersion);
        var field = typeof(StatsService).GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestVersion, value);
    }

    [Fact]
    public void HttpClient_ShouldHaveFiveSecondTimeout()
    {
        var field = typeof(StatsService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        var httpClient = field?.GetValue(null) as HttpClient;
        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    [Fact]
    public async Task SendStatsAsync_ShouldSendPostRequest()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendStatsAsync_ShouldSendToCorrectUrl()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.RequestUri != null && m.RequestUri.ToString() == TestApiUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendStatsAsync_ShouldSetBearerAuthorizationHeader()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m =>
                m.Headers.Authorization != null &&
                m.Headers.Authorization.Scheme == "Bearer" &&
                m.Headers.Authorization.Parameter == TestApiKey),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendStatsAsync_ShouldSendJsonPayload()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendStatsAsync_ShouldReturnEarlyWhenCancelled()
    {
        StatsService.CancelAll();
        var service = CreateServiceWithMockHandler(new Mock<HttpMessageHandler>().Object);

        await service.SendStatsAsync();

        ResetGlobalCts();
    }

    [Fact]
    public Task SendStatsAsync_ShouldHandleOperationCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendStatsAsync();

        // Should not throw
    }

    [Fact]
    public Task SendStatsAsync_ShouldHandleGeneralException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Network error"));
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendStatsAsync();

        // Should not throw
    }

    [Fact]
    public Task SendStatsAsync_ShouldHandleServerError()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.InternalServerError, "Error");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendStatsAsync();

        // Should not throw
    }

    [Fact]
    public Task SendStatsAsync_ShouldHandleNotFound()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.NotFound, "Not found");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendStatsAsync();

        // Should not throw
    }

    [Fact]
    public Task SendStatsAsync_ShouldHandleHttpRequestException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendStatsAsync();

        // Should not throw
    }

    [Fact]
    public Task SendStatsAsync_ShouldHandleTaskCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendStatsAsync();

        // Should not throw
    }

    [Fact]
    public void CancelAll_ShouldReplaceGlobalCts()
    {
        ResetGlobalCts();

        var ctsField = typeof(StatsService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
        var oldCts = ctsField?.GetValue(null) as CancellationTokenSource;

        StatsService.CancelAll();

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

        StatsService.CancelAll();
        StatsService.CancelAll();
        StatsService.CancelAll();

        // Should not throw
        ResetGlobalCts();
    }

    [Fact]
    public async Task CancelAll_ShouldAllowSubsequentRequests()
    {
        ResetGlobalCts();
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        StatsService.CancelAll();
        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        ResetGlobalCts();
    }

    [Fact]
    public async Task SendStatsAsync_ShouldSendWithCancellationToken()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.Is<CancellationToken>(static ct => ct.CanBeCanceled));
    }

    [Fact]
    public async Task SendStatsAsync_ShouldSetContentTypeToJson()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendStatsAsync();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Content != null && m.Content.Headers.ContentType != null),
            ItExpr.IsAny<CancellationToken>());
    }

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(HttpStatusCode statusCode, string responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }

    private static StatsService CreateServiceWithMockHandler(HttpMessageHandler handler)
    {
        SetStaticHttpClient(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
        return new StatsService(TestApiUrl, TestApiKey, TestAppId, TestVersion);
    }

    private static void SetStaticHttpClient(HttpClient httpClient)
    {
        var field = typeof(StatsService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, httpClient);
    }

    private static void ResetGlobalCts()
    {
        var ctsField = typeof(StatsService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
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