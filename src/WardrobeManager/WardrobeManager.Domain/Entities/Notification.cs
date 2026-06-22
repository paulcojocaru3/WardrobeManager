namespace WardrobeManager.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    // "WeatherAlert" | "DuplicateDetected"
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // free-form JSON the frontend uses to act on the notification (itemId, WeatherAlertDto, ...).
    public string? Payload { get; set; }

    // de-duplication marker for producers that can fire repeatedly (weather drift); null = always insert.
    public string? DedupKey { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    public User? User { get; set; }

    public static Notification Create(
        Guid userId,
        string type,
        string title,
        string message,
        string? payload,
        string? dedupKey,
        DateTime createdAt)
    {
        return new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            Payload = payload,
            DedupKey = dedupKey,
            IsRead = false,
            CreatedAt = createdAt
        };
    }

    public bool MarkRead(DateTime readAt)
    {
        if (IsRead)
        {
            return false;
        }

        IsRead = true;
        ReadAt = readAt;
        return true;
    }
}
