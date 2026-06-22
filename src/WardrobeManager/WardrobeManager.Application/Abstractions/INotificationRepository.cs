using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);

    // newest first; unreadOnly filters to IsRead == false. take caps the page size.
    Task<IReadOnlyList<Notification>> GetByUserAsync(Guid userId, bool unreadOnly, int take, CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    // returns false when the notification doesn't exist or isn't the user's.
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);

    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default);

    // used by repeat-fire producers (weather drift) to avoid duplicate notifications.
    Task<bool> ExistsByDedupKeyAsync(Guid userId, string dedupKey, CancellationToken ct = default);
}
