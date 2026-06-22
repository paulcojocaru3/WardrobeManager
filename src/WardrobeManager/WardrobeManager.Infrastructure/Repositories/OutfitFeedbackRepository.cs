using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class OutfitFeedbackRepository : IOutfitFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public OutfitFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddImpressionsAsync(IEnumerable<OutfitFeedback> impressions, CancellationToken ct = default)
    {
        await _context.OutfitFeedbacks.AddRangeAsync(impressions, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RecordActionAsync(Guid userId, Guid generationId, Guid clothingItemId, FeedbackAction action, CancellationToken ct = default)
    {
        var row = await _context.OutfitFeedbacks.FirstOrDefaultAsync(
            f => f.UserId == userId && f.GenerationId == generationId && f.ClothingItemId == clothingItemId, ct);

        if (row == null) return;

        row.Action = action;
        row.ActionedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RecordActionsForItemsAsync(
        Guid userId, Guid generationId, IEnumerable<Guid> clothingItemIds, FeedbackAction action, CancellationToken ct = default)
    {
        var ids = clothingItemIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var rows = await _context.OutfitFeedbacks
            .Where(f => f.UserId == userId && f.GenerationId == generationId && ids.Contains(f.ClothingItemId))
            .ToListAsync(ct);

        if (rows.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.Action = action;
            row.ActionedAt = now;
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OutfitFeedback>> GetByGenerationAsync(Guid userId, Guid generationId, CancellationToken ct = default)
    {
        return await _context.OutfitFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.GenerationId == generationId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<Guid>> GetRejectedItemIdsSinceAsync(Guid userId, DateTime since, CancellationToken ct = default)
    {
        return await _context.OutfitFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Action == FeedbackAction.Rejected && f.ActionedAt >= since)
            .Select(f => f.ClothingItemId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<Guid>> GetRecentlyShownItemIdsAsync(
        Guid userId, DateTime since, ClothingType? slot = null, CancellationToken ct = default)
    {
        var query = _context.OutfitFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.CreatedAt >= since);

        if (slot.HasValue)
        {
            query = query.Where(f => f.SlotType == slot.Value);
        }

        return await query
            .Select(f => f.ClothingItemId)
            .Distinct()
            .ToListAsync(ct);
    }
}
