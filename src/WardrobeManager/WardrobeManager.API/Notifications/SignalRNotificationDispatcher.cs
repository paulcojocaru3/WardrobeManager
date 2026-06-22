using Microsoft.AspNetCore.SignalR;
using WardrobeManager.API.Hubs;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Notifications.Queries;

namespace WardrobeManager.API.Notifications;

public sealed class SignalRNotificationDispatcher(
    IHubContext<NotificationHub> hub) : INotificationPushGateway
{
    public async Task PushAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        await hub.Clients.User(userId.ToString()).SendAsync("notification", notification, ct);
    }
}
