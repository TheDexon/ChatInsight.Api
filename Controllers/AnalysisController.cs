using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : AnalysisControllerBase
{
    private readonly StatisticsService _statistics;

    public AnalysisController(
        TelegramParser parser,
        StatisticsService statistics)
        : base(parser)
    {
        _statistics = statistics;
    }

    [HttpPost("basic")]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

        var context = ChatAnalysisContext.Create(export!);
        return Ok(_statistics.Analyze(context));
    }
}
