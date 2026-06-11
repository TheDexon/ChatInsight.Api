using ChatInsight.Api.Services.Ai;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/chats")]
public class PersonalityController : ControllerBase
{
    private readonly ChatContextLoader _loader;
    private readonly PersonalityCacheService _cache;

    public PersonalityController(
        ChatContextLoader loader,
        PersonalityCacheService cache)
    {
        _loader = loader;
        _cache = cache;
    }

    /// <summary>
    /// AI-портреты участников чата (по их сообщениям). Берёт из кэша;
    /// если нет — считает через Ollama. refresh=true — пересчёт.
    /// </summary>
    [HttpGet("{id:guid}/personality")]
    public async Task<IActionResult> Personality(
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
            var (profiles, fromCache) =
                await _cache.GetOrCreateAsync(context, id, refresh, ct);

            Response.Headers["X-Insight-Cache"] = fromCache ? "hit" : "miss";
            return Ok(profiles);
        }
        catch (OllamaUnavailableException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}
