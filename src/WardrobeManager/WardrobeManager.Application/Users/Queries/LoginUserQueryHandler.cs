using FluentValidation;
using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Users.Queries;

public class LoginUserQueryHandler(IUserRepository userRepository, IValidator<LoginUserQuery> validator) : IRequestHandler<LoginUserQuery, User?>
{
    public async Task<User?> Handle(LoginUserQuery request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        
        if (user != null && user.PasswordHash == request.Password)
        {
            return user;
        }

        return null;
    }
}
