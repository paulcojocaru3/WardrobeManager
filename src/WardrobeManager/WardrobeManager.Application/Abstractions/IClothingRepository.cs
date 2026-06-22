using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Abstractions;

public interface IClothingRepository
{
    Task AddAsync(ClothingItem item, CancellationToken ct = default);
    Task UpdateAsync(ClothingItem item, CancellationToken ct = default);
    Task DeleteAsync(ClothingItem item, CancellationToken ct = default);
    Task<ClothingItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ClothingItem?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<List<ClothingItem>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<List<ClothingItem>> GetByIdsForUserAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
    Task<List<ClothingItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<List<(ClothingItem Item, double Similarity)>> GetSimilarItemsAsync(Guid userId, float[] vector, ClothingType? type = null, int limit = 10, double? threshold = null, string? gender = null, CancellationToken ct = default);

    // last-worn date per item (worn items only), feeds variety scoring
    Task<Dictionary<Guid, DateTime>> GetWearRecencyAsync(Guid userId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> GetWearCountsAsync(Guid userId, CancellationToken ct = default);

    // items with an embedding ordered least-used first (never-worn, then fewest wears, then oldest),
    Task<List<ClothingItem>> GetLeastWornCandidatesAsync(Guid userId, string? style, int limit, CancellationToken ct = default);
    Task<List<ClothingItem>> GetMissingSubTypeWithEmbeddingAsync(Guid userId, CancellationToken ct = default);
    Task UpdateRangeAsync(IReadOnlyCollection<ClothingItem> items, CancellationToken ct = default);
}
