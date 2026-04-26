using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record UpdateEventItineraryCommand(Guid UserId, Guid PlannerEventId, Guid ItineraryId, Guid OutfitId, DateTime Date, string Moment) : IRequest<bool>;

public class UpdateEventItineraryCommandHandler : IRequestHandler<UpdateEventItineraryCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;

    public UpdateEventItineraryCommandHandler(IPlannerEventRepository plannerEventRepository, IOutfitRepository outfitRepository)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
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

        itinerary.OutfitId = request.OutfitId;
        itinerary.Date = request.Date.Date;
        itinerary.Moment = request.Moment;

        await _plannerEventRepository.UpdateItineraryAsync(itinerary, cancellationToken);
        return true;
    }
}
