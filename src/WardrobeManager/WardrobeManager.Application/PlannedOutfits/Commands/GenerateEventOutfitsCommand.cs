using MediatR;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record GenerateEventOutfitsCommand(Guid UserId, Guid PlannerEventId) : IRequest<GenerateEventOutfitsResult>;

public record GenerateEventOutfitsResult(
    int DaysProcessed,
    int OutfitsCreated,
    List<GeneratedDayDto> GeneratedDays,
    WeatherAlertDto? WeatherAlert
);

public record GeneratedDayDto(DateTime Date, string Style, string WeatherDescription, Guid OutfitId, string OutfitName);

public record WeatherAlertDto(
    bool IsAvailable,
    bool IsSignificantChange,
    float TemperatureDelta,
    WeatherDataDto? StoredForecast,
    WeatherDataDto? CurrentWeather,
    string? EventName = null,
    DateTime? EventDate = null,
    Guid? PlannerEventId = null
);

public record WeatherDataDto(float Temperature, string Condition, string SeasonSuggestion, DateTime? Date);
