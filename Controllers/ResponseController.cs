using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/response")]
public class ResponseController : ControllerBase
{
    private readonly TelegramParser _parser;
    private readonly ResponseService _response;

    public ResponseController(
        TelegramParser parser,
        ResponseService response)
    {
        _parser = parser;
        _response = response;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        await using var stream =
            file.OpenReadStream();

        var export =
            await _parser.ParseAsync(stream);

        if (export == null)
            return BadRequest();

        return Ok(
            _response.Analyze(export));
    }
}