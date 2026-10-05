using System.Text.Json;
using WukongBenchmarkRunner.Domain;
using WukongBenchmarkRunner.Infrastructure;

namespace WukongBenchmarkRunner.Application;

public static class RunnerApplication
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = RunnerOptions.Parse(args);
            var locator = new SteamBenchmarkLocator();
            var installation = locator.Locate(options.SteamPath, options.ConfigPath);
            if (options.Command is "detect" or "dry-run") return PrintDetection(installation, options.Command == "dry-run");
            if (installation is null) throw new InvalidOperationException("Benchmark Tool не найден. Укажите --steam-path и --config после установки приложения через Steam.");
            if (options.Command == "parse") return await ParseOnlyAsync(installation, options);
            if (options.Command != "run") throw new ArgumentException($"Неизвестная команда: {options.Command}. Используйте detect, dry-run, run или parse.");
            return await RunBenchmarksAsync(installation, options);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }

    private static int PrintDetection(BenchmarkToolInstallation? installation, bool includeProfiles)
    {
        var machine = new MachineInfoCollector().Collect();
        Console.WriteLine(JsonSerializer.Serialize(new { machine, installation, profiles = includeProfiles ? BenchmarkProfiles.All : null }, JsonOptions));
        return installation is null ? 2 : 0;
    }

    private static async Task<int> ParseOnlyAsync(BenchmarkToolInstallation installation, RunnerOptions options)
    {
        var roots = options.ResultsPath is null ? installation.ResultDirectories : [options.ResultsPath];
        var metrics = new BenchmarkResultParser().Parse(roots, DateTimeOffset.MinValue);
        await WriteJsonAsync(options.OutputPath, metrics);
        Console.WriteLine(metrics.Summary);
        return 0;
    }

    private static async Task<int> RunBenchmarksAsync(BenchmarkToolInstallation installation, RunnerOptions options)
    {
        var machine = new MachineInfoCollector().Collect();
        var store = new UnrealIniSettingsStore(installation.ConfigurationPath);
        var runs = new List<BenchmarkRun>();
        store.Backup();
        try
        {
            foreach (var profile in BenchmarkProfiles.All)
            {
                Console.WriteLine($"Запуск {profile.Name}: {profile.Purpose}");
                store.Apply(profile.Settings);
                var started = DateTimeOffset.UtcNow;
                await new SteamBenchmarkLauncher().RunAndWaitAsync(installation, options.Timeout, CancellationToken.None);
                var finished = DateTimeOffset.UtcNow;
                var metrics = new BenchmarkResultParser().Parse(installation.ResultDirectories, started);
                Console.WriteLine(metrics.Summary);
                runs.Add(new(profile.Name, profile.Purpose, profile.Settings, started, finished, metrics));
            }
        }
        finally
        {
            store.Restore();
        }

        var report = new BenchmarkReport("1.0.0", DateTimeOffset.Now, machine, installation, runs, []);
        var output = options.OutputPath ?? Path.Combine(Environment.CurrentDirectory, $"wukong-benchmark-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine($"Полный отчёт: {Path.GetFullPath(output)}");
        return 0;
    }

    private static async Task WriteJsonAsync(string? outputPath, object value)
    {
        if (outputPath is null) return;
        await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(value, JsonOptions));
    }
}