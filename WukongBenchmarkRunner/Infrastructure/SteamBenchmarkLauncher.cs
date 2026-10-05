using System.Diagnostics;
using WukongBenchmarkRunner.Domain;

namespace WukongBenchmarkRunner.Infrastructure;

public sealed class SteamBenchmarkLauncher
{
    public async Task RunAndWaitAsync(BenchmarkToolInstallation installation, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var existingProcessIds = Process.GetProcesses().Select(process => process.Id).ToHashSet();
        using var steam = Process.Start(new ProcessStartInfo(installation.SteamExecutable, "-applaunch 3132990") { UseShellExecute = true });
        var deadline = DateTimeOffset.UtcNow + timeout;
        Process? benchmark = null;
        while (DateTimeOffset.UtcNow < deadline && benchmark is null)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            benchmark = Process.GetProcesses().FirstOrDefault(process =>
                !existingProcessIds.Contains(process.Id) && IsBenchmarkProcess(process, installation.BenchmarkExecutable));
        }

        if (benchmark is null) throw new TimeoutException("Процесс Benchmark Tool не появился. Проверьте авторизацию Steam и установку приложения.");
        using (benchmark)
        {
            var remaining = deadline - DateTimeOffset.UtcNow;
            await benchmark.WaitForExitAsync(cancellationToken).WaitAsync(remaining, cancellationToken);
        }
    }

    private static bool IsBenchmarkProcess(Process process, string? expectedPath)
    {
        try
        {
            return expectedPath is not null
                ? string.Equals(process.MainModule?.FileName, expectedPath, StringComparison.OrdinalIgnoreCase)
                : process.ProcessName.Contains("benchmark", StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return false; }
    }
}