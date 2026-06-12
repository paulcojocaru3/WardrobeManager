namespace WardrobeManager.Domain.Entities;

// per-user learned weights; when absent the generator falls back to the defaults (cold start)
public class UserEvaluatorWeights
{
    public Guid UserId { get; set; }

    // evaluator name -> weight
    public Dictionary<string, double> Weights { get; set; } = new();

    // learned weight for the raw CLIP similarity term
    public double MlWeight { get; set; } = 0.15;

    public int TrainedOnSamples { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
