using System.Net;
using System.Reflection;
using Moq;
using Moq.Protected;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class BugReportServiceTests
{
    private const string TestAppName = "TestApp";
    private const string TestAppVersion = "2.0.0";

    [Fact]
    public void HttpClient_ShouldHaveFiveSecondTimeout()
    {
        var field = typeof(BugReportService).GetField("HttpClient", BindingFlags.NonPublic | BindingFlags.Static);
        var httpClient = field?.GetValue(null) as HttpClient;
        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromSeconds(5), httpClient.Timeout);
    }

    [Fact]
    public async Task SendAsync_ShouldSendPostRequest()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldSendToCorrectUrl()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.RequestUri != null && m.RequestUri.ToString().Contains("send-bug-report")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldSetApiKeyHeader()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => HasApiKeyHeader(m)),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldSendJsonPayloadWithAllFields()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test message", TestAppName, "1.0.0", "Windows 10", "at StackTrace()");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldReturnEarlyWhenGlobalCtsIsCancelled()
    {
        BugReportService.CancelAll();
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);

        ResetGlobalCts();
    }

    [Fact]
    public Task SendAsync_ShouldHandleOperationCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        return BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);
    }

    [Fact]
    public Task SendAsync_ShouldHandleGeneralException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Network error"));
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        return BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);
    }

    [Fact]
    public Task SendAsync_ShouldHandleServerError()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.InternalServerError, "Internal error");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        return BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);
    }

    [Fact]
    public Task SendAsync_ShouldHandleNotFound()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.NotFound, "Not found");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        return BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);
    }

    [Fact]
    public async Task SendAsync_WithEmptyMessage_ShouldStillSend()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync(string.Empty, TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ShouldStillSend()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync(null!, TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithAllOptionalParamsNull_ShouldUseDefaults()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test message", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldSetContentTypeHeader()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(static m => m.Content != null && m.Content.Headers.ContentType != null),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public void CancelAll_ShouldReplaceGlobalCts()
    {
        ResetGlobalCts();

        var ctsField = typeof(BugReportService).GetField("_globalCts", BindingFlags.NonPublic | BindingFlags.Static);
        var oldCts = ctsField?.GetValue(null) as CancellationTokenSource;

        BugReportService.CancelAll();

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

        BugReportService.CancelAll();
        BugReportService.CancelAll();
        BugReportService.CancelAll();

        ResetGlobalCts();
    }

    [Fact]
    public async Task CancelAll_ShouldAllowSubsequentRequests()
    {
        ResetGlobalCts();
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        BugReportService.CancelAll();
        await BugReportService.SendAsync("Should send after reset", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

        ResetGlobalCts();
    }

    [Fact]
    public async Task SendAsync_ShouldSendWithCancellationToken()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.Is<CancellationToken>(static ct => ct.CanBeCanceled));
    }

    [Fact]
    public async Task SendAsync_WithMultilineMessage_ShouldSendCorrectly()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Line 1\nLine 2\nLine 3", TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithLongMessage_ShouldSendCorrectly()
    {
        var longMessage = new string('A', 10000);
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync(longMessage, TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithSpecialCharacters_ShouldSendCorrectly()
    {
        const string specialMessage = "Error: <html> & \"quotes\" 'single' /path\\file";
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync(specialMessage, TestAppName, TestAppVersion, null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public Task SendAsync_ShouldHandleHttpRequestException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        return BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);
    }

    [Fact]
    public Task SendAsync_ShouldHandleTaskCanceledException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        return BugReportService.SendAsync("Test bug message", TestAppName, TestAppVersion, null, null);
    }

    [Fact]
    public async Task SendAsync_WithVersionOverride_ShouldUseProvidedVersion()
    {
        var handlerMock = CreateMockHttpHandler(HttpStatusCode.OK, "{}");
        SetStaticHttpClient(new HttpClient(handlerMock.Object) { Timeout = TimeSpan.FromSeconds(5) });

        await BugReportService.SendAsync("Test", TestAppName, "3.0.0-custom", null, null);

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
        }

        ctsField?.SetValue(null, new CancellationTokenSource());
    }

    private static bool HasApiKeyHeader(HttpRequestMessage m)
    {
        return m.Headers.TryGetValues("X-API-KEY", out _);
    }
}
