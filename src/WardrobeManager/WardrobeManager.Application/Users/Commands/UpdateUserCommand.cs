using MediatR;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string? Username,
    string? Email,
    string? NewPassword,
    string CurrentPassword
) : IRequest<User>;
