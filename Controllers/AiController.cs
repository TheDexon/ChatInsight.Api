using ChatInsight.Api.Services.Ai;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/chats")]
public class AiController : ControllerBase
{
    private readonly ChatContextLoader _loader;
    private readonly AiInsightCacheService _cache;

    public AiController(
        ChatContextLoader loader,
        AiInsightCacheService cache)
    {
        _loader = loader;
        _cache = cache;
    }

    /// <summary>
    /// AI-выводы по сохранённому чату. Берёт из кэша (БД); если нет —
    /// считает через Ollama и сохраняет. refresh=true — пересчитать заново.
    /// </summary>
    [HttpGet("{id:guid}/insights")]
    public async Task<IActionResult> Insights(
        Guid id,
        [FromQuery] bool refresh = false,
        CancellationToken ct = default)
    {
        var context = await _loader.LoadAsync(id, ct);
        if (context is null)
            return NotFound("Чат не найден.");

        if (context.IsEmpty)
            return BadRequest("В чате нет сообщений для анализа.");

        try
        {
            var (insight, fromCache) =
                await _cache.GetOrCreateAsync(context, id, refresh, ct);

            // подсказка клиенту, откуда результат
            Response.Headers["X-Insight-Cache"] =
                fromCache ? "hit" : "miss";

            return Ok(insight);
        }
        catch (OllamaUnavailableException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}
