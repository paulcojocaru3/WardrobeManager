using MediatR;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
    {
        try
        {
            var user = await mediator.Send(command);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserQuery query)
    {
        var user = await mediator.Send(query);
        if (user == null) return Unauthorized("wrong credentials");
        return Ok(user);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command)
    {
        try
        {
            var updated = await mediator.Send(command with { UserId = id });
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}