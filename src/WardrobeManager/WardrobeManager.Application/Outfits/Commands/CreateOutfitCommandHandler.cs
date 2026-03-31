using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public class CreateOutfitCommandHandler(
    IOutfitRepository outfitRepository, 
    IClothingRepository clothingRepository,
    IValidator<CreateOutfitCommand> validator) : IRequestHandler<CreateOutfitCommand, Guid>
{
    public async Task<Guid> Handle(CreateOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var items = new List<ClothingItem>();
        foreach (var id in request.ItemIds)
        {
            var item = await clothingRepository.GetByIdAsync(id, ct);
            if (item != null) items.Add(item);
        }

        var outfit = new Outfit
        {
            UserId = request.UserId,
            Name = request.Name,
            IsAiGenerated = request.IsAiGenerated,
            Items = items,
            CreatedAt = DateTime.UtcNow
        };

        await outfitRepository.AddAsync(outfit, ct);
        return outfit.Id;
    }
}
