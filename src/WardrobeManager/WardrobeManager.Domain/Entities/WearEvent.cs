namespace WardrobeManager.Domain.Entities;

public class WearEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid ClothingItemId { get; set; }
    public Guid? OutfitId { get; set; }
    public DateTime WearDate { get; set; } = DateTime.UtcNow;

    // navigation properties
    public User? User { get; set; }
    public ClothingItem? ClothingItem { get; set; }
    public Outfit? Outfit { get; set; }

    public static WearEvent RecordClothingWear(
        Guid userId,
        Guid clothingItemId,
        Guid? outfitId,
        DateTime wearDate)
    {
        return new WearEvent
        {
            UserId = userId,
            ClothingItemId = clothingItemId,
            OutfitId = outfitId,
            WearDate = wearDate
        };
    }
}
