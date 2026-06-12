namespace WardrobeManager.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> FavoriteColors { get; set; } = new();

    // User preferences synced to the account (no longer localStorage-only).
    public string? PreferredCity { get; set; }
    public string? ThemePreference { get; set; }

    // Outerwear policy for outfit generation: "auto" (null) | "always" | "never".
    public string? OuterwearMode { get; set; }
    // In "auto" mode, outerwear is dropped above this temperature (°C). Default 23 = legacy behavior.
    public int OuterwearTempThreshold { get; set; } = 23;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public List<ClothingItem> ClothingItems { get; set; } = new();
    public List<Outfit> Outfits { get; set; } = new();
    public List<WearEvent> WearEvents { get; set; } = new();
    public List<PlannerEvent> PlannerEvents { get; set; } = new();
}