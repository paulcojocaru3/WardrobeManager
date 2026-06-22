namespace WardrobeManager.Application.Notifications.Queries;

// shape sent to the frontend over both REST and SignalR. Payload is a JSON string the client parses
public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? Payload,
    bool IsRead,
    DateTime CreatedAt);
