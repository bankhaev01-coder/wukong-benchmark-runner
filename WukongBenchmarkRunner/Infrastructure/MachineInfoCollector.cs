using System.Management;
using System.Runtime.InteropServices;
using WukongBenchmarkRunner.Domain;

namespace WukongBenchmarkRunner.Infrastructure;

public sealed class MachineInfoCollector
{
    public MachineInfo Collect()
    {
        var memory = Query("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory")
            .Select(value => long.TryParse(value, out var bytes) ? bytes : 0)
            .FirstOrDefault();
        return new(
            Query("SELECT Name FROM Win32_Processor", "Name").FirstOrDefault() ?? "Unknown",
            Query("SELECT Name FROM Win32_VideoController", "Name"),
            memory / 1024d / 1024 / 1024,
            RuntimeInformation.OSDescription,
            Environment.ProcessorCount);
    }

    private static IReadOnlyList<string> Query(string query, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            return searcher.Get().Cast<ManagementObject>()
                .Select(item => Convert.ToString(item[property])?.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToArray();
        }
        catch (ManagementException)
        {
            return [];
        }
    }
}