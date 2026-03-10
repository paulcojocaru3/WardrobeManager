namespace WardrobeManager.Domain.Entities;

public class Outfit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAiGenerated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public List<ClothingItem> Items { get; set; } = new();
}