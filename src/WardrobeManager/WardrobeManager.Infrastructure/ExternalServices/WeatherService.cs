using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

public sealed class WeatherService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache) : IWeatherService
{
    private readonly string _apiKey = configuration["WeatherApi:Key"] ?? "YOUR_API_KEY_HERE";
    private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

    public async Task<WeatherData> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
    {
        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Weather API key is not configured.");
        }

        var cacheKey = $"weather_current_{city.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out WeatherData? cachedWeather))
        {
            return cachedWeather!;
        }

        var response = await httpClient.GetFromJsonAsync<OpenWeatherResponse>(
            $"{BaseUrl}?q={city}&appid={_apiKey}&units=metric", ct);

        if (response == null) throw new Exception("Failed to fetch weather data.");

        var temp = response.Main.Temp;
        var firstWeather = response.Weather.FirstOrDefault();
        string condition;
        string? conditionDetail = null;
        if (firstWeather != null)
        {
            condition = firstWeather.Main;
            conditionDetail = firstWeather.Description;
        }
        else
        {
            condition = "Unknown";
        }

        var seasonSuggestion = MapTempToSeason(temp);
        var rainChance = await GetRainChanceAsync(city, condition, ct);

        var result = new WeatherData(
            temp,
            condition,
            seasonSuggestion,
            FeelsLike: response.Main.FeelsLike,
            RainChance: rainChance,
            PrecipitationMm: response.Rain?.OneHour,
            Humidity: response.Main.Humidity,
            WindSpeedMs: response.Wind?.Speed,
            ConditionDetail: conditionDetail);

        // cache for 30 minutes
        cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

