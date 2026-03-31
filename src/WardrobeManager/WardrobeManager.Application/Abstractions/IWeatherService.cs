namespace WardrobeManager.Application.Abstractions;

public record WeatherData(float Temperature, string Condition, string SeasonSuggestion);
public record CitySuggestion(string Name, string Country, string? State);

public interface IWeatherService
{
    Task<WeatherData> GetCurrentWeatherAsync(string city, CancellationToken ct = default);
    Task<List<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default);
}
