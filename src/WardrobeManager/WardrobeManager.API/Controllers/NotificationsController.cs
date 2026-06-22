using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Notifications.Commands;
using WardrobeManager.Application.Notifications.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool unreadOnly = false, [FromQuery] int take = 30, CancellationToken ct = default)
    {
        var items = await mediator.Send(new GetNotificationsQuery(User.GetUserId(), unreadOnly, take), ct);
        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var count = await mediator.Send(new GetUnreadCountQuery(User.GetUserId()), ct);
        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var ok = await mediator.Send(new MarkNotificationReadCommand(User.GetUserId(), id), ct);
        if (!ok) return NotFound();
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var count = await mediator.Send(new MarkAllNotificationsReadCommand(User.GetUserId()), ct);
        return Ok(new { count });
    }

}