        return result;
    }

    // best-effort chance-of-rain (%). The current-weather endpoint has no probability, so we read the
    private async Task<int> GetRainChanceAsync(string city, string condition, CancellationToken ct)
    {
        try
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&cnt=1&appid={_apiKey}&units=metric";
            var forecast = await httpClient.GetFromJsonAsync<OpenWeatherPopResponse>(url, ct);
            var pop = forecast?.List?.FirstOrDefault()?.Pop;
            if (pop.HasValue) return (int)Math.Round(Math.Clamp(pop.Value, 0f, 1f) * 100);
        }
        catch
        {
            // fall through to the condition-based estimate
        }

        if (condition.Contains("Thunderstorm", StringComparison.OrdinalIgnoreCase) ||
            condition.Contains("Rain", StringComparison.OrdinalIgnoreCase) ||
            condition.Contains("Drizzle", StringComparison.OrdinalIgnoreCase) ||
            condition.Contains("Snow", StringComparison.OrdinalIgnoreCase))
            return 80;
        if (condition.Contains("Cloud", StringComparison.OrdinalIgnoreCase))
            return 20;
        return 0;
    }

    public async Task<List<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return new List<CitySuggestion>();
        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey)) return new List<CitySuggestion>();

        var cacheKey = $"weather_cities_{query.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out List<CitySuggestion>? cachedCities))
        {
            return cachedCities!;
        }

        try
        {
            var url = $"http://api.openweathermap.org/geo/1.0/direct?q={query}&limit=5&appid={_apiKey}";
            var response = await httpClient.GetFromJsonAsync<List<OpenWeatherGeoResponse>>(url, ct);
            
            List<CitySuggestion> result;
            if (response != null)
            {
                result = response.Select(x => new CitySuggestion(x.Name, x.Country, x.State)).ToList();
            }
            else
            {
                result = new List<CitySuggestion>();
            }
            
            // cache for 24 hours since city names don't change
            cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            
            return result;
        }
        catch
        {
            return new List<CitySuggestion>();
        }
    }

    private string MapTempToSeason(float temp)
    {
        return temp switch
        {
            > 25 => "Summer",
            < 10 => "Winter",
            _ => "Spring" // Fallback for transition seasons
        };
    }

    public async Task<List<DailyForecast>> GetForecastAsync(string city, int days, DateTime? startDate = null, CancellationToken ct = default)
    {
        // clamp the user-supplied day count: the daily API serves at most 16 days,
        if (days < 1)
        {
            days = 1;
        }
        if (days > 16)
        {
            days = 16;
        }

        DateTime start;
        if (startDate.HasValue)
        {
            start = startDate.Value.Date;
        }
        else
        {
            start = DateTime.UtcNow.Date;
        }

        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Weather API key is not configured.");
        }

        var cacheKey = $"weather_forecast_{city.ToLowerInvariant()}_{days}_{start:yyyyMMdd}";
        if (cache.TryGetValue(cacheKey, out List<DailyForecast>? cachedForecast))
        {
            return cachedForecast!;
        }

        // use daily 16-day forecast API
        var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast/daily?q={city}&cnt={days}&appid={_apiKey}&units=metric";
        var response = await httpClient.GetFromJsonAsync<OpenWeatherDailyForecastResponse>(forecastUrl, ct);
        
        if (response?.List == null || response.List.Count == 0)
        {
            throw new Exception("Failed to fetch forecast data.");
        }

        var result = new List<DailyForecast>();
        for (int i = 0; i < days; i++)
        {
            var targetDate = start.AddDays(i);
            
            // find forecast by matching the date (dt is a Unix timestamp in UTC)
            var forecast = response.List.FirstOrDefault(x => DateTimeOffset.FromUnixTimeSeconds(x.Dt).Date == targetDate);
            
            if (forecast != null)
            {
                var forecastWeather = forecast.Weather.FirstOrDefault();
                string condition;
                if (forecastWeather != null)
                {
                    condition = forecastWeather.Main;
                }
                else
                {
                    condition = "Unknown";
                }

                result.Add(new DailyForecast(
                    targetDate,
                    forecast.Temp.Day,
                    condition,
                    MapTempToSeason(forecast.Temp.Day)
                ));
            }
            else
            {
                // if no forecast available for this date, use the last available
                var lastForecast = response.List.LastOrDefault();

                float fallbackTemp;
                if (lastForecast != null)
                {
                    fallbackTemp = lastForecast.Temp.Day;
                }
                else
                {
                    fallbackTemp = 22;
                }

                string fallbackCondition = "Clear";
                if (lastForecast != null)
                {
                    var lastWeather = lastForecast.Weather.FirstOrDefault();
                    if (lastWeather != null)
                    {
                        fallbackCondition = lastWeather.Main;
                    }
                }

                result.Add(new DailyForecast(targetDate, fallbackTemp, fallbackCondition, MapTempToSeason(fallbackTemp)));
            }
        }

        // cache for 3 hours
        cache.Set(cacheKey, result, TimeSpan.FromHours(3));

        return result;
    }

    private class OpenWeatherGeoResponse
    {
        public string Name { get; set; } = null!;
        public string Country { get; set; } = null!;
        public string? State { get; set; }
    }

    private class OpenWeatherResponse
    {
        public MainData Main { get; set; } = null!;
        public List<WeatherInfo> Weather { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("wind")]
        public WindData? Wind { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("rain")]
        public RainData? Rain { get; set; }
    }

    private class MainData
    {
        [System.Text.Json.Serialization.JsonPropertyName("temp")]
        public float Temp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("feels_like")]
        public float? FeelsLike { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("humidity")]
        public int? Humidity { get; set; }
    }
    private class WeatherInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("main")]
        public string Main { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private class WindData
    {
        [System.Text.Json.Serialization.JsonPropertyName("speed")]
        public float? Speed { get; set; }
    }

    private class RainData
    {
        [System.Text.Json.Serialization.JsonPropertyName("1h")]
        public float? OneHour { get; set; }
    }

    private class OpenWeatherPopResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("list")]
        public List<PopItem>? List { get; set; }
    }

    private class PopItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("pop")]
        public float? Pop { get; set; }
    }

    private class OpenWeatherDailyForecastResponse
    {
        public List<DailyForecastItem> List { get; set; } = new();
    }

    private class DailyForecastItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("dt")]
        public long Dt { get; set; }
        public TempData Temp { get; set; } = null!;
        public List<WeatherInfo> Weather { get; set; } = new();
    }

    private class TempData
    {
        [System.Text.Json.Serialization.JsonPropertyName("day")]
        public float Day { get; set; }
    }
}
