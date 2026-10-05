using System.Globalization;
using System.Text.RegularExpressions;
using WukongBenchmarkRunner.Domain;

namespace WukongBenchmarkRunner.Infrastructure;

public sealed class BenchmarkResultParser
{
    private static readonly string[] Extensions = [".json", ".csv", ".txt", ".log"];

    public BenchmarkMetrics Parse(IEnumerable<string> roots, DateTimeOffset startedAt)
    {
        var file = roots.Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(path => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => startedAt == DateTimeOffset.MinValue
                || File.GetLastWriteTimeUtc(path) >= startedAt.UtcDateTime.AddSeconds(-5))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (file is null) return new(null, null, null, null, null, ["Файл результата после запуска не найден."]);

        var text = File.ReadAllText(file);
        return new(
            Read(text, "average|avg"),
            Read(text, "minimum|min"),
            Read(text, "maximum|max"),
            Read(text, "score"),
            file,
            []);
    }

    private static double? Read(string text, string label)
    {
        var match = Regex.Match(
            text,
            $"(?<![A-Za-z])(?:{label})(?:\\s+fps)?[^:=]{{0,40}}[:=]\\s*([-+]?[0-9]+(?:[.,][0-9]+)?)",
            RegexOptions.IgnoreCase);
        return double.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}