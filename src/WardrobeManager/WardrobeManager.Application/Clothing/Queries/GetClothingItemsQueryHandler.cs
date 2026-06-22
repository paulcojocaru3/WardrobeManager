using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Queries;

public sealed class GetClothingItemsQueryHandler(IClothingRepository clothingRepository) : IRequestHandler<GetClothingItemsQuery, List<ClothingItemDto>>
{
    public async Task<List<ClothingItemDto>> Handle(GetClothingItemsQuery request, CancellationToken ct)
    {
        var items = await clothingRepository.GetByUserIdAsync(request.UserId, ct);

        return items.Select(ClothingItemDto.From).ToList();
    }
}
