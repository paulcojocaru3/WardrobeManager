using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Generation;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class GenerateOutfitCommandHandler(
    IUserRepository userRepository,
    IClothingRepository clothingRepository,
    IOutfitRepository outfitRepository,
    IOutfitGenerator outfitGenerator,
    TimeProvider? clock = null) : IRequestHandler<GenerateOutfitCommand, OutfitDto>
{
    public async Task<OutfitDto> Handle(GenerateOutfitCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }

        var aiResult = await outfitGenerator.GenerateAiOutfitAsync(
            request.UserId,
            request.StartItemId,
            new OutfitGenerationOptions { Threshold = 0.5 },
            ct);

        var itemsInDb = await clothingRepository.GetByIdsAsync(aiResult.SelectedItems.Select(i => i.Id), ct);

        var outfit = Outfit.Create(
            user.Id,
            aiResult.Name,
            itemsInDb,
            (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime);

        await outfitRepository.AddAsync(outfit, ct);

        return new OutfitDto(
            outfit.Id,
            outfit.Name,
            outfit.IsAiGenerated,
            outfit.IsFavorite,
            outfit.Tags,
            outfit.CreatedAt,
            outfit.Items.Select(i => new ClothingItemDto(
                i.Id, i.Name, i.Type, i.SubType, i.Color, i.Gender, i.Season, i.Usage, i.ProcessedImageUrl ?? string.Empty, i.CreatedAt
            )).ToList()
        );
    }
}
