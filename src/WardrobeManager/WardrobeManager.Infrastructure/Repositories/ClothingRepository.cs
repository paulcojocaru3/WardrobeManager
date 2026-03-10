using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public class ClothingRepository(ApplicationDbContext context) : IClothingRepository
{
    public async Task AddAsync(ClothingItem item, CancellationToken ct = default)
    {
        await context.ClothingItems.AddAsync(item, ct);
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

    public IQueryable<ClothingItem> Query()
    {
        return context.ClothingItems;
    }
}