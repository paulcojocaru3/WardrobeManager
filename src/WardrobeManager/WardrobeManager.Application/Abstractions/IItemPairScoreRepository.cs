namespace WardrobeManager.Application.Abstractions;

// one accrual increment for an unordered item pair. Ids are canonicalized by the repository.
public readonly record struct ItemPairDelta(
    Guid ItemAId, Guid ItemBId, int PairedDelta, int PositiveDelta, int NegativeDelta);

public interface IItemPairScoreRepository
{
    // canonical unordered pair (min id, max id) -> compatibility in [-1,1]; only pairs observed
    Task<IReadOnlyDictionary<(Guid, Guid), double>> GetCompatibilityMapAsync(Guid userId, CancellationToken ct = default);

    Task UpsertBatchAsync(Guid userId, IReadOnlyCollection<ItemPairDelta> deltas, CancellationToken ct = default);
}
