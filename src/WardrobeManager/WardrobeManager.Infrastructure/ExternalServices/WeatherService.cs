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

    private class MainData { public float Temp { get; set; } }
    private class WeatherInfo { public string Main { get; set; } = null!; }
}
