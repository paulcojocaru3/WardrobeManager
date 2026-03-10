using MediatR;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.Application.Clothing.Commands;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClothingController(IMediator mediator) : ControllerBase
{
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var items = await mediator.Send(new GetClothingItemsQuery(userId));
        return Ok(items);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadClothingCommand command)
    {
        try
        {
            var item = await mediator.Send(command);
            return Ok(item);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await mediator.Send(new DeleteClothingCommand(id));
        return success ? Ok() : NotFound();
    }
}