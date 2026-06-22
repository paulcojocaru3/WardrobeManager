using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Outfits.Commands;

public sealed class UpdateOutfitCommandHandler(
    IOutfitRepository outfitRepository, 
    IClothingRepository clothingRepository) : IRequestHandler<UpdateOutfitCommand, bool>
{
    public async Task<bool> Handle(UpdateOutfitCommand request, CancellationToken ct)
    {
        var outfit = await outfitRepository.GetByIdForUserAsync(request.Id, request.UserId, ct);
        if (outfit == null)
        {
            return false;
        }

        var requestedItemIds = request.ItemIds.Distinct().ToList();
        var fetched = (await clothingRepository.GetByIdsForUserAsync(requestedItemIds, request.UserId, ct)).ToDictionary(i => i.Id);
        if (fetched.Count != requestedItemIds.Count)
        {
            return false;
        }
        var newItems = new List<ClothingItem>();
        foreach (var itemId in request.ItemIds)
        {
            if (!fetched.TryGetValue(itemId, out var item)) continue;
            newItems.Add(item);
        }

        outfit.UpdateDetails(request.Name, request.Tags, newItems);

        await outfitRepository.UpdateAsync(outfit, ct);
        return true;
    }
}
