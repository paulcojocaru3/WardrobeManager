namespace WardrobeManager.Application.Abstractions;

// single entry point for raising a notification: persists it and pushes it live to the user.
public interface INotificationDispatcher
{
    // persists and pushes a notification. When dedupKey is non-null and a notification with that key
    Task DispatchAsync(
        Guid userId,
        string type,
        string title,
        string message,
        object? payload,
        string? dedupKey,
        CancellationToken ct = default);
}
