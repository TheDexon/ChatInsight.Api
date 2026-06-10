using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/relationship")]
public class RelationshipController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly RelationshipService _service;

    public RelationshipController(
        TelegramParser parser,
        RelationshipService service)
    {
        _parser = parser;
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(
        IFormFile file)
    {
        await using var stream =
            file.OpenReadStream();

        var export =
            await _parser.ParseAsync(stream);

        if (export == null)
            return BadRequest();

        return Ok(
            _service.Analyze(export));
    }
}