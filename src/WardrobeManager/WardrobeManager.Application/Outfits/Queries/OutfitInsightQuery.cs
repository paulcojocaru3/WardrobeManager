using MediatR;
using WardrobeManager.Application.Outfits.Explaining;

namespace WardrobeManager.Application.Outfits.Queries;

// richer, weather-aware insight for the Outfit-of-the-day card: a headline idea, a note per piece, and
public sealed record OutfitInsightQuery(
    Guid UserId,
    IReadOnlyList<Guid> ItemIds,
    string? Style = null,
    string? Occasion = null,
    string? City = null,
    IReadOnlyList<string>? Tradeoffs = null) : IRequest<OutfitInsight>;
