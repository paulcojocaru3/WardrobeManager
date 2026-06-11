using WardrobeManager.Application.Outfits.Generation;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitGenerator
{
    Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(Guid userId, Guid startItemId, OutfitGenerationOptions options, CancellationToken ct = default);
}
