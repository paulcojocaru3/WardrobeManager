using System.Collections.Generic;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

// keep one item per body slot after gemma3 selection.
public static class BodySlotDeduplicator
{
    public static List<ClothingItem> Deduplicate(IEnumerable<ClothingItem> items)
    {
        var result = new List<ClothingItem>();
        var usedSingleSlots = new HashSet<ClothingType>();

        foreach (var item in items)
        {
            if (IsStackable(item.Type))
            {
                result.Add(item);
                continue;
            }

            if (usedSingleSlots.Add(item.Type))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private static bool IsStackable(ClothingType type) => type == ClothingType.Accessory;
}
