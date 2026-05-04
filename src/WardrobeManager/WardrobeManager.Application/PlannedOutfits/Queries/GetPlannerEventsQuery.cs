using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.PlannedOutfits.Queries;

public record GetPlannerEventsQuery(Guid UserId) : IRequest<IEnumerable<PlannerEventDto>>;

public class GetPlannerEventsQueryHandler : IRequestHandler<GetPlannerEventsQuery, IEnumerable<PlannerEventDto>>
{
    private readonly IPlannerEventRepository _plannerEventRepository;

    public GetPlannerEventsQueryHandler(IPlannerEventRepository plannerEventRepository)
    {
        _plannerEventRepository = plannerEventRepository;
    }

    public async Task<IEnumerable<PlannerEventDto>> Handle(GetPlannerEventsQuery request, CancellationToken cancellationToken)
    {
        var plannerEvents = (await _plannerEventRepository.GetByUserIdAsync(request.UserId, cancellationToken)).ToList();

        var now = DateTime.UtcNow;
        bool hasChanges = false;

        foreach (var plannerEvent in plannerEvents.ToList())
        {
            // Auto-archive if EndDate is in the past and it's still Active
            if (plannerEvent.Status == "Active" && plannerEvent.EndDate < now.Date)
            {
                plannerEvent.Status = "Archived";
                plannerEvent.ArchivedAt = now;
                await _plannerEventRepository.UpdateAsync(plannerEvent, cancellationToken);
                hasChanges = true;
            }
            // Auto-delete if Archived more than 30 days ago
            else if (plannerEvent.Status == "Archived" && plannerEvent.ArchivedAt.HasValue && (now - plannerEvent.ArchivedAt.Value).TotalDays > 30)
            {
                await _plannerEventRepository.DeleteAsync(plannerEvent, cancellationToken);
                plannerEvents.Remove(plannerEvent);
                hasChanges = true;
            }
        }

        // Filter to return only active events
        var activeEvents = plannerEvents.Where(p => p.Status == "Active");

        return activeEvents.Select(p => new PlannerEventDto
        {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type,
            Location = p.Location,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            ArchivedAt = p.ArchivedAt,
            PreferredStyles = p.PreferredStyles ?? new List<string>(),
            Itineraries = p.Itineraries.Select(i => new EventItineraryDto
            {
                Id = i.Id,
                OutfitId = i.OutfitId,
                Date = i.Date,
                Moment = i.Moment,
                Outfit = new OutfitDto(
                    i.Outfit!.Id,
                    i.Outfit.Name,
                    i.Outfit.IsAiGenerated,
                    i.Outfit.CreatedAt,
                    i.Outfit.Items.Select(item => new ClothingItemDto(
                        item.Id,
                        item.Name,
                        item.Type,
                        item.Color,
                        item.Gender,
                        item.Season,
                        item.Usage,
                        item.ProcessedImageUrl,
                        item.CreatedAt
                    )).ToList()
                )
            }).ToList()
        });
    }
}