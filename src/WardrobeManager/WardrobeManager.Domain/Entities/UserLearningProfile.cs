namespace WardrobeManager.Domain.Entities;

// per-user taste learned from feedback via EMA. Keys are normalized (color family, style tag);
public class UserLearningProfile
{
    public Guid UserId { get; set; }

    public Dictionary<string, double> ColorScores { get; set; } = new();
    public Dictionary<string, double> StyleScores { get; set; } = new();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
