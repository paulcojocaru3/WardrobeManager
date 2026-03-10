using MediatR;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Commands;

public record RegisterUserCommand(string Email, string PasswordHash, string Username) : IRequest<User>;