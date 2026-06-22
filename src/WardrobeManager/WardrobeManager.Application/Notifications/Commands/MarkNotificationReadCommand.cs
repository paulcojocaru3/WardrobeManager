using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Notifications.Commands;

public record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest<bool>;

public sealed class MarkNotificationReadCommandHandler(INotificationRepository repository)
    : IRequestHandler<MarkNotificationReadCommand, bool>
{
    public Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken ct) =>
        repository.MarkReadAsync(request.UserId, request.NotificationId, ct);
}
