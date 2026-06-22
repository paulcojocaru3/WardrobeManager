using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitRepository
{
    Task<Outfit?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Outfit?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct);
    Task<List<Outfit>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Outfit outfit, CancellationToken ct);
    Task UpdateAsync(Outfit outfit, CancellationToken ct);
    Task DeleteAsync(Outfit outfit, CancellationToken ct);
}
