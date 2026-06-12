using MediatR;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Commands;

public record RegisterUserCommand(string Email, string Password, string Username) : IRequest<AuthResponse>;
