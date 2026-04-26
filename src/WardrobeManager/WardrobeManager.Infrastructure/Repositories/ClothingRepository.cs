using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public class ClothingRepository(ApplicationDbContext context) : IClothingRepository
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
        return await context.ClothingItems
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

    public async Task<List<ClothingItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.ClothingItems
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<(ClothingItem Item, double Similarity)>> GetSimilarItemsAsync(Guid userId, float[] vector, ClothingType? type = null, int limit = 10, double? threshold = null, CancellationToken ct = default)
    {
        var pgVector = new Pgvector.Vector(vector);

        var query = context.ClothingItems
            .Where(i => i.UserId == userId && i.Embedding != null);

        if (type.HasValue)
        {
            query = query.Where(i => i.Type == type.Value);
        }

        // Order directly by distance to ensure HNSW index usage
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

    public IQueryable<ClothingItem> Query()
    {
        return context.ClothingItems;
    }
}
