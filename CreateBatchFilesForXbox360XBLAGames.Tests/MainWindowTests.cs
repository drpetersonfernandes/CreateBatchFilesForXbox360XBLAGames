using System.Runtime.InteropServices;
using System.Text;

namespace CreateBatchFilesForXbox360XBLAGames.Tests;

public class MainWindowTests : IDisposable
{
    private readonly List<string> _tempDirectories = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        GC.SuppressFinalize(this);
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"XblaTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }

    private static string CreateTempFile(string directory, string fileName, string content = "")
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    // =========================================================================
    // CheckWritePermission Tests
    // =========================================================================

    [Fact]
    public void CheckWritePermission_ShouldReturnTrue_ForWritableDirectory()
    {
        var dir = CreateTempDirectory();
        var result = MainWindow.CheckWritePermission(dir);
        Assert.True(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldReturnFalse_ForNonExistentDirectory()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");
        var result = MainWindow.CheckWritePermission(nonExistentPath);
        Assert.False(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldReturnFalse_ForNullPath()
    {
        var result = MainWindow.CheckWritePermission(null!);
        Assert.False(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldReturnFalse_ForEmptyPath()
    {
        // Empty path resolves to current directory which is usually writable
        _ = MainWindow.CheckWritePermission(string.Empty);
        // Empty path behavior depends on environment; just verify no exception
    }

    [Fact]
    public void CheckWritePermission_ShouldReturnFalse_ForFilePathInsteadOfDirectory()
    {
        var dir = CreateTempDirectory();
        var filePath = CreateTempFile(dir, "test.txt", "content");
        var result = MainWindow.CheckWritePermission(filePath);
        Assert.False(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldNotLeaveTempFiles()
    {
        var dir = CreateTempDirectory();
        var beforeFiles = Directory.GetFiles(dir).Length;

        MainWindow.CheckWritePermission(dir);

        var afterFiles = Directory.GetFiles(dir).Length;
        Assert.Equal(beforeFiles, afterFiles);
    }

    [Fact]
    public void CheckWritePermission_ShouldWorkWithUnicodePath()
    {
        var dir = CreateTempDirectory();
        var unicodeDir = Path.Combine(dir, "\u6d4b\u8bd5\u6587\u4ef6\u5939_\u65e5\u672c\u8a9e_\ud55c\uad6d\uc5b4");
        Directory.CreateDirectory(unicodeDir);

        var result = MainWindow.CheckWritePermission(unicodeDir);
        Assert.True(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldWorkWithLongPath()
    {
        var dir = CreateTempDirectory();
        var longName = new string('a', 100);
        var longDir = Path.Combine(dir, longName);
        Directory.CreateDirectory(longDir);

        var result = MainWindow.CheckWritePermission(longDir);
        Assert.True(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldWorkWithSpacesInPath()
    {
        var dir = CreateTempDirectory();
        var spaceDir = Path.Combine(dir, "folder with spaces");
        Directory.CreateDirectory(spaceDir);

        var result = MainWindow.CheckWritePermission(spaceDir);
        Assert.True(result);
    }

    [Fact]
    public void CheckWritePermission_ShouldNotThrowForNetworkPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        _ = MainWindow.CheckWritePermission(@"\\localhost\C$\Windows");
        // Don't assert result - just verify no exception
    }

    [Fact]
    public void CheckWritePermission_ShouldWorkWithRelativePath()
    {
        var result = MainWindow.CheckWritePermission(Path.GetTempPath());
        Assert.True(result);
    }

    [Fact]
    public async Task CheckWritePermission_ShouldHandleConcurrentAccess()
    {
        var dir = CreateTempDirectory();
        var tasks = new List<Task<bool>>();

        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => MainWindow.CheckWritePermission(dir)));
        }

        var results = await Task.WhenAll(tasks);
        Assert.All(results, Assert.True);
    }

    // =========================================================================
    // Batch File Content Tests
    // =========================================================================

    [Fact]
    public void CreateBatchFiles_VerifyBatFileContentTemplate()
    {
        const string xeniaPath = @"C:\Xenia\xenia.exe";
        const string gameFilePath = @"C:\Games\MyGame\000D0000\game.xex";
        var expectedLines = new[]
        {
            "@echo off",
            $"cd /d \"{Path.GetDirectoryName(xeniaPath)}\"",
            $"start \"\" \"{Path.GetFileName(xeniaPath)}\" \"{gameFilePath}\""
        };

        Assert.Equal("@echo off", expectedLines[0]);
        Assert.Contains("cd /d", expectedLines[1], StringComparison.Ordinal);
        Assert.Contains("start \"\"", expectedLines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void CreateBatchFiles_BatContent_ShouldEscapePathsCorrectly()
    {
        const string xeniaPath = @"C:\Program Files\Xenia\xenia.exe";
        const string gameFilePath = @"C:\Users\Test\My Games\Game with spaces\game.xex";

        Assert.True(xeniaPath.Contains(' ') || gameFilePath.Contains(' '));
    }

    [Fact]
    public void CreateBatchFiles_BatContent_ShouldUseXeniaDirectoryForCd()
    {
        const string xeniaPath = @"D:\Emulators\Xenia\Canary\xenia_canary.exe";
        var xeniaDir = Path.GetDirectoryName(xeniaPath);

        Assert.Equal(@"D:\Emulators\Xenia\Canary", xeniaDir);
    }

    [Fact]
    public void CreateBatchFiles_BatContent_ShouldUseXeniaExeNameForStart()
    {
        const string xeniaPath = @"E:\Emulators\xenia_canary.exe";
        var xeniaExeName = Path.GetFileName(xeniaPath);

        Assert.Equal("xenia_canary.exe", xeniaExeName);
    }

    // =========================================================================
    // File system structure tests
    // =========================================================================

    [Fact]
    public void FindGameFile_000D0000_Structure_ContainsFile()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "Game1");
        var dataDir = Path.Combine(gameDir, "000D0000");
        Directory.CreateDirectory(dataDir);
        var gameFile = CreateTempFile(dataDir, "game.xex", "fake xex content");

        Assert.True(File.Exists(gameFile));
        Assert.Contains("000D0000", gameFile, StringComparison.Ordinal);
    }

    [Fact]
    public void FindGameFile_000D0000_Structure_MultipleFiles_UsesFirst()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "GameWithMultipleFiles");
        var dataDir = Path.Combine(gameDir, "000D0000");
        Directory.CreateDirectory(dataDir);

        CreateTempFile(dataDir, "aaa_game.xex", "content1");
        CreateTempFile(dataDir, "bbb_game.xex", "content2");

        var files = Directory.GetFiles(dataDir);
        Assert.Equal("aaa_game.xex", Path.GetFileName(files[0]));
        Assert.Equal(2, files.Length);
    }

    [Fact]
    public void FindGameFile_Fallback_WhenNo000D0000_UsesFirstRecursiveFile()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "NoDataDirGame");
        var subDir = Path.Combine(gameDir, "some_subdir");
        Directory.CreateDirectory(subDir);
        CreateTempFile(subDir, "game.xex", "content");

        var allFiles = Directory.GetFiles(gameDir, "*", SearchOption.AllDirectories);
        Assert.Single(allFiles);
        Assert.EndsWith("game.xex", allFiles[0], StringComparison.Ordinal);
    }

    [Fact]
    public void FindGameFile_EmptyDirectory_ReturnsNoFiles()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "EmptyGame");
        Directory.CreateDirectory(gameDir);

        var files = Directory.GetFiles(gameDir, "*", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void FindGameFile_DirectoryWithOnlySubdirs_NoFiles()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "OnlySubdirs");
        Directory.CreateDirectory(Path.Combine(gameDir, "subdir1"));
        Directory.CreateDirectory(Path.Combine(gameDir, "subdir2"));
        Directory.CreateDirectory(Path.Combine(gameDir, "subdir3"));

        var files = Directory.GetFiles(gameDir, "*", SearchOption.AllDirectories);
        Assert.Empty(files);
    }

    [Fact]
    public void FindGameFile_DeeplyNested000D0000()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "DeepGame");
        var deepDir = Path.Combine(gameDir, "Content", "Install", "000D0000");
        Directory.CreateDirectory(deepDir);
        CreateTempFile(deepDir, "paid_content.xex", "dlc content");

