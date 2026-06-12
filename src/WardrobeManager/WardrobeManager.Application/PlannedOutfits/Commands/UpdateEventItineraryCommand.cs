using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record UpdateEventItineraryCommand(Guid UserId, Guid PlannerEventId, Guid ItineraryId, Guid OutfitId, DateTime Date, string Moment) : IRequest<bool>;

public sealed class UpdateEventItineraryCommandHandler : IRequestHandler<UpdateEventItineraryCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<UpdateEventItineraryCommandHandler> _logger;

    public UpdateEventItineraryCommandHandler(IPlannerEventRepository plannerEventRepository, IOutfitRepository outfitRepository, IWeatherService weatherService, ILogger<UpdateEventItineraryCommandHandler> logger)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
        _weatherService = weatherService;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateEventItineraryCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        var outfit = await _outfitRepository.GetByIdAsync(request.OutfitId, cancellationToken);
        if (outfit == null || outfit.UserId != request.UserId)
        {
            return false;
        }

        var itinerary = plannerEvent.Itineraries.FirstOrDefault(i => i.Id == request.ItineraryId);
        if (itinerary == null)
        {
            return false;
        }

        float? storedTemp = itinerary.StoredTemperature;
        if (itinerary.Date.Date != request.Date.Date)
        {
            var resolved = await EventForecastHelper.TryGetTemperatureAsync(_weatherService, _logger, plannerEvent, request.Date, cancellationToken);
            if (resolved.HasValue)
            {
                storedTemp = resolved.Value;
            }
        }

        itinerary.OutfitId = request.OutfitId;
        itinerary.Date = request.Date.Date;
        itinerary.Moment = request.Moment;
        itinerary.StoredTemperature = storedTemp;

        await _plannerEventRepository.UpdateItineraryAsync(itinerary, cancellationToken);
        return true;
    }
}
