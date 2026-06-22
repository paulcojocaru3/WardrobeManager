using System.Text.Json;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Notifications.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Notifications;

public sealed class NotificationDispatcher(
    INotificationRepository repository,
    INotificationPushGateway pushGateway,
    ILogger<NotificationDispatcher> logger,
    TimeProvider? clock = null) : INotificationDispatcher
{
    public async Task DispatchAsync(
        Guid userId,
        string type,
        string title,
        string message,
        object? payload,
        string? dedupKey,
        CancellationToken ct = default)
    {
        if (dedupKey != null && await repository.ExistsByDedupKeyAsync(userId, dedupKey, ct))
        {
            return;
        }

        var json = payload == null ? null : JsonSerializer.Serialize(payload);
        var notification = Notification.Create(userId, type, title, message, json, dedupKey, (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime);

        await repository.AddAsync(notification, ct);

        try
        {
            await pushGateway.PushAsync(userId, ToDto(notification), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to push notification {Id}.", notification.Id);
        }
    }

    private static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.Payload,
            notification.IsRead,
            notification.CreatedAt);
    }
}
