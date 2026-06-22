using MediatR;
using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Commands;

public sealed class ProcessClothingCommandHandler(
    IMlService mlService,
    IClothingRepository clothingRepository,
    INotificationDispatcher notificationDispatcher,
    ILogger<ProcessClothingCommandHandler> logger)
    : IRequestHandler<ProcessClothingCommand, ProcessedClothingDto>
{
    // cosine similarity above which an existing item is treated as a likely duplicate of the upload.
    private const double DuplicateThreshold = 0.85;
    private const int MaxDuplicatesShown = 3;

    public async Task<ProcessedClothingDto> Handle(ProcessClothingCommand request, CancellationToken ct)
    {
        using var stream = new MemoryStream(request.FileContent);
        var ml = await mlService.ProcessClothingImageAsync(stream, request.FileName, request.ContentType, ct);

        var type = ArticleTypeMap.ToClothingType(ml.Type);
        var duplicates = await FindDuplicatesAsync(request.UserId, ml.Embedding, type, ct);

        if (duplicates.Count > 0)
        {
            var top = duplicates[0];
            await notificationDispatcher.DispatchAsync(
                request.UserId,
                "DuplicateDetected",
                "Possible duplicate detected",
                $"\"{request.Name}\" looks very similar to \"{top.Name}\" already in your wardrobe.",
                new
                {
                    uploadName = request.Name,
                    existingItemId = top.Id,
                    existingItemName = top.Name,
                    existingItemImage = top.ImageUrl,
                    similarity = top.Similarity
                },
                dedupKey: null,
                ct);
        }

        return new ProcessedClothingDto(
            request.Name,
            type,
            ArticleTypeMap.Normalize(ml.Type),
            ml.Color,
            ml.Gender,
            ml.Season,
            ml.Usage,
            ml.ProcessedImageB64!,
            ml.Embedding,
            duplicates
        );
    }

    private async Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(
        Guid userId, float[]? embedding, Domain.Enums.ClothingType type, CancellationToken ct)
    {
        if (embedding == null)
        {
            logger.LogInformation("Duplicate check skipped: upload has no embedding.");
            return [];
        }

        // fetch top candidates WITHOUT a threshold so we can log the real best score, then filter.
        var matches = await clothingRepository.GetSimilarItemsAsync(
            userId, embedding, type: type, limit: MaxDuplicatesShown, threshold: null, ct: ct);

        if (matches == null || matches.Count == 0)
        {
            logger.LogInformation("Duplicate check: no existing {Type} items with an embedding for this user.", type);
            return [];
        }

        var best = matches[0];
        logger.LogInformation(
            "Duplicate check: top match '{Name}' similarity={Similarity:F3} (threshold={Threshold}), {Count} candidate(s).",
            best.Item.Name, best.Similarity, DuplicateThreshold, matches.Count);

        return matches
            .Where(m => m.Similarity >= DuplicateThreshold)
            .Select(m => new DuplicateCandidate(
                m.Item.Id, m.Item.Name, m.Item.ProcessedImageUrl ?? string.Empty, m.Similarity))
            .ToList();
    }
}
