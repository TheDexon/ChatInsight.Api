using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/text")]
public class TextController : AnalysisControllerBase
{
    private readonly TextAnalyticsService _service;

    public TextController(
        TelegramParser parser,
        TextAnalyticsService service)
        : base(parser)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

        var context = ChatAnalysisContext.Create(export!);
        return Ok(_service.Analyze(context));
    }
}
