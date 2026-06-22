using MediatR;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Commands;

// low-sensitivity preferences (colors, city, theme) — no current-password check needed
public record UpdateUserPreferencesCommand(
    Guid UserId,
    List<string>? FavoriteColors,
    string? PreferredCity,
    string? ThemePreference,
    string? OuterwearMode = null,
    int? OuterwearTempThreshold = null,
    List<string>? AvoidColors = null,
    string? VarietyLevel = null,
    bool? BlockDuplicateUploads = null,
    bool? PreferLightOnHotDays = null,
    bool? UseGemmaStylistForOutfits = null,
    int? DefaultReuseAfterDays = null,
    bool UpdateDefaultReuseAfterDays = false
) : IRequest<UserDto>;
