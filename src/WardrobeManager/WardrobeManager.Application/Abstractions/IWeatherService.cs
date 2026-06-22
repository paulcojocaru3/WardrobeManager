namespace WardrobeManager.Application.Abstractions;

// temperature/FeelsLike are °C; RainChance/Humidity are 0-100 %; WindSpeedMs is m/s.
public record WeatherData(
    float Temperature,
    string Condition,
    string SeasonSuggestion,
    float? FeelsLike = null,
    int? RainChance = null,
    float? PrecipitationMm = null,
    int? Humidity = null,
    float? WindSpeedMs = null,
    // the granular description from the provider, e.g. "light rain" / "shower rain" / "broken clouds".
    string? ConditionDetail = null);
public record CitySuggestion(string Name, string Country, string? State);
public record DailyForecast(DateTime Date, float Temperature, string Condition, string SeasonSuggestion);

public interface IWeatherService
{
    Task<WeatherData> GetCurrentWeatherAsync(string city, CancellationToken ct = default);
    Task<List<CitySuggestion>> SearchCitiesAsync(string query, CancellationToken ct = default);
    Task<List<DailyForecast>> GetForecastAsync(string city, int days, DateTime? startDate = null, CancellationToken ct = default);
}
