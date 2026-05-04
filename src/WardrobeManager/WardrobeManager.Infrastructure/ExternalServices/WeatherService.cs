using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

public class WeatherService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache) : IWeatherService
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
        var condition = response.Weather.FirstOrDefault()?.Main ?? "Unknown";
        
        var seasonSuggestion = MapTempToSeason(temp);

        var result = new WeatherData(temp, condition, seasonSuggestion);
        
        // Cache for 30 minutes
        cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
        
        return result;
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
            
            var result = response?.Select(x => new CitySuggestion(x.Name, x.Country, x.State)).ToList() ?? new List<CitySuggestion>();
            
            // Cache for 24 hours since city names don't change
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
        var start = startDate?.Date ?? DateTime.UtcNow.Date;

        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey))
        {
            throw new InvalidOperationException("Weather API key is not configured.");
        }

        var cacheKey = $"weather_forecast_{city.ToLowerInvariant()}_{days}_{start:yyyyMMdd}";
        if (cache.TryGetValue(cacheKey, out List<DailyForecast>? cachedForecast))
        {
            return cachedForecast!;
        }

        // Use daily 16-day forecast API
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
            
            // Find forecast by matching the date (dt is a Unix timestamp in UTC)
            var forecast = response.List.FirstOrDefault(x => DateTimeOffset.FromUnixTimeSeconds(x.Dt).Date == targetDate);
            
            if (forecast != null)
            {
                result.Add(new DailyForecast(
                    targetDate,
                    forecast.Temp.Day,
                    forecast.Weather.FirstOrDefault()?.Main ?? "Unknown",
                    MapTempToSeason(forecast.Temp.Day)
                ));
            }
            else
            {
                // If no forecast available for this date, use the last available
                var fallbackTemp = response.List.LastOrDefault()?.Temp.Day ?? 22;
                var fallbackCondition = response.List.LastOrDefault()?.Weather.FirstOrDefault()?.Main ?? "Clear";
                result.Add(new DailyForecast(targetDate, fallbackTemp, fallbackCondition, MapTempToSeason(fallbackTemp)));
            }
        }

        // Cache for 3 hours
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
    }

    private class MainData 
    { 
        [System.Text.Json.Serialization.JsonPropertyName("temp")]
        public float Temp { get; set; } 
    }
    private class WeatherInfo 
    { 
        [System.Text.Json.Serialization.JsonPropertyName("main")]
        public string Main { get; set; } = null!; 
    }

    private class OpenWeatherForecastResponse
    {
        public List<ForecastItem> List { get; set; } = new();
    }

    private class ForecastItem
    {
        [System.Text.Json.Serialization.JsonPropertyName("dt_txt")]
        public DateTime DtTxt { get; set; }
        public MainData Main { get; set; } = null!;
        public List<WeatherInfo> Weather { get; set; } = new();
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
