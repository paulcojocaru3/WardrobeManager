using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class OutfitRepository(ApplicationDbContext context) : IOutfitRepository
{
    public async Task<Outfit?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Outfits
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Outfit?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct)
    {
        return await context.Outfits
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, ct);
    }

    public async Task<List<Outfit>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        // read-only listing — no change tracking needed.
        return await context.Outfits
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Outfit outfit, CancellationToken ct)
    {
        // atașăm hainele existente pentru a nu încerca să le creeze din nou
        var attachedItems = new List<ClothingItem>();
        foreach (var item in outfit.Items)
        {
            var trackedItem = context.ClothingItems.Local.FirstOrDefault(i => i.Id == item.Id);
            if (trackedItem != null)
            {
                attachedItems.Add(trackedItem);
            }
            else
            {
                context.ClothingItems.Attach(item);
                attachedItems.Add(item);
            }
        }

        outfit.Items = attachedItems;
        await context.Outfits.AddAsync(outfit, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Outfit outfit, CancellationToken ct)
    {
        context.Outfits.Update(outfit);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Outfit outfit, CancellationToken ct)
    {
        await context.Outfits.Where(o => o.Id == outfit.Id).ExecuteDeleteAsync(ct);
    }
}
