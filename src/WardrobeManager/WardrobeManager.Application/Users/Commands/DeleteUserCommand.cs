using MediatR;

namespace WardrobeManager.Application.Users.Commands;

public record DeleteUserCommand(Guid UserId) : IRequest;
