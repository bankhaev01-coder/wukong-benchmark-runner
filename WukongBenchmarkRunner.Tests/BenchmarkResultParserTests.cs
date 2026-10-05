using WukongBenchmarkRunner.Infrastructure;

namespace WukongBenchmarkRunner.Tests;

[TestClass]
public sealed class BenchmarkResultParserTests
{
    [TestMethod]
    public void Parse_ReadsFpsAndScoreFromNewestResultFile()
    {
        using var fixture = new TemporaryDirectory();
        var oldResult = Path.Combine(fixture.Path, "old.log");
        var expectedSource = Path.Combine(fixture.Path, "result.txt");
        File.WriteAllText(oldResult, "Average FPS: 1");
        File.SetLastWriteTimeUtc(oldResult, DateTime.UtcNow.AddMinutes(-10));
        File.WriteAllText(expectedSource, "Average FPS: 73.5\nMinimum FPS: 49,25\nMaximum FPS: 102.0\nScore: 7350");

        var actual = new BenchmarkResultParser().Parse([fixture.Path], DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.AreEqual(73.5, actual.AverageFps);
        Assert.AreEqual(49.25, actual.MinimumFps);
        Assert.AreEqual(102.0, actual.MaximumFps);
        Assert.AreEqual(7350.0, actual.Score);
        Assert.AreEqual(expectedSource, actual.Source);
        Assert.IsEmpty(actual.Warnings);
    }

    [TestMethod]
    public void Parse_ReturnsWarningWhenNoFreshResultExists()
    {
        using var fixture = new TemporaryDirectory();

        var actual = new BenchmarkResultParser().Parse([fixture.Path], DateTimeOffset.UtcNow);

        Assert.IsNull(actual.AverageFps);
        Assert.IsNull(actual.Source);
        Assert.Contains("Файл результата после запуска не найден.", actual.Warnings);
    }

    [TestMethod]
    public void Parse_DoesNotUseUnrelatedNumberBetweenMetricNameAndValue()
    {
        using var fixture = new TemporaryDirectory();
        var resultPath = Path.Combine(fixture.Path, "result.txt");
        File.WriteAllText(resultPath, "Average FPS (pass 2): 73.5\nMinimum FPS: 49.25");

        var actual = new BenchmarkResultParser().Parse([fixture.Path], DateTimeOffset.MinValue);

        Assert.AreEqual(73.5, actual.AverageFps);
        Assert.AreEqual(49.25, actual.MinimumFps);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wukong-runner-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}