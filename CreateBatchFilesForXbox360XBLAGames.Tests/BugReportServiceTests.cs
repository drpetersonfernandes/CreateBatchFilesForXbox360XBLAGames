using System.Net;
using System.Reflection;
using Moq;
using Moq.Protected;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class BugReportServiceTests
{
    private const string TestApiUrl = "https://test.example.com/api/report";
    private const string TestApiKey = "test-api-key-12345";
    private const string TestAppName = "TestApp";
    private const string TestAppVersion = "2.0.0";

    [Fact]
    public void Constructor_ShouldStoreApiUrl()
    {
        var service = new BugReportService(TestApiUrl, TestApiKey, TestAppName, TestAppVersion);
        var field = typeof(BugReportService).GetField("_apiUrl", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestApiUrl, value);
    }

    [Fact]
    public void Constructor_ShouldStoreApiKey()
    {
        var service = new BugReportService(TestApiUrl, TestApiKey, TestAppName, TestAppVersion);
        var field = typeof(BugReportService).GetField("_apiKey", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestApiKey, value);
    }

    [Fact]
    public void Constructor_ShouldStoreApplicationName()
    {
        var service = new BugReportService(TestApiUrl, TestApiKey, TestAppName, TestAppVersion);
        var field = typeof(BugReportService).GetField("_applicationName", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestAppName, value);
    }

    [Fact]
    public void Constructor_ShouldStoreApplicationVersion()
    {
        var service = new BugReportService(TestApiUrl, TestApiKey, TestAppName, TestAppVersion);
        var field = typeof(BugReportService).GetField("_applicationVersion", BindingFlags.NonPublic | BindingFlags.Instance);
        var value = field?.GetValue(service) as string;
        Assert.Equal(TestAppVersion, value);
    }

    [Fact]
    public void HttpClient_ShouldHaveFiveSecondTimeout()
    {
        var field = typeof(BugReportService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        var httpClient = field?.GetValue(null) as HttpClient;
        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldSendPostRequest()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test bug message");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldSendToCorrectUrl()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test bug message");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.RequestUri != null && m.RequestUri.ToString() == TestApiUrl),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldSetApiKeyHeader()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test bug message");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => HasApiKeyHeader(m, TestApiKey)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldSendJsonPayloadWithAllFields()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test message", "1.0.0", "Windows 10", "at StackTrace()");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldReturnEarlyWhenGlobalCtsIsCancelled()
    {
        BugReportService.CancelAll();
        var service = CreateServiceWithMockHandler(new Mock<HttpMessageHandler>().Object);

        await service.SendBugReportAsync("Test bug message");

        ResetGlobalCts();
    }

    [Fact]
    public Task SendBugReportAsync_ShouldHandleOperationCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendBugReportAsync("Test bug message");

        // Should not throw
    }

    [Fact]
    public Task SendBugReportAsync_ShouldHandleGeneralException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Network error"));
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendBugReportAsync("Test bug message");

        // Should not throw
    }

    [Fact]
    public Task SendBugReportAsync_ShouldHandleServerError()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.InternalServerError, "Internal error");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendBugReportAsync("Test bug message");

        // Should not throw
    }

    [Fact]
    public Task SendBugReportAsync_ShouldHandleNotFound()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.NotFound, "Not found");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendBugReportAsync("Test bug message");

        // Should not throw
    }

    [Fact]
    public async Task SendBugReportAsync_WithEmptyMessage_ShouldStillSend()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync(string.Empty);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_WithNullMessage_ShouldStillSend()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync(null!);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_WithAllOptionalParamsNull_ShouldUseDefaults()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test message");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldSetContentTypeHeader()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test bug message");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Content != null && m.Content.Headers.ContentType != null),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public void CancelAll_ShouldCancelGlobalToken()
    {
        ResetGlobalCts();

        BugReportService.CancelAll();

        var ctsField = typeof(BugReportService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
        var cts = ctsField?.GetValue(null) as CancellationTokenSource;
        Assert.True(cts?.IsCancellationRequested ?? false);

        ResetGlobalCts();
    }

    [Fact]
    public void CancelAll_ShouldHandleMultipleCalls()
    {
        ResetGlobalCts();

        BugReportService.CancelAll();
        BugReportService.CancelAll();
        BugReportService.CancelAll();

        // Should not throw
        ResetGlobalCts();
    }

    [Fact]
    public async Task CancelAll_ShouldPreventSubsequentSendRequests()
    {
        ResetGlobalCts();
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        BugReportService.CancelAll();
        await service.SendBugReportAsync("Should not send");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        ResetGlobalCts();
    }

    [Fact]
    public async Task SendBugReportAsync_ShouldSendWithCancellationToken()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test bug message");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.Is<CancellationToken>(static ct => ct.CanBeCanceled));
    }

    [Fact]
    public async Task SendBugReportAsync_WithMultilineMessage_ShouldSendCorrectly()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Line 1\nLine 2\nLine 3");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_WithLongMessage_ShouldSendCorrectly()
    {
        var longMessage = new string('A', 10000);
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync(longMessage);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBugReportAsync_WithSpecialCharacters_ShouldSendCorrectly()
    {
        const string specialMessage = "Error: <html> & \"quotes\" 'single' /path\\file";
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync(specialMessage);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public Task SendBugReportAsync_ShouldHandleHttpRequestException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendBugReportAsync("Test bug message");

        // Should not throw
    }

    [Fact]
    public Task SendBugReportAsync_ShouldHandleTaskCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        return service.SendBugReportAsync("Test bug message");

        // Should not throw
    }

    [Fact]
    public async Task SendBugReportAsync_WithVersionOverride_ShouldUseProvidedVersion()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        var service = CreateServiceWithMockHandler(handlerMock.Object);

        await service.SendBugReportAsync("Test", "3.0.0-custom");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(HttpStatusCode statusCode, string responseContent)
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

    private static BugReportService CreateServiceWithMockHandler(HttpMessageHandler handler)
    {
        SetStaticHttpClient(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) });
        return new BugReportService(TestApiUrl, TestApiKey, TestAppName, TestAppVersion);
    }

    private static void SetStaticHttpClient(HttpClient httpClient)
    {
        var field = typeof(BugReportService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, httpClient);
    }

    private static void ResetGlobalCts()
    {
        var ctsField = typeof(BugReportService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
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

    private static bool HasApiKeyHeader(HttpRequestMessage m, string apiKey)
    {
        return m.Headers.TryGetValues("X-API-KEY", out var values) && values.Contains(apiKey);
    }
}
