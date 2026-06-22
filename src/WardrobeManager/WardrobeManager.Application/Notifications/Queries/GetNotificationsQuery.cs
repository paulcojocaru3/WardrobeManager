using MediatR;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Notifications.Queries;

public record GetNotificationsQuery(Guid UserId, bool UnreadOnly, int Take) : IRequest<IReadOnlyList<NotificationDto>>;

public sealed class GetNotificationsQueryHandler(INotificationRepository repository)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.Take, 1, 100);
        var rows = await repository.GetByUserAsync(request.UserId, request.UnreadOnly, take, ct);
        return rows.Select(ToDto).ToList();
    }

    internal static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Message, n.Payload, n.IsRead, n.CreatedAt);
}
