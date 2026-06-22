using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Notifications.Commands;

public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<int>;

public sealed class MarkAllNotificationsReadCommandHandler(INotificationRepository repository)
    : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    public Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct) =>
        repository.MarkAllReadAsync(request.UserId, ct);
}
