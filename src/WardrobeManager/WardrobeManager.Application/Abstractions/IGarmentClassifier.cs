using WardrobeManager.Application.Outfits.Prompting;

namespace WardrobeManager.Application.Abstractions;

public interface IGarmentClassifier
{
    IReadOnlyList<RequestedGarment> Detect(string prompt);
}
