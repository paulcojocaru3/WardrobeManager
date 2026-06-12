using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing;

namespace WardrobeManager.Application.Seeding.Commands;

// one-time backfill: derive SubType for existing items from their stored embeddings (no re-upload)
public record BackfillSubTypesCommand(Guid UserId) : IRequest<int>;

public sealed class BackfillSubTypesCommandHandler(
    IClothingRepository clothingRepository,
    IMlService mlService) : IRequestHandler<BackfillSubTypesCommand, int>
{
    public async Task<int> Handle(BackfillSubTypesCommand request, CancellationToken ct)
    {
        var items = await clothingRepository.GetMissingSubTypeWithEmbeddingAsync(request.UserId, ct);
        if (items.Count == 0) return 0;

        var embeddings = items.Select(i => i.Embedding!).ToList();
        var labels = await mlService.PredictArticleTypesAsync(embeddings, ct);
        if (labels.Count != items.Count)
            throw new InvalidOperationException("ML returned an unexpected number of predictions.");

        for (int i = 0; i < items.Count; i++)
            items[i].SubType = ArticleTypeMap.Normalize(labels[i]);

        await clothingRepository.UpdateRangeAsync(items, ct);
        return items.Count;
    }
}
