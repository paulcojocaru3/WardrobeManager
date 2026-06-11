using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Dtos;

// safe projection of User for API responses — never carries the password hash
public record UserDto(
    Guid Id,
    string Username,
    string Email,
    List<string> FavoriteColors,
    string? PreferredCity,
    string? ThemePreference,
    string? OuterwearMode,
    int OuterwearTempThreshold,
    DateTime CreatedAt)
{
    public static UserDto FromEntity(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.FavoriteColors,
        user.PreferredCity,
        user.ThemePreference,
        user.OuterwearMode,
        user.OuterwearTempThreshold,
        user.CreatedAt);
}
