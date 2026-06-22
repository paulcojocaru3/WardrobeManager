using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Dtos;

namespace WardrobeManager.Application.Users.Commands;

public sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher
) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        // any credential change requires proving knowledge of the current password.
        var changingCredentials = request.NewPassword != null
            || (request.Email != null && request.Email != user.Email)
            || (request.Username != null && request.Username != user.Username);

        if (changingCredentials && !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new Exception("Current password is incorrect.");

        if (request.Username != null && request.Username != user.Username)
        {
            var taken = await userRepository.GetByUsernameAsync(request.Username, ct);
            if (taken != null) throw new Exception("Username is already taken.");
            user.Username = request.Username;
        }

        if (request.Email != null && request.Email != user.Email)
        {
            var taken = await userRepository.GetByEmailAsync(request.Email, ct);
            if (taken != null) throw new Exception("Email is already in use.");
            user.Email = request.Email;
        }

        if (request.NewPassword != null)
            user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await userRepository.UpdateAsync(user, ct);
        return UserDto.FromEntity(user);
    }
}
