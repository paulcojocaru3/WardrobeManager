using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IClothingRepository
{
    Task AddAsync(ClothingItem item, CancellationToken ct = default);
    Task DeleteAsync(ClothingItem item, CancellationToken ct = default);
    Task<ClothingItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ClothingItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    IQueryable<ClothingItem> Query();
}
