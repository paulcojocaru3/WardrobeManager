using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class ClothingRepository(ApplicationDbContext context) : IClothingRepository
{
    public async Task AddAsync(ClothingItem item, CancellationToken ct = default)
    {
        await context.ClothingItems.AddAsync(item, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ClothingItem item, CancellationToken ct = default)
    {
        context.ClothingItems.Update(item);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ClothingItem item, CancellationToken ct = default)
    {
        await context.ClothingItems.Where(i => i.Id == item.Id).ExecuteDeleteAsync(ct);
    }

    public async Task<ClothingItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // assplitquery: two collection Includes would otherwise produce an Outfits×WearEvents Cartesian product.
        return await context.ClothingItems
            .AsSplitQuery()
            .Include(i => i.Outfits)
            .Include(i => i.WearEvents)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<List<ClothingItem>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        return await context.ClothingItems
            .Where(i => ids.Contains(i.Id))
            .ToListAsync(ct);
    }

    public async Task<ClothingItem?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        return await context.ClothingItems
            .AsSplitQuery()
            .Include(i => i.Outfits)
            .Include(i => i.WearEvents)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);
    }

    public async Task<List<ClothingItem>> GetByIdsForUserAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        return await context.ClothingItems
            .Where(i => i.UserId == userId && idList.Contains(i.Id))
            .ToListAsync(ct);
    }

    public async Task<List<ClothingItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        // read-only listing — no change tracking needed.
        return await context.ClothingItems
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<(ClothingItem Item, double Similarity)>> GetSimilarItemsAsync(Guid userId, float[] vector, ClothingType? type = null, int limit = 10, double? threshold = null, string? gender = null, CancellationToken ct = default)
    {
        var pgVector = new Pgvector.Vector(vector);

        // read-only candidate scoring — no change tracking on this hot path.
        var query = context.ClothingItems
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Embedding != null);

        if (type.HasValue)
        {
            query = query.Where(i => i.Type == type.Value);
        }

        // gender hard filter: only same-gender, Unisex, or unspecified items.
        if (!string.IsNullOrEmpty(gender))
        {
            query = query.Where(i => i.Gender == null || i.Gender == gender || i.Gender == "Unisex");
        }

        // order directly by distance to ensure HNSW index usage
        var results = await query
            .OrderBy(i => EF.Property<Pgvector.Vector>(i, "Embedding").CosineDistance(pgVector))
            .Take(limit)
            .Select(i => new 
            { 
                Item = i, 
                Distance = EF.Property<Pgvector.Vector>(i, "Embedding").CosineDistance(pgVector)
            })
            .ToListAsync(ct);

        var finalResults = results.Select(x => (x.Item, Similarity: 1 - x.Distance));

        if (threshold.HasValue)
        {
            finalResults = finalResults.Where(x => x.Similarity >= threshold.Value);
        }

        return finalResults.ToList();
    }

    public async Task<Dictionary<Guid, DateTime>> GetWearRecencyAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.WearEvents
            .Where(w => w.UserId == userId)
            .GroupBy(w => w.ClothingItemId)
            .Select(g => new { ItemId = g.Key, LastWorn = g.Max(w => w.WearDate) })
            .ToDictionaryAsync(x => x.ItemId, x => x.LastWorn, ct);
    }

    public async Task<Dictionary<Guid, int>> GetWearCountsAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.WearEvents
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .GroupBy(w => w.ClothingItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ItemId, x => x.Count, ct);
    }

    public async Task<List<ClothingItem>> GetLeastWornCandidatesAsync(Guid userId, string? style, int limit, CancellationToken ct = default)
    {
        var query = context.ClothingItems
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Embedding != null);

        if (!string.IsNullOrWhiteSpace(style))
        {
            query = query.Where(i => i.Usage != null && EF.Functions.ILike(i.Usage, $"%{style}%"));
        }

        // correlated wear-count/last-worn per item -> never-worn (count 0) sort first, then oldest.
        return await query
            .Select(i => new
            {
                Item = i,
                Count = context.WearEvents.Count(w => w.ClothingItemId == i.Id),
                Last = context.WearEvents.Where(w => w.ClothingItemId == i.Id).Max(w => (DateTime?)w.WearDate)
            })
            .OrderBy(x => x.Count)
            .ThenBy(x => x.Last)
            .Take(limit)
            .Select(x => x.Item)
            .ToListAsync(ct);
    }

    public async Task<List<ClothingItem>> GetMissingSubTypeWithEmbeddingAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.ClothingItems
            .Where(c => c.UserId == userId && c.SubType == null && c.Embedding != null)
            .ToListAsync(ct);
    }

    public async Task UpdateRangeAsync(IReadOnlyCollection<ClothingItem> items, CancellationToken ct = default)
    {
        context.ClothingItems.UpdateRange(items);
        await context.SaveChangesAsync(ct);
    }
}
