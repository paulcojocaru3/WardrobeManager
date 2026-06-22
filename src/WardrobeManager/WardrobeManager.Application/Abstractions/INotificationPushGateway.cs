using WardrobeManager.Application.Notifications.Queries;

namespace WardrobeManager.Application.Abstractions;

public interface INotificationPushGateway
{
    Task PushAsync(Guid userId, NotificationDto notification, CancellationToken ct = default);
}
