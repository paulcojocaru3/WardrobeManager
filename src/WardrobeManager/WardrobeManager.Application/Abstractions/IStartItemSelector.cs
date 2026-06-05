using WardrobeManager.Application.Outfits.Prompting;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

/// <summary>
/// Chooses the seed clothing item an outfit is generated around, based on the
/// parsed prompt intent (semantic match) instead of a random pick.
/// </summary>
public interface IStartItemSelector
{
    Task<ClothingItem?> SelectAsync(Guid userId, PromptIntent intent, CancellationToken ct = default);
}
