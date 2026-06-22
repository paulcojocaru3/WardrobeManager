using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Domain.Entities;

public class OutfitFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid GenerationId { get; set; }
    public Guid ClothingItemId { get; set; }

    public ClothingType SlotType { get; set; }

    // 0 = the slot's top pick; a Rank == 0 row later marked Rejected is an active swap-out
    public int Rank { get; set; }

    public FeedbackAction Action { get; set; } = FeedbackAction.Shown;
    public string? Occasion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActionedAt { get; set; }
}
