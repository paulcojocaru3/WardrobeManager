using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Outfits.Queries;

public sealed class GetOutfitsQueryHandler(IOutfitRepository outfitRepository, IValidator<GetOutfitsQuery> validator) : IRequestHandler<GetOutfitsQuery, List<OutfitDto>>
{
    public async Task<List<OutfitDto>> Handle(GetOutfitsQuery request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var outfits = await outfitRepository.GetByUserIdAsync(request.UserId, ct);

        return outfits.Where(o => !o.IsEventExclusive).Select(o => new OutfitDto(
            o.Id,
            o.Name,
            o.IsAiGenerated,
            o.IsFavorite,
            o.Tags,
            o.CreatedAt,
            o.Items.Select(i => new ClothingItemDto(
                i.Id, i.Name, i.Type, i.SubType, i.Color, i.Gender, i.Season, i.Usage, i.ProcessedImageUrl!, i.CreatedAt
            )).ToList()
        )).ToList();
    }
}
