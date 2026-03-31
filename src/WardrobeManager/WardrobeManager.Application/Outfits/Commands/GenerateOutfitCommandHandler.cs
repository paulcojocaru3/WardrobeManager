using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing.Queries;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public class GenerateOutfitCommandHandler(
    IUserRepository userRepository,
    IClothingRepository clothingRepository,
    IOutfitRepository outfitRepository,
    IOutfitGenerator outfitGenerator,
    IValidator<GenerateOutfitCommand> validator) : IRequestHandler<GenerateOutfitCommand, OutfitDto>
{
    public async Task<OutfitDto> Handle(GenerateOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} was not found.");
        }


        var aiResult = await outfitGenerator.GenerateAiOutfitAsync(request.UserId, request.StartItemId, 0.5, ct: ct);

        var itemsInDb = new List<ClothingItem>();
        foreach (var item in aiResult.SelectedItems)
        {
            var dbItem = await clothingRepository.GetByIdAsync(item.Id, ct);
            if (dbItem != null) itemsInDb.Add(dbItem);
        }

        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = aiResult.Name,
            IsAiGenerated = true,
            Items = itemsInDb,
            CreatedAt = DateTime.UtcNow
        };

        await outfitRepository.AddAsync(outfit, ct);

        return new OutfitDto(
            outfit.Id,
            outfit.Name,
            outfit.IsAiGenerated,
            outfit.CreatedAt,
            outfit.Items.Select(i => new ClothingItemDto(
                i.Id, i.Name, i.Type, i.Color, i.Gender, i.Season, i.Usage, i.ProcessedImageUrl ?? string.Empty, i.CreatedAt
            )).ToList()
        );
    }
}
