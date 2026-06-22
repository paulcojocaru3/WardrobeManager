namespace WardrobeManager.Application.Outfits.Generation;

public sealed class StylistSettings
{
    public bool Enabled { get; set; } = false;

    public int MaxCandidates { get; set; } = 24;

    // balance relevance and visual diversity in the candidate slate.
    public double MmrLambda { get; set; } = 0.7;
}
