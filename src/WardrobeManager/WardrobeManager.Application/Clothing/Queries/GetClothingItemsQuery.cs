using MediatR;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Clothing.Queries;

public record GetClothingItemsQuery(Guid UserId) : IRequest<List<ClothingItem>>;