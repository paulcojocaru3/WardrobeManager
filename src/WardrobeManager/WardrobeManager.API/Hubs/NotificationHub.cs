using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WardrobeManager.API.Hubs;

// push-only hub. Clients just connect (authenticated); the server pushes "notification" messages to
[Authorize]
public sealed class NotificationHub : Hub
{
}