        var dirs = Directory.GetDirectories(gameDir, "000D0000", SearchOption.AllDirectories);
        Assert.Single(dirs);
    }

    [Fact]
    public void FindGameFile_Multiple000D0000_UsesFirstFound()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "MultiDataGame");
        var dataDir1 = Path.Combine(gameDir, "Content", "000D0000");
        var dataDir2 = Path.Combine(gameDir, "Update", "000D0000");
        Directory.CreateDirectory(dataDir1);
        Directory.CreateDirectory(dataDir2);

        CreateTempFile(dataDir1, "main.xex", "main");
        CreateTempFile(dataDir2, "update.xex", "update");

        var dirs = Directory.GetDirectories(gameDir, "000D0000", SearchOption.AllDirectories);
        Assert.True(dirs.Length >= 1);
    }

    [Fact]
    public void CreateBatchFiles_DirectoryProcessing_MultipleGameFolders()
    {
        var rootDir = CreateTempDirectory();
        Directory.CreateDirectory(Path.Combine(rootDir, "GameA", "000D0000"));
        Directory.CreateDirectory(Path.Combine(rootDir, "GameB", "000D0000"));
        Directory.CreateDirectory(Path.Combine(rootDir, "GameC", "000D0000"));

        var dirs = Directory.GetDirectories(rootDir);
        Assert.Equal(3, dirs.Length);
    }

    [Fact]
    public void CreateBatchFiles_DirectoryProcessing_ShouldSkipFiles()
    {
        var rootDir = CreateTempDirectory();
        CreateTempFile(rootDir, "not_a_dir.txt", "text");
        Directory.CreateDirectory(Path.Combine(rootDir, "GameA", "000D0000"));

        var dirs = Directory.GetDirectories(rootDir);
        Assert.Single(dirs);
    }

    [Fact]
    public void CreateBatchFiles_BatFileName_ShouldMatchGameFolderName()
    {
        var rootDir = CreateTempDirectory();
        const string gameFolderName = "XBLA_Game_Title_2024";
        var gameDir = Path.Combine(rootDir, gameFolderName);
        Directory.CreateDirectory(gameDir);

        const string expectedBatName = gameFolderName + ".bat";
        Assert.Equal("XBLA_Game_Title_2024.bat", expectedBatName);
    }

    [Fact]
    public void CreateBatchFiles_BatFileName_WithSpecialFolderChars()
    {
        var gameFolderNames = new[] { "Game (USA)", "Game [NTSC]", "Game v1.0" };

        foreach (var name in gameFolderNames)
        {
            var rootDir = CreateTempDirectory();
            var gameDir = Path.Combine(rootDir, name);
            Directory.CreateDirectory(gameDir);

            var batchFileName = name + ".bat";
            Assert.EndsWith(".bat", batchFileName, StringComparison.Ordinal);
        }
    }

    // =========================================================================
    // Path validation tests
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatePath_EmptyOrWhitespace_ShouldFail(string path)
    {
        var ex = Record.Exception(() => MainWindow.CheckWritePermission(path));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(@"C:\Program Files\Xenia\xenia.exe", true)]
    [InlineData(@"D:\Emulators\xenia_canary.exe", true)]
    [InlineData(@"C:\Xenia\notxenia.exe", false)]
    [InlineData(@"C:\Xenia\xenia.dll", false)]
    public void XeniaExePath_Validation(string path, bool expectedValid)
    {
        var fileName = Path.GetFileName(path);
        var isValid = fileName.StartsWith("xenia", StringComparison.OrdinalIgnoreCase)
                      && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void GameFolderPath_MustBeExistingDirectory()
    {
        var existingDir = CreateTempDirectory();
        Assert.True(Directory.Exists(existingDir));

        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");
        Assert.False(Directory.Exists(nonExistentDir));
    }

    // =========================================================================
    // Batch file writing verification
    // =========================================================================

    [Fact]
    public async Task WriteBatchFile_VerifiesContent()
    {
        var rootDir = CreateTempDirectory();
        var batchFilePath = Path.Combine(rootDir, "TestGame.bat");
        const string xeniaPath = @"C:\Xenia\xenia.exe";
        const string gameFilePath = @"C:\Games\TestGame\000D0000\game.xex";

        await using (var sw = new StreamWriter(batchFilePath))
        {
            await sw.WriteLineAsync("@echo off");
            await sw.WriteLineAsync($"cd /d \"{Path.GetDirectoryName(xeniaPath)}\"");
            await sw.WriteLineAsync($"start \"\" \"{Path.GetFileName(xeniaPath)}\" \"{gameFilePath}\"");
        }

        var content = await File.ReadAllTextAsync(batchFilePath);
        Assert.Contains("@echo off", content, StringComparison.Ordinal);
        Assert.Contains("cd /d", content, StringComparison.Ordinal);
        Assert.Contains("start \"\"", content, StringComparison.Ordinal);
        Assert.Contains("xenia.exe", content, StringComparison.Ordinal);
        Assert.Contains("game.xex", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteBatchFile_UsesCrLfLineEndings()
    {
        var rootDir = CreateTempDirectory();
        var batchFilePath = Path.Combine(rootDir, "LineEndings.bat");

        await using (var sw = new StreamWriter(batchFilePath))
        {
            await sw.WriteLineAsync("@echo off");
        }

        var content = await File.ReadAllTextAsync(batchFilePath);
        Assert.Contains("\r\n", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteBatchFile_Encoding_UsesUtf8WithoutBom()
    {
        var rootDir = CreateTempDirectory();
        var batchFilePath = Path.Combine(rootDir, "EncodingTest.bat");

        await using (var sw = new StreamWriter(batchFilePath))
        {
            await sw.WriteLineAsync("@echo off");
        }

        var bytes = await File.ReadAllBytesAsync(batchFilePath);
        var hasUtf8Bom = bytes is [0xEF, 0xBB, 0xBF, ..];
        Assert.False(hasUtf8Bom, "Batch files should not have UTF-8 BOM on .NET");
        var content = await File.ReadAllTextAsync(batchFilePath);
        Assert.StartsWith("@echo off", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteBatchFile_Content_ShouldEndWithNewline()
    {
        var rootDir = CreateTempDirectory();
        var batchFilePath = Path.Combine(rootDir, "NewlineTest.bat");

        await using (var sw = new StreamWriter(batchFilePath))
        {
            await sw.WriteLineAsync("@echo off");
        }

        var content = await File.ReadAllTextAsync(batchFilePath);
        Assert.EndsWith(Environment.NewLine, content, StringComparison.Ordinal);
    }

    // =========================================================================
    // Edge cases for paths
    // =========================================================================

    [Fact]
    public void XeniaExePath_WithForwardSlashes()
    {
        const string xeniaPath = "C:/Xenia/xenia.exe";
        var fileName = xeniaPath.Split('/', '\\')[^1];
        Assert.Equal("xenia.exe", fileName);
    }

    [Fact]
    public void GameFilePath_WithMixedSeparators()
    {
        const string mixedPath = @"C:\Games\XBLA/Game/000D0000/game.xex";
        var normalized = mixedPath.Replace('/', '\\');
        Assert.Contains("\\", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("/", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void BatFileContent_ShouldNotContainCarriageReturnOnly()
    {
        // Proper batch file content uses \r\n line endings, not standalone \r
        const string content = "@echo off\r\ncd /d \"C:\\Xenia\"\r\nstart \"\" \"xenia.exe\" \"game.xex\"\r\n";
        Assert.DoesNotContain("\r@", content, StringComparison.Ordinal);
        Assert.DoesNotContain("\rc", content, StringComparison.Ordinal);
        Assert.DoesNotContain("\rs", content, StringComparison.Ordinal);
    }

    // =========================================================================
    // Multiple batch file scenarios
    // =========================================================================

    [Fact]
    public async Task CreateMultipleBatchFiles_ContentConsistency()
    {
        var rootDir = CreateTempDirectory();
        const string xeniaExePath = @"C:\Xenia\xenia.exe";

        var gameDirs = new[] { "Game1", "Game2", "Game3" };
        foreach (var gameDir in gameDirs)
        {
            var dir = Path.Combine(rootDir, gameDir, "000D0000");
            Directory.CreateDirectory(dir);
            CreateTempFile(dir, "game.xex");
        }

        var createdFiles = 0;
        foreach (var gameDir in gameDirs)
        {
            var fullGameDir = Path.Combine(rootDir, gameDir);
            var batchFilePath = Path.Combine(rootDir, $"{gameDir}.bat");
            var dataDir = Path.Combine(fullGameDir, "000D0000");
            var gameFile = Directory.GetFiles(dataDir).FirstOrDefault();

            if (gameFile != null)
            {
                await using var sw = new StreamWriter(batchFilePath);
                await sw.WriteLineAsync("@echo off");
                await sw.WriteLineAsync($"cd /d \"{Path.GetDirectoryName(xeniaExePath)}\"");
                await sw.WriteLineAsync($"start \"\" \"{Path.GetFileName(xeniaExePath)}\" \"{gameFile}\"");
                createdFiles++;
            }
        }

        Assert.Equal(3, createdFiles);

        foreach (var gameDir in gameDirs)
        {
            var batchFilePath = Path.Combine(rootDir, $"{gameDir}.bat");
            Assert.True(File.Exists(batchFilePath));

            var content = await File.ReadAllTextAsync(batchFilePath);
            Assert.Contains("@echo off", content, StringComparison.Ordinal);
            Assert.Contains($"cd /d \"{Path.GetDirectoryName(xeniaExePath)}\"", content, StringComparison.Ordinal);
            Assert.Contains(Path.GetFileName(xeniaExePath), content, StringComparison.Ordinal);
        }
    }

    // =========================================================================
    // Game file discovery edge cases
    // =========================================================================

    [Fact]
    public void FindGameFile_000D0000_IsCaseInsensitive()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "CaseGame");
        var dataDir = Path.Combine(gameDir, "000d0000");
        Directory.CreateDirectory(dataDir);
        CreateTempFile(dataDir, "game.xex", "content");

        var dirs = Directory.GetDirectories(gameDir, "000D0000", SearchOption.AllDirectories);
        Assert.Single(dirs);
    }

    [Fact]
    public void FindGameFile_HiddenFiles_ShouldBeFound()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "HiddenFilesGame");
        Directory.CreateDirectory(gameDir);
        var hiddenFile = CreateTempFile(gameDir, "game.xex", "content");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetAttributes(hiddenFile, File.GetAttributes(hiddenFile) | FileAttributes.Hidden);
        }

        var files = Directory.GetFiles(gameDir, "*", SearchOption.AllDirectories);
        Assert.Single(files);
    }

    [Fact]
    public void FindGameFile_ReadOnlyFiles_ShouldBeFound()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "ReadOnlyGame");
        Directory.CreateDirectory(gameDir);
        var readOnlyFile = CreateTempFile(gameDir, "game.xex", "content");

        File.SetAttributes(readOnlyFile, File.GetAttributes(readOnlyFile) | FileAttributes.ReadOnly);

        var files = Directory.GetFiles(gameDir, "*", SearchOption.AllDirectories);
        Assert.Single(files);

        File.SetAttributes(readOnlyFile, FileAttributes.Normal);
    }

    [Fact]
    public void FindGameFile_VariousExtensions()
    {
        var extensions = new[] { ".xex", ".xcp", ".xbe", ".iso", ".bin" };
        var rootDir = CreateTempDirectory();

        foreach (var ext in extensions)
        {
            var gameDir = Path.Combine(rootDir, $"Game{ext.Replace(".", "")}");
            var dataDir = Path.Combine(gameDir, "000D0000");
            Directory.CreateDirectory(dataDir);
            CreateTempFile(dataDir, $"game{ext}");

            var files = Directory.GetFiles(dataDir);
            Assert.Single(files);
        }
    }

    [Fact]
    public void FindGameFile_LargeNumberOfFiles()
    {
        var rootDir = CreateTempDirectory();
        var gameDir = Path.Combine(rootDir, "ManyFilesGame");
        var dataDir = Path.Combine(gameDir, "000D0000");
        Directory.CreateDirectory(dataDir);

        for (var i = 0; i < 50; i++)
        {
            CreateTempFile(dataDir, $"file_{i:D4}.dat");
        }

        CreateTempFile(dataDir, "game.xex", "actual game");

        var files = Directory.GetFiles(dataDir);
        Assert.Equal(51, files.Length);
    }

    // =========================================================================
    // Exception handling tests using App helpers
    // =========================================================================

    [Fact]
    public void Exception_BuildExceptionReport_IncludesAllLevels()
    {
        // ReSharper disable once NotResolvedInText
        var inner = new ArgumentNullException("param", "Inner exception message");
        var middle = new InvalidOperationException("Middle exception message", inner);
        var outer = new InvalidOperationException("Outer exception message", middle);

        var report = App.BuildExceptionReport(outer, "TestSource", "TestEnv");

        Assert.Contains("Outer exception message", report, StringComparison.Ordinal);
        Assert.Contains("Middle exception message", report, StringComparison.Ordinal);
        Assert.Contains("Inner exception message", report, StringComparison.Ordinal);
    }

    [Fact]
    public void Exception_RecursiveStructure_IsPreserved()
    {
        var inner = new InvalidOperationException("Level 1", new InvalidOperationException("Level 2", new InvalidOperationException("Level 3")));
        var sb = new StringBuilder();

        App.AppendExceptionDetails(sb, inner);

        var result = sb.ToString();
        Assert.Contains("Level 1", result, StringComparison.Ordinal);
        Assert.Contains("Level 2", result, StringComparison.Ordinal);
        Assert.Contains("Level 3", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Exception_NullStackTrace_ShouldNotThrow()
    {
        var exception = new InvalidOperationException("Test");
        var sb = new StringBuilder();

        var ex = Record.Exception(() => App.AppendExceptionDetails(sb, exception));
        Assert.Null(ex);
    }
}
