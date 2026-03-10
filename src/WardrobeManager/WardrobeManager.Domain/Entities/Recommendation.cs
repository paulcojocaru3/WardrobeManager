namespace WardrobeManager.Domain.Entities;

public class Recommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string SuggestedItemName { get; set; } = string.Empty;
    public string? SuggestedItemUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}