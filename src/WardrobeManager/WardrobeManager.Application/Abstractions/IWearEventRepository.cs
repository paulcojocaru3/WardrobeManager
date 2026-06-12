using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IWearEventRepository
{
    Task AddAsync(WearEvent wearEvent, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<WearEvent> wearEvents, CancellationToken ct = default);
    Task<IEnumerable<WearEvent>> GetByUserIdAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<IEnumerable<WearEvent>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
}
