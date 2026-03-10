namespace WardrobeManager.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> FavoriteColors { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<ClothingItem> ClothingItems { get; set; } = new();
    public List<Outfit> Outfits { get; set; } = new();
    public List<WearEvent> WearEvents { get; set; } = new();
}