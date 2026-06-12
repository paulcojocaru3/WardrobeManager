using MediatR;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Queries;

public record LoginUserQuery(string Email, string Password) : IRequest<AuthResponse?>;
