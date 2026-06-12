using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class UpdateOutfitCommandHandler(
    IOutfitRepository outfitRepository, 
    IClothingRepository clothingRepository,
    IValidator<UpdateOutfitCommand> validator) : IRequestHandler<UpdateOutfitCommand, bool>
{
    public async Task<bool> Handle(UpdateOutfitCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var outfit = await outfitRepository.GetByIdAsync(request.Id, ct);
        if (outfit == null)
        {
            throw new InvalidOperationException($"Outfit with ID {request.Id} was not found.");
        }

        outfit.Name = request.Name;
        if (request.Tags != null)
        {
            outfit.Tags = request.Tags;
        }

        var fetched = (await clothingRepository.GetByIdsAsync(request.ItemIds, ct)).ToDictionary(i => i.Id);
        var newItems = new List<ClothingItem>();
        foreach (var itemId in request.ItemIds)
        {
            if (!fetched.TryGetValue(itemId, out var item)) continue;
            if (newItems.Any(i => i.Type == item.Type))
            {
                throw new InvalidOperationException($"Outfit already contains an item of type {item.Type}. Each type must be unique.");
            }
            newItems.Add(item);
        }

outfit.Items = newItems;

        await outfitRepository.UpdateAsync(outfit, ct);
        return true;
    }
}
