using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        if (result == null) return Unauthorized("wrong credentials");
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command, CancellationToken ct)
    {
        if (id != User.GetUserId()) return Forbid();
        try
        {
            // Identity comes from the token, never the route/body.
            var updated = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public record UpdatePreferencesRequest(
        List<string>? FavoriteColors,
        string? PreferredCity,
        string? ThemePreference,
        string? OuterwearMode = null,
        int? OuterwearTempThreshold = null);

    [HttpPut("{id:guid}/preferences")]
    public async Task<IActionResult> UpdatePreferences(Guid id, [FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        if (id != User.GetUserId()) return Forbid();
        var updated = await mediator.Send(new UpdateUserPreferencesCommand(
            User.GetUserId(), request.FavoriteColors, request.PreferredCity, request.ThemePreference,
            request.OuterwearMode, request.OuterwearTempThreshold), ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (id != User.GetUserId()) return Forbid();
        await mediator.Send(new DeleteUserCommand(User.GetUserId()), ct);
        return NoContent();
    }
}
