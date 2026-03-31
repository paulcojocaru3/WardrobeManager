using WardrobeManager.Domain.Entities;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Abstractions;

public interface IOutfitGenerator
{
    Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(Guid userId, Guid startItemId, double threshold = 0.5, WeatherData? weatherData = null, string? style = null, CancellationToken ct = default);
}
