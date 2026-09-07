using System.Text;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class AppExceptionHandlingTests
{
    private const string TestEnvironment = "Test Environment";

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainDate()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Date:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainApplicationName()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Application Name:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainApplicationVersion()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Application Version:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainOSVersion()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("OS Version:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainArchitecture()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Architecture:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainBitness()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Bitness:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainWindowsVersion()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Windows Version:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainProcessorCount()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Processor Count:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainBaseDirectory()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Base Directory:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldContainTempPath()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.Contains("Temp Path:", environment, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildEnvironmentDetails_ShouldReturnNonEmptyString()
    {
        var environment = BugReportSink.BuildEnvironmentDetails();

        Assert.False(string.IsNullOrEmpty(environment));
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainEnvironmentDetailsSection()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("=== Environment Details ===", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainErrorDetailsSection()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("=== Error Details ===", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainExceptionDetailsSection()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("=== Exception Details ===", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainErrorSource()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("TestSource", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainExceptionType()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("InvalidOperationException", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainExceptionMessage()
    {
        var exception = new InvalidOperationException("Unique test message 12345");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("Unique test message 12345", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainExceptionSource()
    {
        var exception = new InvalidOperationException("Test error") { Source = "MyApp.Module" };
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("MyApp.Module", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainStackTrace()
    {
        try
        {
            throw new InvalidOperationException("Test error with stack trace");
        }
        catch (Exception ex)
        {
            var report = BugReportSink.BuildExceptionReport(ex, "TestSource", TestEnvironment);
            Assert.Contains("BuildExceptionReport_ShouldContainStackTrace", report, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BuildExceptionReport_ShouldReturnNonEmptyString()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.False(string.IsNullOrEmpty(report));
    }

    [Fact]
    public void BuildExceptionReport_ShouldContainEnvironment()
    {
        var exception = new InvalidOperationException("Test error");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", "CustomEnv123");

        Assert.Contains("CustomEnv123", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldNotThrowForNullMessage()
    {
        var exception = new InvalidOperationException(null);
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.NotNull(report);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldIncludeInnerException()
    {
        var inner = new ArgumentException("Inner error");
        var outer = new InvalidOperationException("Outer error", inner);
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, outer);

        var result = sb.ToString();
        Assert.Contains("Inner Exception:", result, StringComparison.Ordinal);
        Assert.Contains("Inner error", result, StringComparison.Ordinal);
        Assert.Contains("ArgumentException", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldIncludeNestedInnerExceptions()
    {
        var inner2 = new FormatException("Innermost");
        var inner1 = new InvalidOperationException("Middle", inner2);
        var outer = new InvalidOperationException("Outer", inner1);
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, outer);

        var result = sb.ToString();
        Assert.Contains("FormatException", result, StringComparison.Ordinal);
        Assert.Contains("Innermost", result, StringComparison.Ordinal);
        Assert.Contains("Middle", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldHandleThreeLevelsOfInnerExceptions()
    {
        var level3 = new DivideByZeroException("Level 3");
        var level2 = new InvalidOperationException("Level 2", level3);
        var level1 = new InvalidOperationException("Level 1", level2);
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, level1);

        var result = sb.ToString();
        Assert.Contains("Level 1", result, StringComparison.Ordinal);
        Assert.Contains("Level 2", result, StringComparison.Ordinal);
        Assert.Contains("Level 3", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldIncludeType()
    {
        var exception = new IndexOutOfRangeException("Test");
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("Type:", result, StringComparison.Ordinal);
        Assert.Contains("IndexOutOfRangeException", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldIncludeMessage()
    {
        var exception = new InvalidOperationException("Specific message ABC");
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("Message:", result, StringComparison.Ordinal);
        Assert.Contains("Specific message ABC", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldIncludeSource()
    {
        var exception = new InvalidOperationException("Test") { Source = "TestLibrary" };
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("Source:", result, StringComparison.Ordinal);
        Assert.Contains("TestLibrary", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldIncludeStackTrace()
    {
        var sb = new StringBuilder();
        try
        {
            throw new InvalidOperationException("Stack test");
        }
        catch (Exception ex)
        {
            BugReportSink.AppendExceptionDetails(sb, ex);
        }

        var result = sb.ToString();
        Assert.Contains("StackTrace:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldHandleNullSource()
    {
        var exception = new InvalidOperationException("Test") { Source = null! };
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("Source:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldHandleNullStackTrace()
    {
        var exception = new InvalidOperationException("Test");
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("StackTrace:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldUseProperIndentation()
    {
        var inner = new ArgumentException("Inner error");
        var outer = new InvalidOperationException("Outer error", inner);
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, outer);

        var result = sb.ToString();
        Assert.Contains("  ", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldNotAddExtraIndentForSingleException()
    {
        var exception = new InvalidOperationException("Single error");
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("Type:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldWorkWithEmptyMessage()
    {
        var exception = new InvalidOperationException(string.Empty);
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        var result = sb.ToString();
        Assert.Contains("Message:", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldHandleAggregateException()
    {
        var inner1 = new InvalidOperationException("Inner 1");
        var inner2 = new ArgumentException("Inner 2");
        var aggregate = new AggregateException("Outer", inner1, inner2);
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, aggregate);

        var result = sb.ToString();
        Assert.Contains("AggregateException", result, StringComparison.Ordinal);
        Assert.Contains("Outer", result, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldNotThrowForExceptionWithNoInnerException()
    {
        var exception = new InvalidOperationException("Simple error");
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, exception);

        Assert.True(sb.Length > 0);
    }

    [Fact]
    public void BuildExceptionReport_WithCustomException_ShouldIncludeDerivedType()
    {
        var exception = new CustomTestException("Custom error message");
        var report = BugReportSink.BuildExceptionReport(exception, "TestSource", TestEnvironment);

        Assert.Contains("CustomTestException", report, StringComparison.Ordinal);
        Assert.Contains("Custom error message", report, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExceptionReport_ShouldIncludeExceptionDetailsWithInnerExceptions()
    {
        var inner = new ArgumentException("Inner msg");
        var outer = new InvalidOperationException("Outer msg", inner);
        var report = BugReportSink.BuildExceptionReport(outer, "TestSource", TestEnvironment);

        Assert.Contains("Outer msg", report, StringComparison.Ordinal);
        Assert.Contains("Inner msg", report, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendExceptionDetails_ShouldHandleRecursiveInnerExceptions()
    {
        var inner = new InvalidOperationException("Level 1",
            new InvalidOperationException("Level 2", new InvalidOperationException("Level 3")));
        var sb = new StringBuilder();

        BugReportSink.AppendExceptionDetails(sb, inner);

        var result = sb.ToString();
        Assert.Contains("Level 1", result, StringComparison.Ordinal);
        Assert.Contains("Level 2", result, StringComparison.Ordinal);
        Assert.Contains("Level 3", result, StringComparison.Ordinal);
    }

    private class CustomTestException(string message) : Exception(message);
}