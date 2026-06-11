using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Domain.Entities;

public class OutfitFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid GenerationId { get; set; }
    public Guid ClothingItemId { get; set; }

    public ClothingType SlotType { get; set; }
    public int Rank { get; set; }
    public double FinalScore { get; set; }

    // normalized per-evaluator scores — these are the training features
    public Dictionary<string, double> EvaluatorScores { get; set; } = new();

    public FeedbackAction Action { get; set; } = FeedbackAction.Shown;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActionedAt { get; set; }
}
