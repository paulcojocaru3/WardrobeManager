namespace WardrobeManager.Application.Abstractions;

// maps an occasion word (casual, work, smart, formal, ...) to a target formality level on the 1..5 scale,
public interface IOccasionFormalityRules
{
    int? FormalityFor(string? occasion);
}
