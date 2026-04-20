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
        context.ClothingItems.Remove(item);
        await context.SaveChangesAsync(ct);
    }

    public async Task<ClothingItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.ClothingItems
            .Include(i => i.Outfits)
            .Include(i => i.WearEvents)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
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

        var projection = query.Select(i => new 
            { 
                Item = i, 
                // Convertim property-ul float[] la Vector pentru a putea folosi CosineDistance
                Distance = EF.Property<Pgvector.Vector>(i, "Embedding").CosineDistance(pgVector)
            });

        if (threshold.HasValue)
        {
            // similarity = 1 - distance
            projection = projection.Where(x => (1 - x.Distance) >= threshold.Value);
        }

        var results = await projection
            .OrderBy(x => x.Distance)
            .Take(limit)
            .ToListAsync(ct);

        return results.Select(x => (x.Item, 1 - x.Distance)).ToList();
    }

    public IQueryable<ClothingItem> Query()
    {
        return context.ClothingItems;
    }
}
