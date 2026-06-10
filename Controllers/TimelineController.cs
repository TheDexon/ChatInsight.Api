using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/timeline")]
public class TimelineController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly TimelineService _timeline;

    public TimelineController(
        TelegramParser parser,
        TimelineService timeline)
    {
        _parser = parser;
        _timeline = timeline;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var export = await _parser.ParseAsync(stream);

        if (export == null)
            return BadRequest();

        return Ok(_timeline.Analyze(export));
    }
}