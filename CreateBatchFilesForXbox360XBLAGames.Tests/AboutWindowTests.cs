using System.Reflection;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class AboutWindowTests
{
    [Fact]
    public void GetApplicationVersion_ShouldReturnValidVersionString()
    {
        var version = AboutWindow.GetApplicationVersion();
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public void GetApplicationVersion_ShouldNotBeNull()
    {
        var version = AboutWindow.GetApplicationVersion();
        Assert.NotNull(version);
    }

    [Fact]
    public void GetApplicationVersion_ShouldMatchAssemblyVersion()
    {
        var assembly = Assembly.GetAssembly(typeof(App))!;
        var expectedVersion = assembly.GetName().Version?.ToString(3);
        var actualVersion = AboutWindow.GetApplicationVersion();

        Assert.Equal(expectedVersion, actualVersion);
    }

    [Fact]
    public void GetApplicationVersion_ShouldHaveCorrectFormat()
    {
        var version = AboutWindow.GetApplicationVersion();
        // Assembly.GetName().Version produces format like "1.6.0.0" - should parse as Version
        Assert.True(Version.TryParse(version, out var parsed));
        Assert.True(parsed.Major >= 0);
    }

    [Fact]
    public void GetApplicationVersion_ShouldReturnSameResultOnMultipleCalls()
    {
        var version1 = AboutWindow.GetApplicationVersion();
        var version2 = AboutWindow.GetApplicationVersion();
        var version3 = AboutWindow.GetApplicationVersion();

        Assert.Equal(version1, version2);
        Assert.Equal(version2, version3);
    }

    [Fact]
    public void GetApplicationVersion_ShouldNotThrowException()
    {
        var exception = Record.Exception(static () => AboutWindow.GetApplicationVersion());
        Assert.Null(exception);
    }

    [Fact]
    public void GetApplicationVersion_ShouldBeConsistentWithAssemblyName()
    {
        var assemblyName = Assembly.GetAssembly(typeof(App))!.GetName();

        Assert.NotNull(assemblyName);
        Assert.NotNull(assemblyName.Version);

        var expected = assemblyName.Version!.ToString(3);
        var actual = AboutWindow.GetApplicationVersion();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetApplicationVersion_ShouldBeStatic()
    {
        var method = typeof(AboutWindow).GetMethod("GetApplicationVersion", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }
}
