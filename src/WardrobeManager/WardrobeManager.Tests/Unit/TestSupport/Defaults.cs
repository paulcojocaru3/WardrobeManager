using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Infrastructure.ExternalServices;

namespace WardrobeManager.Tests.Unit.TestSupport;

// shared defaults for the two-stage generation model. ThermalRules with a missing path falls back to the
internal static class Defaults
{
    public static readonly IThermalRules Thermal = new ThermalRules("does-not-exist.json");
    public static readonly IGarmentFeasibility Feasibility = new GarmentFeasibility(Thermal);
}
