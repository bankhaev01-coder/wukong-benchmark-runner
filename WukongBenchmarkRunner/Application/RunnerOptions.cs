namespace WukongBenchmarkRunner.Application;

public sealed record RunnerOptions(
    string Command,
    string? SteamPath,
    string? ConfigPath,
    string? ResultsPath,
    string? OutputPath,
    TimeSpan Timeout)
{
    public static RunnerOptions Parse(string[] args)
    {
        var command = args.FirstOrDefault() is { } firstArgument && !firstArgument.StartsWith("--", StringComparison.Ordinal)
            ? firstArgument
            : "run";
        return new(
            command.ToLowerInvariant(),
            GetValue(args, "--steam-path"),
            GetValue(args, "--config"),
            GetValue(args, "--results-path"),
            GetValue(args, "--output"),
            TimeSpan.FromMinutes(ParsePositiveInt(GetValue(args, "--timeout-minutes"), 30)));
    }

    private static string? GetValue(IReadOnlyList<string> args, string name)
    {
        var index = Array.FindIndex(args.ToArray(), argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Count ? args[index + 1] : null;
    }

    private static int ParsePositiveInt(string? value, int fallback) =>
        int.TryParse(value, out var result) && result > 0 ? result : fallback;
}