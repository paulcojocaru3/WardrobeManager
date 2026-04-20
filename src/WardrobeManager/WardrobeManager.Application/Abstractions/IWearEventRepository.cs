using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IWearEventRepository
{
    Task AddAsync(WearEvent wearEvent);
    Task<IEnumerable<WearEvent>> GetByUserIdAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<WearEvent>> GetAllByUserIdAsync(Guid userId);
}