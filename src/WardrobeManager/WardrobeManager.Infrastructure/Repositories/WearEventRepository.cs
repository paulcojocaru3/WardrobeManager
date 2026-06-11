using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class WearEventRepository : IWearEventRepository
{
    private readonly ApplicationDbContext _context;

    public WearEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WearEvent wearEvent, CancellationToken ct = default)
    {
        await _context.WearEvents.AddAsync(wearEvent, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<WearEvent> wearEvents, CancellationToken ct = default)
    {
        await _context.WearEvents.AddRangeAsync(wearEvents, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<WearEvent>> GetByUserIdAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _context.WearEvents
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.WearDate >= startDate && w.WearDate <= endDate)
            .Include(w => w.ClothingItem)
            .OrderByDescending(w => w.WearDate)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<WearEvent>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.WearEvents
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.ClothingItem)
            .ToListAsync(ct);
    }
}
