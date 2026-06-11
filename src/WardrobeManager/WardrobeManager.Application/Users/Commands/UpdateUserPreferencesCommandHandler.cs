using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Commands;

public sealed class UpdateUserPreferencesCommandHandler(IUserRepository userRepository)
    : IRequestHandler<UpdateUserPreferencesCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserPreferencesCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        if (request.FavoriteColors != null)
            user.FavoriteColors = request.FavoriteColors
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

        if (request.PreferredCity != null)
        {
            if (string.IsNullOrWhiteSpace(request.PreferredCity))
            {
                user.PreferredCity = null;
            }
            else
            {
                user.PreferredCity = request.PreferredCity.Trim();
            }
        }

        if (request.ThemePreference != null)
        {
            if (string.IsNullOrWhiteSpace(request.ThemePreference))
            {
                user.ThemePreference = null;
            }
            else
            {
                user.ThemePreference = request.ThemePreference.Trim();
            }
        }

        if (request.OuterwearMode != null)
        {
            var mode = request.OuterwearMode.Trim().ToLowerInvariant();
            if (mode is "always" or "never")
            {
                user.OuterwearMode = mode;
            }
            else
            {
                user.OuterwearMode = "auto";
            }
        }

        if (request.OuterwearTempThreshold != null)
            user.OuterwearTempThreshold = Math.Clamp(request.OuterwearTempThreshold.Value, 5, 30);

        await userRepository.UpdateAsync(user, ct);
        return UserDto.FromEntity(user);
    }
}
