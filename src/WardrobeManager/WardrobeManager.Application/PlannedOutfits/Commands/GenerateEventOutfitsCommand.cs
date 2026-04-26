using MediatR;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record GenerateEventOutfitsCommand(Guid UserId, Guid PlannerEventId) : IRequest<GenerateEventOutfitsResult>;

public record GenerateEventOutfitsResult(
    int DaysProcessed,
    int OutfitsCreated,
    List<GeneratedDayDto> GeneratedDays
);

public record GeneratedDayDto(DateTime Date, string Style, string WeatherDescription, Guid OutfitId, string OutfitName);