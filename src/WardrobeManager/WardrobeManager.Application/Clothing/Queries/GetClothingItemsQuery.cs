using MediatR;

namespace WardrobeManager.Application.Clothing.Queries;

public record GetClothingItemsQuery(Guid UserId) : IRequest<List<ClothingItemDto>>;
