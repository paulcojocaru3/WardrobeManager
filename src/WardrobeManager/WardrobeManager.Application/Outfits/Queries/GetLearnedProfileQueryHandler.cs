using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class GetLearnedProfileQueryHandler(
    IUserLearningProfileRepository profileRepository,
    IItemPairScoreRepository pairScoreRepository,
    IClothingRepository clothingRepository) : IRequestHandler<GetLearnedProfileQuery, LearnedProfileDto>
{
    private const double FavoriteThreshold = 0.55; // above the 0.5 neutral prior = a learned liking
    private const double DislikeThreshold = 0.45;
    private const int MaxTastes = 6;
    private const int MaxPairs = 5;

    public async Task<LearnedProfileDto> Handle(GetLearnedProfileQuery request, CancellationToken ct)
    {
        var profile = await profileRepository.GetByUserIdAsync(request.UserId, ct);

        var topColors = TopTastes(profile?.ColorScores);
        var topStyles = TopTastes(profile?.StyleScores);
        var avoidedColors = AvoidedTastes(profile?.ColorScores);
        var strongPairs = await StrongPairsAsync(request.UserId, ct);

        return new LearnedProfileDto(topColors, topStyles, avoidedColors, strongPairs, profile?.UpdatedAt);
    }

    private static List<LearnedTasteDto> TopTastes(IReadOnlyDictionary<string, double>? scores)
    {
        if (scores is null) return new List<LearnedTasteDto>();
        return scores
            .Where(kv => kv.Value >= FavoriteThreshold)
            .OrderByDescending(kv => kv.Value)
            .Take(MaxTastes)
            .Select(kv => new LearnedTasteDto(kv.Key, kv.Value))
            .ToList();
    }

    private static List<LearnedTasteDto> AvoidedTastes(IReadOnlyDictionary<string, double>? scores)
    {
        if (scores is null) return new List<LearnedTasteDto>();
        return scores
            .Where(kv => kv.Value < DislikeThreshold)
            .OrderBy(kv => kv.Value)
            .Take(MaxTastes)
            .Select(kv => new LearnedTasteDto(kv.Key, kv.Value))
            .ToList();
    }

    private async Task<List<LearnedPairDto>> StrongPairsAsync(Guid userId, CancellationToken ct)
    {
        var map = await pairScoreRepository.GetCompatibilityMapAsync(userId, ct);
        var top = map
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Take(MaxPairs)
            .ToList();
        if (top.Count == 0) return new List<LearnedPairDto>();

        var ids = top.SelectMany(kv => new[] { kv.Key.Item1, kv.Key.Item2 }).Distinct();
        var names = (await clothingRepository.GetByIdsAsync(ids, ct))
            .ToDictionary(i => i.Id, i => i.Name);

        var result = new List<LearnedPairDto>(top.Count);
        foreach (var kv in top)
        {
            // skip pairs whose items have since been deleted.
            if (names.TryGetValue(kv.Key.Item1, out var a) && names.TryGetValue(kv.Key.Item2, out var b))
                result.Add(new LearnedPairDto(a, b, kv.Value));
        }
        return result;
    }
}
