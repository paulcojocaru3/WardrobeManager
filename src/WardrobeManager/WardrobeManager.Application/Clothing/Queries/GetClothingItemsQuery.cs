using MediatR;
using WardrobeManager.Application.Clothing;

namespace WardrobeManager.Application.Clothing.Queries;

public record GetClothingItemsQuery(Guid UserId) : IRequest<List<ClothingItemDto>>;
