namespace WardrobeManager.Domain.Entities;

// per-user compatibility for an unordered item pair (canonical: ItemAId < ItemBId), accrued
public class ItemPairScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public Guid ItemAId { get; set; }
    public Guid ItemBId { get; set; }

    public int PairedCount { get; set; }
    public int PositiveCount { get; set; }
    public int NegativeCount { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
