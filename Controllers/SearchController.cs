using ChatInsight.Api.Services.Ai;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/chats")]
public class SearchController : ControllerBase
{
    private readonly SemanticSearchService _search;

    public SearchController(SemanticSearchService search)
    {
        _search = search;
    }

    /// <summary>
    /// Семантический поиск по сообщениям чата. Требует построенных эмбеддингов
    /// (POST /embeddings/async). Если их нет — embeddingsReady=false.
    /// </summary>
    [HttpGet("{id:guid}/search")]
    public async Task<IActionResult> Search(
        Guid id,
        [FromQuery] string q,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Пустой запрос.");

        var count = await _search.CountAsync(id, ct);
        if (count == 0)
            return Ok(new { embeddingsReady = false, hits = Array.Empty<object>() });

        try
        {
            var hits = await _search.SearchAsync(id, q, Math.Clamp(limit, 1, 50), ct);
            return Ok(new { embeddingsReady = true, hits });
        }
        catch (OllamaUnavailableException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}
