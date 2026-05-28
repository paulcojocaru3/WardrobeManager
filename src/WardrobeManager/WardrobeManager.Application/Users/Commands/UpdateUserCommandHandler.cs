using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Commands;

public class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IValidator<UpdateUserCommand> validator
) : IRequestHandler<UpdateUserCommand, User>
{
    public async Task<User> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new Exception("User not found.");

        if (request.NewPassword != null)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || user.PasswordHash != request.CurrentPassword)
                throw new Exception("Current password is incorrect.");
        }

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
            user.PasswordHash = request.NewPassword!;

        await userRepository.UpdateAsync(user, ct);
        return user;
    }
}
