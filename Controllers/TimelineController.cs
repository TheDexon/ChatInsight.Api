using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/timeline")]
public class TimelineController : AnalysisControllerBase
{
    private readonly TimelineService _timeline;

    public TimelineController(
        TelegramParser parser,
        TimelineService service)
        : base(parser)
    {
        _timeline = service;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

        var context = ChatAnalysisContext.Create(export!);
        return Ok(_timeline.Analyze(context));
    }
}
