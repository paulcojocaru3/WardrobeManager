namespace WardrobeManager.Application.Outfits.Feasibility;

// hard constraints a candidate must satisfy to fill a slot. The enum order IS the relaxation order:
public enum ConstraintKind
{
    SubType = 0,       // requested article type (e.g. "jeans"); easiest to substitute
    DesiredColor = 1,  // requested color for the slot
    Style = 2,         // hard style clash (e.g. Sports piece in a Formal outfit)
    AvoidColor = 3,    // a color the user explicitly asked to avoid
    Gender = 4,        // the seed's gender lock
    Weather = 5        // unwearable for the weather (comfort) — relaxed only as a last resort
}
