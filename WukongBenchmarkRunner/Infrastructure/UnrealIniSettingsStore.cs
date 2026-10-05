using System.Text;
using WukongBenchmarkRunner.Domain;

namespace WukongBenchmarkRunner.Infrastructure;

public sealed class UnrealIniSettingsStore(string path)
{
    private const string Section = "[/Script/Engine.GameUserSettings]";
    private readonly string backupPath = path + ".wukong-runner-backup";

    public void Backup()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path)) File.Copy(path, backupPath, true);
        else File.WriteAllText(backupPath, string.Empty, new UTF8Encoding(false));
    }

    public void Apply(BenchmarkSettings settings)
    {
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : [];
        var sectionIndex = lines.FindIndex(line => string.Equals(line.Trim(), Section, StringComparison.OrdinalIgnoreCase));
        if (sectionIndex < 0)
        {
            lines.Add(Section);
            sectionIndex = lines.Count - 1;
        }

        var sectionEnd = lines.FindIndex(sectionIndex + 1, line => line.StartsWith("[", StringComparison.Ordinal));
        if (sectionEnd < 0) sectionEnd = lines.Count;
        foreach (var setting in settings.Values)
        {
            var prefix = setting.Key + "=";
            var existingIndex = lines.FindIndex(sectionIndex + 1, sectionEnd - sectionIndex - 1,
                line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0) lines[existingIndex] = prefix + setting.Value;
            else lines.Insert(sectionEnd++, prefix + setting.Value);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(false));
        Verify(settings);
    }

    public void Restore()
    {
        if (!File.Exists(backupPath)) return;
        if (new FileInfo(backupPath).Length == 0) File.Delete(path);
        else File.Copy(backupPath, path, true);
        File.Delete(backupPath);
    }

    private void Verify(BenchmarkSettings settings)
    {
        var lines = File.ReadAllLines(path);
        var missing = settings.Values.Where(setting => !lines.Any(line =>
            line.Equals($"{setting.Key}={setting.Value}", StringComparison.OrdinalIgnoreCase))).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException($"Не удалось проверить настройки: {string.Join(", ", missing.Select(x => x.Key))}");
    }
}