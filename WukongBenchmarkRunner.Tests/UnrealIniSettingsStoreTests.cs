using WukongBenchmarkRunner.Domain;
using WukongBenchmarkRunner.Infrastructure;

namespace WukongBenchmarkRunner.Tests;

[TestClass]
public sealed class UnrealIniSettingsStoreTests
{
    [TestMethod]
    public void Apply_UpdatesExistingValueAndAddsMissingValueInGameUserSettingsSection()
    {
        using var fixture = new TemporaryDirectory();
        var configPath = Path.Combine(fixture.Path, "GameUserSettings.ini");
        File.WriteAllText(configPath, "[/Script/Engine.GameUserSettings]\nResolutionSizeX=1920\n[Other]\nKey=Value\n");
        var store = new UnrealIniSettingsStore(configPath);

        store.Backup();
        store.Apply(new BenchmarkSettings(new Dictionary<string, string>
        {
            ["ResolutionSizeX"] = "1280",
            ["r.RayTracing"] = "0"
        }));

        var actual = File.ReadAllText(configPath);
        Assert.Contains("ResolutionSizeX=1280", actual);
        Assert.Contains("r.RayTracing=0", actual);
        Assert.Contains("[Other]", actual);
        Assert.Contains("Key=Value", actual);
        Assert.IsLessThan(
            actual.IndexOf("[Other]", StringComparison.Ordinal),
            actual.IndexOf("r.RayTracing=0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Restore_ReturnsConfigurationToItsOriginalContent()
    {
        using var fixture = new TemporaryDirectory();
        var configPath = Path.Combine(fixture.Path, "GameUserSettings.ini");
        const string expected = "[/Script/Engine.GameUserSettings]\nResolutionSizeX=1920\n";
        File.WriteAllText(configPath, expected);
        var store = new UnrealIniSettingsStore(configPath);

        store.Backup();
        store.Apply(new BenchmarkSettings(new Dictionary<string, string> { ["ResolutionSizeX"] = "1280" }));
        store.Restore();

        var actual = File.ReadAllText(configPath);
        Assert.AreEqual(expected, actual);
        Assert.IsFalse(File.Exists(configPath + ".wukong-runner-backup"));
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