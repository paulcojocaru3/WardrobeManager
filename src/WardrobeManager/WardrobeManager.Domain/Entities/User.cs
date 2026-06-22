namespace WardrobeManager.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> FavoriteColors { get; set; } = new();
    public List<string> AvoidColors { get; set; } = new();

    public string? PreferredCity { get; set; }
    public string? ThemePreference { get; set; }

    public string VarietyLevel { get; set; } = "normal";

    public int? DefaultReuseAfterDays { get; set; } = 3;

    public bool BlockDuplicateUploads { get; set; }

    public bool PreferLightOnHotDays { get; set; } = true;

    // wait for gemma3 to return the final outfit.
    public bool UseGemmaStylistForOutfits { get; set; }

    public string? OuterwearMode { get; set; }

    public int OuterwearTempThreshold { get; set; } = 23;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ClothingItem> ClothingItems { get; set; } = new();
    public List<Outfit> Outfits { get; set; } = new();
    public List<WearEvent> WearEvents { get; set; } = new();
    public List<PlannerEvent> PlannerEvents { get; set; } = new();
}
