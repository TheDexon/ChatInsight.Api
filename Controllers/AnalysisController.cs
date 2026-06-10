using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;
using ChatInsight.Api.Domain;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly StatisticsService _statistics;

    public AnalysisController(
        TelegramParser parser,
        StatisticsService statistics)
    {
        _parser = parser;
        _statistics = statistics;
    }

    [HttpPost("basic")]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не загружен.");

        await using var stream = file.OpenReadStream();

        var export = await _parser.ParseAsync(stream);

        if (export == null)
            return BadRequest("Не удалось прочитать Telegram Export.");

        var context = ChatAnalysisContext.Create(export);

        var result = _statistics.Analyze(context);

        return Ok(result);
    }
}