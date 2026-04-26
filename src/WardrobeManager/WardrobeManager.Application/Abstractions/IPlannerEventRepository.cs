using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IPlannerEventRepository
{
    Task<PlannerEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PlannerEvent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(PlannerEvent plannerEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlannerEvent plannerEvent, CancellationToken cancellationToken = default);
    Task DeleteAsync(PlannerEvent plannerEvent, CancellationToken cancellationToken = default);
    
    Task AddItineraryAsync(EventItinerary itinerary, CancellationToken cancellationToken = default);
    Task UpdateItineraryAsync(EventItinerary itinerary, CancellationToken cancellationToken = default);
    Task DeleteItineraryAsync(EventItinerary itinerary, CancellationToken cancellationToken = default);
    Task<EventItinerary?> GetItineraryByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
