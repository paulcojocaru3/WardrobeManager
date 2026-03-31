using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Clothing.Queries;

public class GetClothingItemsQueryHandler(IClothingRepository clothingRepository, IValidator<GetClothingItemsQuery> validator) : IRequestHandler<GetClothingItemsQuery, List<ClothingItemDto>>
{
    public async Task<List<ClothingItemDto>> Handle(GetClothingItemsQuery request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        
        var items = await clothingRepository.GetByUserIdAsync(request.UserId, ct);

        return items.Select(i => new ClothingItemDto(
            i.Id,
            i.Name,
            i.Type,
            i.Color,
            i.Gender,
            i.Season,
            i.Usage,
            i.ProcessedImageUrl ?? string.Empty,
            i.CreatedAt
        )).ToList();
    }
}
