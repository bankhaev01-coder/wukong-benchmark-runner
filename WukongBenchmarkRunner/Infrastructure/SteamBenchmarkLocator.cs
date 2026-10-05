using System.Text.RegularExpressions;
using Microsoft.Win32;
using WukongBenchmarkRunner.Domain;

namespace WukongBenchmarkRunner.Infrastructure;

public sealed class SteamBenchmarkLocator
{
    private const string GameDirectoryName = "Black Myth Wukong Benchmark Tool";

    public BenchmarkToolInstallation? Locate(string? steamPath, string? configurationPath)
    {
        var steamExecutable = NormalizeSteamExecutable(steamPath ?? ReadSteamPath());
        if (string.IsNullOrWhiteSpace(steamExecutable) || !File.Exists(steamExecutable)) return null;

        var steamRoot = Path.GetDirectoryName(steamExecutable)!;
        var gameDirectories = DiscoverLibraryRoots(steamRoot)
            .Select(root => Path.Combine(root, "steamapps", "common", GameDirectoryName))
            .Where(Directory.Exists)
            .ToArray();
        var installation = gameDirectories.FirstOrDefault();
        var config = configurationPath ?? FindConfiguration(gameDirectories);
        if (config is null) return null;

        var root = installation ?? Path.GetDirectoryName(config)!;
        var resultDirectories = new[]
        {
            Path.Combine(root, "b1", "Saved"),
            Path.Combine(root, "Saved"),
            Path.GetDirectoryName(config)!
        }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var executable = installation is null
            ? null
            : Directory.EnumerateFiles(installation, "*.exe", SearchOption.AllDirectories)
                .FirstOrDefault(path => Path.GetFileName(path).Contains("benchmark", StringComparison.OrdinalIgnoreCase));

        return new(steamExecutable, root, config, resultDirectories, executable);
    }

    private static string? ReadSteamPath() =>
        Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") as string;

    private static string? NormalizeSteamExecutable(string? path) =>
        string.IsNullOrWhiteSpace(path)
            ? null
            : Directory.Exists(path)
                ? Path.Combine(path, "steam.exe")
                : path;

    private static string? FindConfiguration(IEnumerable<string> gameDirectories) =>
        gameDirectories.SelectMany(root => new[]
        {
            Path.Combine(root, "b1", "Saved", "Config", "Windows", "GameUserSettings.ini"),
            Path.Combine(root, "b1", "Saved", "Config", "WindowsNoEditor", "GameUserSettings.ini")
        }).FirstOrDefault(File.Exists);

    private static IEnumerable<string> DiscoverLibraryRoots(string steamRoot)
    {
        yield return steamRoot;
        var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFile)) yield break;

        foreach (Match match in Regex.Matches(
                     File.ReadAllText(libraryFile),
                     "\\\"path\\\"\\s+\\\"(?<path>[^\\\"]+)\\\"",
                     RegexOptions.IgnoreCase))
        {
            yield return match.Groups["path"].Value.Replace("\\\\", "\\");
        }
    }
}