using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Infrastructure.ExternalServices;

public class WeatherService(HttpClient httpClient, IConfiguration configuration) : IWeatherService
{
    private readonly string _apiKey = configuration["WeatherApi:Key"] ?? "YOUR_API_KEY_HERE";
    private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

    public async Task<WeatherData> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
    {
        // For development, if no API key is provided, we return a fallback
        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey))
        {
            return new WeatherData(22, "Clear", "Summer");
        }

        try
        {
            var response = await httpClient.GetFromJsonAsync<OpenWeatherResponse>(
                $"{BaseUrl}?q={city}&appid={_apiKey}&units=metric", ct);

            if (response == null) return new WeatherData(20, "Unknown", "Spring");

            var temp = response.Main.Temp;
            var condition = response.Weather.FirstOrDefault()?.Main ?? "Unknown";
            
            var seasonSuggestion = MapTempToSeason(temp);

            return new WeatherData(temp, condition, seasonSuggestion);
        }
        catch
        {
            // Fail safe
            return new WeatherData(15, "Cloudy", "Autumn");
        }
    }

    public async Task<List<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) return new List<CitySuggestion>();
        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey)) return new List<CitySuggestion>();

        try
        {
            var url = $"http://api.openweathermap.org/geo/1.0/direct?q={query}&limit=5&appid={_apiKey}";
            var response = await httpClient.GetFromJsonAsync<List<OpenWeatherGeoResponse>>(url, ct);
            
            return response?.Select(x => new CitySuggestion(x.Name, x.Country, x.State)).ToList() ?? new List<CitySuggestion>();
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

        // For development, if no API key is provided, return mock forecast based on reasonable temperature
        if (_apiKey == "YOUR_API_KEY_HERE" || string.IsNullOrEmpty(_apiKey))
        {
            // Return mock forecast with 22°C (Summer) - realistic for weekend
            return Enumerable.Range(0, days)
                .Select(i => new DailyForecast(start.AddDays(i), 22, "Clear", "Summer"))
                .ToList();
        }

        try
        {
            // Use 5 day / 3 hour forecast API
            var forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={_apiKey}&units=metric";
            var response = await httpClient.GetFromJsonAsync<OpenWeatherForecastResponse>(forecastUrl, ct);
            
            if (response?.List == null || response.List.Count == 0)
            {
                return GetDefaultForecast(days, start);
            }

            // Group by day and take average
            var dailyForecasts = response.List
                .GroupBy(x => x.DtTxt.Date)
                .Select(g => new DailyForecast(
                    g.Key,
                    g.Average(x => x.Main.Temp),
                    g.First().Weather.FirstOrDefault()?.Main ?? "Unknown",
                    MapTempToSeason((float)g.Average(x => x.Main.Temp))
                ))
                .ToList();

            // Map to requested dates
            var result = new List<DailyForecast>();
            for (int i = 0; i < days; i++)
            {
                var targetDate = start.AddDays(i);
                var forecast = dailyForecasts.FirstOrDefault(f => f.Date == targetDate);
                
                if (forecast != null)
                {
                    result.Add(forecast);
                }
                else
                {
                    // If no forecast available for this date (e.g. > 5 days in future), use the last available or default
                    var fallbackTemp = dailyForecasts.LastOrDefault()?.Temperature ?? 22;
                    var fallbackCondition = dailyForecasts.LastOrDefault()?.Condition ?? "Clear";
                    result.Add(new DailyForecast(targetDate, fallbackTemp, fallbackCondition, MapTempToSeason(fallbackTemp)));
                }
            }

            return result;
        }
        catch
        {
            return GetDefaultForecast(days, start);
        }
    }

    private List<DailyForecast> GetDefaultForecast(int days, DateTime start)
    {
        // Default to reasonable summer temperature if API fails
        return Enumerable.Range(0, days)
            .Select(i => new DailyForecast(start.AddDays(i), 22, "Clear", "Summer"))
            .ToList();
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
}
