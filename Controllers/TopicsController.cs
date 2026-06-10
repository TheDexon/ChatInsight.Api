using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/topics")]
public class TopicsController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly TopicService _service;

    public TopicsController(
        TelegramParser parser,
        TopicService service)
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

        var context =
            ChatAnalysisContext.Create(export);

        var result =
            _service.Analyze(context);

        return Ok(result);
    }
}