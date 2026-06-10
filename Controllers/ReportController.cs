using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/report")]
public class ReportController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly ReportService _service;

    public ReportController(
        TelegramParser parser,
        ReportService service)
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
            _service.Analyze(
                export,
                context));
    }
}