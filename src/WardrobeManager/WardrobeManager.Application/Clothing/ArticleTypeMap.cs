using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Clothing;

public static class ArticleTypeMap
{
    // lowercased, "type_"-stripped, trimmed — this is what we persist as SubType
    public static string? Normalize(string? label)
    {
        var clean = label?.ToLowerInvariant().Replace("type_", "").Trim();
        if (string.IsNullOrWhiteSpace(clean))
        {
            return null;
        }
        return clean;
    }

    // Upload path always needs a type, so unknown labels fall back to Outerwear.
    public static ClothingType ToClothingType(string? label)
    {
        var type = TryGetClothingType(label);
        if (type != null)
        {
            return type.Value;
        }
        return ClothingType.Outerwear;
    }

    // coarse type only for recognized labels; null lets callers skip non-garment classes (beauty/underwear)
    public static ClothingType? TryGetClothingType(string? label) => Normalize(label) switch
    {
        // TOPS
        "shirts" or "tops" or "tshirts" or "kurta" or "kurtas" or "tunics" or "kurtis"
            or "dresses" or "jumpsuit" or "rompers" => ClothingType.Top,

        // BOTTOMS
        "pants" or "trousers" or "skirts" or "jeans" or "shorts" or "track pants" or "leggings"
            or "jeggings" or "capris" or "tights" or "churidar" or "lounge pants" or "lounge shorts"
            or "patiala" or "salwar" or "rain trousers" => ClothingType.Bottom,

        // SHOES
        "shoes" or "heels" or "sports shoes" or "casual shoes" or "flip flops" or "sandals"
            or "flats" or "formal shoes" or "booties" or "sports sandals" => ClothingType.Shoes,

        // ACCESSORIES
        "watches" or "sunglasses" or "belts" or "wallets" or "backpacks" or "caps" or "hat"
            or "bangle" or "bracelet" or "earrings" or "jewellery set" or "necklace and chains"
            or "pendant" or "ring" or "wristbands" or "clutches" or "headband" or "scarves"
            or "stoles" or "ties" or "umbrellas" => ClothingType.Accessory,

        // OUTERWEAR
        "jackets" or "sweaters" or "sweatshirts" or "shrug" or "rain jacket" or "waistcoat"
            or "blazers" or "suits" or "nehru jackets" => ClothingType.Outerwear,

        _ => null
    };
}
