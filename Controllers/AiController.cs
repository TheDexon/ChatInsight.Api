using ChatInsight.Api.Services.Ai;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/chats")]
public class AiController : ControllerBase
{
    private readonly ChatContextLoader _loader;
    private readonly AiInsightService _ai;

    public AiController(
        ChatContextLoader loader,
        AiInsightService ai)
    {
        _loader = loader;
        _ai = ai;
    }

    /// <summary>AI-выводы по сохранённому чату (через локальную модель Ollama).</summary>
    [HttpGet("{id:guid}/insights")]
    public async Task<IActionResult> Insights(Guid id, CancellationToken ct)
    {
        var context = await _loader.LoadAsync(id, ct);
        if (context is null)
            return NotFound("Чат не найден.");

        if (context.IsEmpty)
            return BadRequest("В чате нет сообщений для анализа.");

        try
        {
            var insight = await _ai.AnalyzeAsync(context, ct);
            return Ok(insight);
        }
        catch (OllamaUnavailableException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}
