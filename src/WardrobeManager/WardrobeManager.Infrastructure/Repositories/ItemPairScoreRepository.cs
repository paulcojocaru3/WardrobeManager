using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class ItemPairScoreRepository : IItemPairScoreRepository
{
    private readonly ApplicationDbContext _context;

    public ItemPairScoreRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<(Guid, Guid), double>> GetCompatibilityMapAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.ItemPairScores
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.PairedCount >= 2)
            .ToListAsync(ct);

        var map = new Dictionary<(Guid, Guid), double>(rows.Count);
        foreach (var r in rows)
        {
            var compat = Math.Clamp((r.PositiveCount - r.NegativeCount) / (double)Math.Max(r.PairedCount, 1), -1.0, 1.0);
            map[(r.ItemAId, r.ItemBId)] = compat;
        }
        return map;
    }

    public async Task UpsertBatchAsync(Guid userId, IReadOnlyCollection<ItemPairDelta> deltas, CancellationToken ct = default)
    {
        if (deltas.Count == 0) return;

        // merge incoming deltas onto their canonical pair key.
        var merged = new Dictionary<(Guid, Guid), (int Paired, int Positive, int Negative)>();
        foreach (var d in deltas)
        {
            var key = ItemPair.Canonical(d.ItemAId, d.ItemBId);
            var cur = merged.TryGetValue(key, out var v) ? v : default;
            merged[key] = (cur.Paired + d.PairedDelta, cur.Positive + d.PositiveDelta, cur.Negative + d.NegativeDelta);
        }

        // load the touched rows in one query (over-fetch is filtered by exact pair lookup below).
        var aIds = merged.Keys.Select(k => k.Item1).Distinct().ToList();
        var bIds = merged.Keys.Select(k => k.Item2).Distinct().ToList();
        var existing = await _context.ItemPairScores
            .Where(p => p.UserId == userId && aIds.Contains(p.ItemAId) && bIds.Contains(p.ItemBId))
            .ToListAsync(ct);
        var existingByPair = existing.ToDictionary(p => (p.ItemAId, p.ItemBId));

        var now = DateTime.UtcNow;
        foreach (var (pair, delta) in merged)
        {
            if (existingByPair.TryGetValue(pair, out var row))
            {
                row.PairedCount += delta.Paired;
                row.PositiveCount += delta.Positive;
                row.NegativeCount += delta.Negative;
                row.UpdatedAt = now;
            }
            else
            {
                await _context.ItemPairScores.AddAsync(new ItemPairScore
                {
                    UserId = userId,
                    ItemAId = pair.Item1,
                    ItemBId = pair.Item2,
                    PairedCount = delta.Paired,
                    PositiveCount = delta.Positive,
                    NegativeCount = delta.Negative,
                    UpdatedAt = now
                }, ct);
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
