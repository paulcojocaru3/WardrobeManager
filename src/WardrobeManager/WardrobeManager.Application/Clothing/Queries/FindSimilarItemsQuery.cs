using MediatR;

namespace WardrobeManager.Application.Clothing.Queries;

// "More like this": visually-closest wardrobe items to an existing one, via CLIP embeddings + pgvector.
public record FindSimilarItemsQuery(Guid UserId, Guid ItemId, int Limit = 8, bool SameTypeOnly = false)
    : IRequest<List<SimilarItemDto>>;
