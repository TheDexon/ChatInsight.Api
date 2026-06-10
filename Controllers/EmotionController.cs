using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/emotion")]
public class EmotionController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly EmotionService _service;

    public EmotionController(
        TelegramParser parser,
        EmotionService service)
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

        return Ok(
            _service.Analyze(context));
    }
}