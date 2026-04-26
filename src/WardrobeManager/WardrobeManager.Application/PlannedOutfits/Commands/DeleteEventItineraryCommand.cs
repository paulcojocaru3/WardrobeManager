using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.PlannedOutfits.Commands;

public record DeleteEventItineraryCommand(Guid UserId, Guid PlannerEventId, Guid ItineraryId) : IRequest<bool>;

public class DeleteEventItineraryCommandHandler : IRequestHandler<DeleteEventItineraryCommand, bool>
{
    private readonly IPlannerEventRepository _plannerEventRepository;

    public DeleteEventItineraryCommandHandler(IPlannerEventRepository plannerEventRepository)
    {
        _plannerEventRepository = plannerEventRepository;
    }

    public async Task<bool> Handle(DeleteEventItineraryCommand request, CancellationToken cancellationToken)
    {
        var plannerEvent = await _plannerEventRepository.GetByIdAsync(request.PlannerEventId, cancellationToken);
        if (plannerEvent == null || plannerEvent.UserId != request.UserId)
        {
            return false;
        }

        var itinerary = await _plannerEventRepository.GetItineraryByIdAsync(request.ItineraryId, cancellationToken);
        if (itinerary == null || itinerary.PlannerEventId != request.PlannerEventId)
        {
            return false;
        }

        await _plannerEventRepository.DeleteItineraryAsync(itinerary, cancellationToken);
        return true;
    }
}