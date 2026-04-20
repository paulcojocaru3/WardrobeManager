using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public class WearEventRepository : IWearEventRepository
{
    private readonly ApplicationDbContext _context;

    public WearEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WearEvent wearEvent)
    {
        await _context.WearEvents.AddAsync(wearEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WearEvent>> GetByUserIdAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.WearEvents
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.WearDate >= startDate && w.WearDate <= endDate)
            .Include(w => w.ClothingItem)
            .OrderByDescending(w => w.WearDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<WearEvent>> GetAllByUserIdAsync(Guid userId)
    {
        return await _context.WearEvents
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.ClothingItem)
            .ToListAsync();
    }
}