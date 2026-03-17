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
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name)
    {
        // Explicitly reading from form fields to ensure maximum compatibility
        var item = await mediator.Send(new UploadClothingCommand(File, UserId, Name));
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteClothingCommand(id));
        return Ok();
    }
}
