using MediatR;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Queries;

public record LoginUserQuery(string Email, string Password) : IRequest<User?>;