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

    public async Task<IReadOnlyList<OutfitFeedback>> GetTrainingRowsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.OutfitFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Action != FeedbackAction.Shown)
            .ToListAsync(ct);
    }

    public Task<int> CountActionableAsync(Guid userId, CancellationToken ct = default)
    {
        return _context.OutfitFeedbacks
            .CountAsync(f => f.UserId == userId && f.Action != FeedbackAction.Shown, ct);
    }
}
