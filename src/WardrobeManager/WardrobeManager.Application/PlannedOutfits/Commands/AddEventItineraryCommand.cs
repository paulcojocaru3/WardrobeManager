using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record AddEventItineraryCommand(Guid UserId, Guid PlannerEventId, Guid OutfitId, DateTime Date, string Moment) : IRequest<Guid>;

public class AddEventItineraryCommandHandler : IRequestHandler<AddEventItineraryCommand, Guid>
{
    private readonly IPlannerEventRepository _plannerEventRepository;
    private readonly IOutfitRepository _outfitRepository;

    public AddEventItineraryCommandHandler(IPlannerEventRepository plannerEventRepository, IOutfitRepository outfitRepository)
    {
        _plannerEventRepository = plannerEventRepository;
        _outfitRepository = outfitRepository;
    }

    public async Task<Guid> Handle(AddEventItineraryCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            throw new Exception("Planner event not found or does not belong to user.");
        }

        var outfit = await _outfitRepository.GetByIdAsync(request.OutfitId, cancellationToken);
        if (outfit == null || outfit.UserId != request.UserId)
        {
            throw new Exception("Outfit not found or does not belong to user.");
        }

        var itinerary = new EventItinerary
        {
            PlannerEventId = request.PlannerEventId,
            OutfitId = request.OutfitId,
            Date = request.Date.Date,
            Moment = request.Moment
        };

        await _plannerEventRepository.AddItineraryAsync(itinerary, cancellationToken);

        return itinerary.Id;
    }
}