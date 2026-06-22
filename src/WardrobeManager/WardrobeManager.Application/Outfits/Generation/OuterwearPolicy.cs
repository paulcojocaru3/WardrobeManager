using System;

namespace WardrobeManager.Application.Outfits.Generation;

// single source of truth for "should this outfit include an outerwear layer", shared by the deterministic
public static class OuterwearPolicy
{
    public static bool ShouldIncludeOuterwear(
        string? mode, double thresholdC, double? temperatureC, string? temperatureHint)
    {
        switch (mode?.ToLowerInvariant())
        {
            case "always": return true;
            case "never": return false;
            default: // "auto" / null
                if (temperatureC is double t)
                {
                    return t <= thresholdC;
                }
                return temperatureHint?.ToLowerInvariant() switch
                {
                    "hot" or "warm" => false,
                    "cold" or "cool" => true,
                    _ => false // no information -> don't force a layer (respect the user's threshold)
                };
        }
    }
}
