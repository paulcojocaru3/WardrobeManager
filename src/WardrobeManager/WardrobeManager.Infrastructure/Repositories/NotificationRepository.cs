using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, bool unreadOnly, int take, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var row = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (row == null) return false;
        if (!row.MarkRead(DateTime.UtcNow)) return true;

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (rows.Count == 0) return 0;

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.MarkRead(now);
        }
        await _context.SaveChangesAsync(ct);
        return rows.Count;
    }

    public async Task<bool> ExistsByDedupKeyAsync(Guid userId, string dedupKey, CancellationToken ct = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.UserId == userId && n.DedupKey == dedupKey, ct);
    }
}
