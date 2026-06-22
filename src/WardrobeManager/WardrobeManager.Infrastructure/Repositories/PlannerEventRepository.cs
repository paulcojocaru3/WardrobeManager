using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class PlannerEventRepository : IPlannerEventRepository
{
    private readonly ApplicationDbContext _context;

    public PlannerEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PlannerEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PlannerEvents
            .Include(p => p.Itineraries)
                .ThenInclude(i => i.Outfit)
                    .ThenInclude(o => o!.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PlannerEvent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // stays tracked: GetPlannerEventsQueryHandler mutates these (auto-archive) and saves.
        return await _context.PlannerEvents
            .Include(p => p.Itineraries)
                .ThenInclude(i => i.Outfit)
                    .ThenInclude(o => o!.Items)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlannerEvent>> GetActiveWithUpcomingItinerariesAsync(DateTime fromDateUtc, CancellationToken cancellationToken = default)
    {
        var fromDate = fromDateUtc.Date;
        return await _context.PlannerEvents
            .AsNoTracking()
            .Include(p => p.Itineraries)
            .Where(p => p.Status == "Active"
                && p.EndDate >= fromDate
                && p.Itineraries.Any(i => i.Date >= fromDate && i.StoredTemperature.HasValue))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PlannerEvent plannerEvent, CancellationToken cancellationToken = default)
    {
        await _context.PlannerEvents.AddAsync(plannerEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlannerEvent plannerEvent, CancellationToken cancellationToken = default)
    {
        _context.PlannerEvents.Update(plannerEvent);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PlannerEvent plannerEvent, CancellationToken cancellationToken = default)
    {
        await _context.PlannerEvents.Where(p => p.Id == plannerEvent.Id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddItineraryAsync(EventItinerary itinerary, CancellationToken cancellationToken = default)
    {
        await _context.EventItineraries.AddAsync(itinerary, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateItineraryAsync(EventItinerary itinerary, CancellationToken cancellationToken = default)
    {
        _context.EventItineraries.Update(itinerary);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteItineraryAsync(EventItinerary itinerary, CancellationToken cancellationToken = default)
    {
        _context.EventItineraries.Remove(itinerary);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EventItinerary?> GetItineraryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EventItineraries
            .Include(i => i.Outfit)
                .ThenInclude(o => o!.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }
}
