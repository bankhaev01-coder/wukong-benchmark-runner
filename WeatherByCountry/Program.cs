using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WeatherByCountry;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<int> Main(string[] args)
    {
        var citiesFile = args.Length > 0
            ? Path.GetFullPath(args[0])
            : Path.Combine(AppContext.BaseDirectory, "Cities.txt");

        if (!File.Exists(citiesFile))
        {
            Console.Error.WriteLine($"Файл со списком городов не найден: {citiesFile}");
            return 1;
        }

        var cities = (await File.ReadAllLinesAsync(citiesFile))
            .Select(city => city.Trim())
            .Where(city => city.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (cities.Length == 0)
        {
            Console.Error.WriteLine("Файл со списком городов пуст.");
            return 1;
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WeatherByCountry", "1.0"));

        var weatherByCity = new List<WeatherData>();
        foreach (var city in cities)
        {
            try
            {
                var encodedCity = Uri.EscapeDataString(city);
                var json = await client.GetStringAsync($"https://wttr.in/{encodedCity}?format=j1");
                var response = JsonSerializer.Deserialize<WeatherResponse>(json, JsonOptions)
                    ?? throw new JsonException("API вернул пустой ответ.");
                var current = response.CurrentCondition?.FirstOrDefault()
                    ?? throw new JsonException("В ответе отсутствуют текущие погодные условия.");
                var area = response.NearestArea?.FirstOrDefault()
                    ?? throw new JsonException("В ответе отсутствуют сведения о местоположении.");

                if (!double.TryParse(current.TemperatureC, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
                {
                    throw new JsonException($"Некорректное значение температуры: {current.TemperatureC}.");
                }
                var country = area.Country?.FirstOrDefault()?.Value;
                if (string.IsNullOrWhiteSpace(country))
                {
                    throw new JsonException("В ответе отсутствует страна.");
                }

                var result = new WeatherData(city, temperature, country);
                weatherByCity.Add(result);
                Console.WriteLine($"{result.City}, {result.Country} {FormatTemperature(result.TemperatureC)} °C");
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or FormatException)
            {
                Console.Error.WriteLine($"Не удалось получить погоду для «{city}»: {exception.Message}");
            }
        }

        if (weatherByCity.Count == 0)
        {
            Console.Error.WriteLine("Не удалось получить погодные данные ни для одного города.");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Сводка по странам:");
        foreach (var group in weatherByCity
                     .GroupBy(weather => weather.Country, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var temperatures = group.Select(weather => weather.TemperatureC).ToArray();
            var average = temperatures.Average();
            var minimum = temperatures.Min();
            var maximum = temperatures.Max();
            Console.WriteLine(
                $"{group.Key} — городов: {temperatures.Length}, средняя: {FormatTemperature(average)} °C, " +
                $"минимум: {FormatTemperature(minimum)} °C, максимум: {FormatTemperature(maximum)} °C");
        }

        return 0;
    }

    private static string FormatTemperature(double temperature) =>
        $"{temperature.ToString("+0.##;-0.##;+0", CultureInfo.InvariantCulture)}";

    private sealed record WeatherData(string City, double TemperatureC, string Country);

    private sealed class WeatherResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("current_condition")]
        public List<CurrentCondition>? CurrentCondition { get; init; }

        [System.Text.Json.Serialization.JsonPropertyName("nearest_area")]
        public List<NearestArea>? NearestArea { get; init; }
    }

    private sealed class CurrentCondition
    {
        [System.Text.Json.Serialization.JsonPropertyName("temp_C")]
        public string TemperatureC { get; init; } = "";
    }

    private sealed class NearestArea
    {
        [System.Text.Json.Serialization.JsonPropertyName("country")]
        public List<LocalizedValue>? Country { get; init; }
    }

    private sealed class LocalizedValue
    {
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; init; } = "";
    }
}