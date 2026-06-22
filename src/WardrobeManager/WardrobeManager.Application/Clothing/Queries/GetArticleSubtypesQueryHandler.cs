using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Queries;

public sealed class GetArticleSubtypesQueryHandler(IMlService mlService)
    : IRequestHandler<GetArticleSubtypesQuery, Dictionary<string, List<string>>>
{
    public async Task<Dictionary<string, List<string>>> Handle(GetArticleSubtypesQuery request, CancellationToken ct)
    {
        var labels = await mlService.GetArticleTypesAsync(ct);

        return labels
            .Select(l => (Norm: ArticleTypeMap.Normalize(l), Type: ArticleTypeMap.TryGetClothingType(l)))
            .Where(x => x.Norm != null && x.Type != null)
            .GroupBy(x => x.Type!.Value.ToString().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Select(x => x.Norm!).Distinct().OrderBy(s => s).ToList());
    }
}
