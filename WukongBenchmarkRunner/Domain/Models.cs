namespace WukongBenchmarkRunner.Domain;

public sealed record BenchmarkSettings(IReadOnlyDictionary<string, string> Values);

public sealed record BenchmarkProfile(string Name, string Purpose, BenchmarkSettings Settings);

public sealed record BenchmarkToolInstallation(
    string SteamExecutable,
    string InstallationDirectory,
    string ConfigurationPath,
    IReadOnlyList<string> ResultDirectories,
    string? BenchmarkExecutable);

public sealed record MachineInfo(
    string Cpu,
    IReadOnlyList<string> Gpus,
    double MemoryGiB,
    string OperatingSystem,
    int LogicalProcessors);

public sealed record BenchmarkMetrics(
    double? AverageFps,
    double? MinimumFps,
    double? MaximumFps,
    double? Score,
    string? Source,
    IReadOnlyList<string> Warnings)
{
    public string Summary =>
        $"avg FPS={Format(AverageFps)}, min FPS={Format(MinimumFps)}, " +
        $"max FPS={Format(MaximumFps)}, score={Format(Score)}";

    private static string Format(double? value) => value?.ToString("F2") ?? "n/a";
}

public sealed record BenchmarkRun(
    string ProfileName,
    string Purpose,
    BenchmarkSettings Settings,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    BenchmarkMetrics Metrics);

public sealed record BenchmarkReport(
    string ToolVersion,
    DateTimeOffset CreatedAt,
    MachineInfo Machine,
    BenchmarkToolInstallation Installation,
    IReadOnlyList<BenchmarkRun> Runs,
    IReadOnlyList<string> Warnings);