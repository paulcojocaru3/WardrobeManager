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

    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Process([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name)
    {
        var result = await mediator.Send(new ProcessClothingCommand(File, UserId, Name));
        return Ok(result);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddClothingCommand command)
    {
        var item = await mediator.Send(command);
        return Ok(item);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile File, [FromForm] Guid UserId, [FromForm] string Name)
    {
        // Keep for backward compatibility or direct upload
        var processed = await mediator.Send(new ProcessClothingCommand(File, UserId, Name));
        var item = await mediator.Send(new AddClothingCommand(
            UserId, 
            Name, 
            processed.Type, 
            processed.Color, 
            processed.Gender, 
            processed.Season, 
            processed.Usage, 
            processed.ProcessedImageB64, 
            processed.Embedding));
            
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteClothingCommand(id));
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClothingCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await mediator.Send(command);
        if (!result) return NotFound();
        return Ok();
    }
}
