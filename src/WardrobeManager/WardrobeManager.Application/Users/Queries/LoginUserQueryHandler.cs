using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Queries;

public sealed class LoginUserQueryHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginUserQuery, AuthResponse?>
{
    public async Task<AuthResponse?> Handle(LoginUserQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);

        if (user == null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var token = jwtTokenService.GenerateToken(user);
        return new AuthResponse(token, UserDto.FromEntity(user));
    }
}
