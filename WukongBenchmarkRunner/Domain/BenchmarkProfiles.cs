namespace WukongBenchmarkRunner.Domain;

public static class BenchmarkProfiles
{
    public static IReadOnlyList<BenchmarkProfile> All { get; } =
    [
        new(
            "CPU-test",
            "Снизить вероятность упора в GPU и сохранить нагрузку на обработку сцены и дальность видимости.",
            new BenchmarkSettings(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ResolutionSizeX"] = "1280",
                ["ResolutionSizeY"] = "720",
                ["sg.ResolutionQuality"] = "50",
                ["sg.ViewDistanceQuality"] = "3",
                ["sg.AntiAliasingQuality"] = "0",
                ["sg.ShadowQuality"] = "0",
                ["sg.GlobalIlluminationQuality"] = "0",
                ["sg.ReflectionQuality"] = "0",
                ["sg.PostProcessQuality"] = "0",
                ["sg.TextureQuality"] = "0",
                ["sg.EffectsQuality"] = "0",
                ["sg.FoliageQuality"] = "0",
                ["r.RayTracing"] = "0",
                ["r.DynamicGlobalIlluminationMethod"] = "0",
                ["r.ScreenPercentage"] = "50"
            })),
        new(
            "GPU-test",
            "Создать максимально тяжёлую графическую нагрузку при фиксированном разрешении.",
            new BenchmarkSettings(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sg.ResolutionQuality"] = "100",
                ["sg.ViewDistanceQuality"] = "4",
                ["sg.AntiAliasingQuality"] = "4",
                ["sg.ShadowQuality"] = "4",
                ["sg.GlobalIlluminationQuality"] = "4",
                ["sg.ReflectionQuality"] = "4",
                ["sg.PostProcessQuality"] = "4",
                ["sg.TextureQuality"] = "4",
                ["sg.EffectsQuality"] = "4",
                ["sg.FoliageQuality"] = "4",
                ["r.RayTracing"] = "1",
                ["r.DynamicGlobalIlluminationMethod"] = "1",
                ["r.ScreenPercentage"] = "100"
            }))
    ];
}