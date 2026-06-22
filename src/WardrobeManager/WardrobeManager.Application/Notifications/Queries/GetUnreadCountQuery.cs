using MediatR;
using WardrobeManager.Application.Abstractions;

namespace WardrobeManager.Application.Notifications.Queries;

public record GetUnreadCountQuery(Guid UserId) : IRequest<int>;

public sealed class GetUnreadCountQueryHandler(INotificationRepository repository)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct) =>
        repository.GetUnreadCountAsync(request.UserId, ct);
}
