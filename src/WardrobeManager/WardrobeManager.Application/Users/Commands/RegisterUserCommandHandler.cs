using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Dtos;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Commands;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (await userRepository.GetByEmailAsync(request.Email, ct) != null)
            throw new Exception("Email is already in use.");
        if (await userRepository.GetByUsernameAsync(request.Username, ct) != null)
            throw new Exception("Username is already taken.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Username = request.Username
        };

        await userRepository.AddAsync(user, ct);

        var token = jwtTokenService.GenerateToken(user);
        return new AuthResponse(token, UserDto.FromEntity(user));
    }
}
