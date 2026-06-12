using MediatR;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string? Username,
    string? Email,
    string? NewPassword,
    string CurrentPassword
) : IRequest<UserDto>;
