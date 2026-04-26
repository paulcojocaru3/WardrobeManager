namespace WardrobeManager.Application.Abstractions;

public record WeatherData(float Temperature, string Condition, string SeasonSuggestion);
public record CitySuggestion(string Name, string Country, string? State);
public record DailyForecast(DateTime Date, float Temperature, string Condition, string SeasonSuggestion);

public interface IWeatherService
{
    Task<WeatherData> GetCurrentWeatherAsync(string city, CancellationToken ct = default);
    Task<List<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default);
    Task<List<DailyForecast>> GetForecastAsync(string city, int days, DateTime? startDate = null, CancellationToken ct = default);
}
