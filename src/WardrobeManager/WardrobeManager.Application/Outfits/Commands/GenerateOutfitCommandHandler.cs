using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Clothing;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public class GenerateOutfitCommandHandler(
    IUserRepository userRepository,
    IClothingRepository clothingRepository,
    IOutfitRepository outfitRepository,
    OutfitGenerator outfitGenerator,
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

        var allItems = await clothingRepository.GetByUserIdAsync(request.UserId, ct);
        
        var startItem = allItems.FirstOrDefault(i => i.Id == request.StartItemId);
        if (startItem == null)
        {
            throw new InvalidOperationException($"Start item with ID {request.StartItemId} was not found in the user's wardrobe.");
        }

        var outfit = outfitGenerator.Create(user, startItem, allItems);

        await outfitRepository.AddAsync(outfit, ct);

        return new OutfitDto(
            outfit.Id,
            outfit.Name,
            outfit.IsAiGenerated,
            outfit.CreatedAt,
            outfit.Items.Select(i => new ClothingItemDto(
                i.Id, i.Name, i.Type, i.Color, i.ProcessedImageUrl, i.CreatedAt
            )).ToList()
        );
    }
}
