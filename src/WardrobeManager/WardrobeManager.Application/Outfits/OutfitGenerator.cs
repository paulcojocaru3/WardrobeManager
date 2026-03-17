using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits;

public class OutfitGenerator
{
    private static readonly Random _random = new();

    public Outfit Create(User user, ClothingItem startItem, List<ClothingItem> allItems)
    {
        var itemsByType = allItems.ToLookup(i => i.Type);
        var outfitItems = new List<ClothingItem> { startItem };
        var neededTypes = GetNeededTypes(startItem.Type);

        foreach (var type in neededTypes)
        {
            var candidates = itemsByType[type].Where(i => !outfitItems.Contains(i)).ToList();
            if (candidates.Any())
            {
                // Selectie COMPLET RANDOM din lista de candidati de acelasi tip
                var randomIndex = _random.Next(candidates.Count);
                outfitItems.Add(candidates[randomIndex]);
            }
        }

        return new Outfit
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = $"Random Outfit with {startItem.Name}",
            IsAiGenerated = false,
            Items = outfitItems,
            CreatedAt = DateTime.UtcNow
        };
    }

    private List<ClothingType> GetNeededTypes(ClothingType startType)
    {
        return startType switch
        {
            ClothingType.Top => new List<ClothingType> { ClothingType.Bottom, ClothingType.Shoes },
            ClothingType.Bottom => new List<ClothingType> { ClothingType.Top, ClothingType.Shoes },
            ClothingType.Shoes => new List<ClothingType> { ClothingType.Top, ClothingType.Bottom },
            _ => new List<ClothingType> { ClothingType.Top, ClothingType.Bottom, ClothingType.Shoes }
        };
    }
}
