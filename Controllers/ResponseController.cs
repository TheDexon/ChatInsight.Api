using ChatInsight.Api.Domain;
using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/response")]
public class ResponseController : AnalysisControllerBase
{
    private readonly ResponseService _response;

    public ResponseController(
        TelegramParser parser,
        ResponseService service)
        : base(parser)
    {
        _response = service;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

        var context = ChatAnalysisContext.Create(export!);
        return Ok(_response.Analyze(context));
    }
}
