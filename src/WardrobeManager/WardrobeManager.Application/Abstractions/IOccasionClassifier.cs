namespace WardrobeManager.Application.Abstractions;

public interface IOccasionClassifier
{
    string? ClassifyStyle(string prompt);
}
