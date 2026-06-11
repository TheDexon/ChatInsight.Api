using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/chats")]
public class ComparisonController : ControllerBase
{
    private readonly ChatContextLoader _loader;
    private readonly ComparisonService _comparison;

    public ComparisonController(
        ChatContextLoader loader,
        ComparisonService comparison)
    {
        _loader = loader;
        _comparison = comparison;
    }

    /// <summary>
    /// Сравнение двух периодов общения. По умолчанию режет пополам;
    /// splitDate (ISO) — резать в конкретной точке («до/после события»).
    /// </summary>
    [HttpGet("{id:guid}/compare")]
    public async Task<IActionResult> Compare(
        Guid id,
        [FromQuery] DateTime? splitDate,
        CancellationToken ct = default)
    {
        var context = await _loader.LoadAsync(id, ct);
        if (context is null)
            return NotFound("Чат не найден.");

        if (context.Messages.Count < 2)
            return BadRequest("Недостаточно сообщений для сравнения.");

        return Ok(_comparison.Analyze(context, splitDate));
    }
}
