using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Users.Commands;

public sealed class DeleteUserCommandHandler(IUserRepository userRepository) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        await userRepository.DeleteAsync(user, ct);
    }
}
