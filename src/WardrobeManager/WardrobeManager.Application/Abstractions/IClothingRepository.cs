using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Abstractions;

public interface IClothingRepository
{
    Task AddAsync(ClothingItem item, CancellationToken ct = default);
    Task DeleteAsync(ClothingItem item, CancellationToken ct = default);
    Task<ClothingItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ClothingItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<(ClothingItem Item, double Similarity)>> GetSimilarItemsAsync(Guid userId, float[] vector, ClothingType? type = null, int limit = 10, double? threshold = null, CancellationToken ct = default);
    IQueryable<ClothingItem> Query();
}
