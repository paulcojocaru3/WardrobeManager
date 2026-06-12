using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

// picks the seed item an outfit is built around, from the parsed prompt intent
public interface IStartItemSelector
{
    Task<ClothingItem?> SelectAsync(Guid userId, PromptIntent intent, IReadOnlyCollection<Guid>? excludedItemIds = null, WeatherData? weather = null, CancellationToken ct = default);
}
