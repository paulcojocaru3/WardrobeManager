using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Commands;

public class RegisterUserCommandHandler(IUserRepository userRepository, IValidator<RegisterUserCommand> validator) : IRequestHandler<RegisterUserCommand, User>
{
    public async Task<User> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = new User 
        { 
            Id = Guid.NewGuid(),
            Email = request.Email, 
            PasswordHash = request.PasswordHash, 
            Username = request.Username 
        };

        await userRepository.AddAsync(user, ct);
        return user;
    }
}
