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
    int? OuterwearTempThreshold = null
) : IRequest<UserDto>;
