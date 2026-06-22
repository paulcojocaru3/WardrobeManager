using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Queries;

public sealed class FindSimilarItemsQueryHandler(IClothingRepository clothingRepository)
    : IRequestHandler<FindSimilarItemsQuery, List<SimilarItemDto>>
{
    public async Task<List<SimilarItemDto>> Handle(FindSimilarItemsQuery request, CancellationToken ct)
    {
        var seed = await clothingRepository.GetByIdAsync(request.ItemId, ct);

        // ownership guard + we can only search when the seed has an embedding.
        if (seed == null || seed.UserId != request.UserId || seed.Embedding == null)
        {
            return [];
        }

        // fetch one extra: the seed itself will rank first (cosine ~1.0) and gets filtered out below.
        var matches = await clothingRepository.GetSimilarItemsAsync(
            request.UserId,
            seed.Embedding,
            type: request.SameTypeOnly ? seed.Type : null,
            limit: request.Limit + 1,
            gender: seed.Gender,
            ct: ct);

        return matches
            .Where(m => m.Item.Id != seed.Id)
            .Take(request.Limit)
            .Select(m => new SimilarItemDto(ClothingItemDto.From(m.Item), m.Similarity))
            .ToList();
    }
}
