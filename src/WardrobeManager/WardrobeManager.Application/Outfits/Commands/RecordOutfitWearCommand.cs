using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public record RecordOutfitWearCommand(Guid UserId, Guid OutfitId) : IRequest<bool>;

public sealed class RecordOutfitWearCommandHandler : IRequestHandler<RecordOutfitWearCommand, bool>
{
    private readonly IOutfitRepository _outfitRepository;
    private readonly IWearEventRepository _wearEventRepository;

    public RecordOutfitWearCommandHandler(IOutfitRepository outfitRepository, IWearEventRepository wearEventRepository)
    {
        _outfitRepository = outfitRepository;
        _wearEventRepository = wearEventRepository;
    }

    public async Task<bool> Handle(RecordOutfitWearCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow; 
        
        // Count distinct OUTFIT wear events recorded today
        var eventsToday = await _wearEventRepository.GetByUserIdAsync(request.UserId, today, today.AddDays(1).AddTicks(-1), cancellationToken);
        
        // Group by OutfitId AND a rounded timestamp (to 1 minute) to identify a "session"
        var distinctSessionsToday = eventsToday
            .Where(e => e.OutfitId.HasValue)
            .GroupBy(e => new { e.OutfitId, Time = e.WearDate.ToString("yyyy-MM-dd HH:mm") })
            .Count();

        if (distinctSessionsToday >= 10)
        {
            return false; // Limit reached
        }

        var outfit = await _outfitRepository.GetByIdAsync(request.OutfitId, cancellationToken);
        if (outfit == null || outfit.UserId != request.UserId)
        {
            return false;
        }

        var wearEvents = outfit.Items.Select(item => new WearEvent
        {
            UserId = request.UserId,
            ClothingItemId = item.Id,
            OutfitId = outfit.Id,
            WearDate = now // Use the SAME timestamp
        });
        await _wearEventRepository.AddRangeAsync(wearEvents, cancellationToken);

        return true;
    }
}