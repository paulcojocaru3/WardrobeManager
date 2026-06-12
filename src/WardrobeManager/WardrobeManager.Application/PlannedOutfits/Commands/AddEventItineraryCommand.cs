using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record AddEventItineraryCommand(Guid UserId, Guid PlannerEventId, Guid OutfitId, DateTime Date, string Moment) : IRequest<Guid>;

public sealed class AddEventItineraryCommandHandler : IRequestHandler<AddEventItineraryCommand, Guid>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<AddEventItineraryCommandHandler> _logger;

    public AddEventItineraryCommandHandler(IPlannerEventRepository plannerEventRepository, IOutfitRepository outfitRepository, IWeatherService weatherService, ILogger<AddEventItineraryCommandHandler> logger)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<Guid> Handle(AddEventItineraryCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            throw new KeyNotFoundException("Planner event not found or does not belong to user.");
        }

        var outfit = await _outfitRepository.GetByIdAsync(request.OutfitId, cancellationToken);
        if (outfit == null || outfit.UserId != request.UserId)
        {
            throw new KeyNotFoundException("Outfit not found or does not belong to user.");
        }

        var storedTemp = await EventForecastHelper.TryGetTemperatureAsync(_weatherService, _logger, plannerEvent, request.Date, cancellationToken);

        var itinerary = new EventItinerary
        {
            PlannerEventId = request.PlannerEventId,
            OutfitId = request.OutfitId,
            Date = request.Date.Date,
            Moment = request.Moment,
            StoredTemperature = storedTemp
        };

        await _plannerEventRepository.AddItineraryAsync(itinerary, cancellationToken);

        return itinerary.Id;
    }
}