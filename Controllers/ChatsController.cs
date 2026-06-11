using ChatInsight.Api.Analysis.Ai;
using ChatInsight.Api.Analysis.Personality;
using ChatInsight.Api.Data;
using ChatInsight.Api.Reports;
using ChatInsight.Api.Services.Ai;
using ChatInsight.Api.Services.Analytics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly ChatInsightDbContext _db;
    private readonly ChatContextLoader _loader;
    private readonly ReportService _report;
    private readonly PdfReportService _pdf;
    private readonly AiInsightCacheService _aiCache;
    private readonly PersonalityCacheService _personaCache;

    public ChatsController(
        ChatInsightDbContext db,
        ChatContextLoader loader,
        ReportService report,
        PdfReportService pdf,
        AiInsightCacheService aiCache,
        PersonalityCacheService personaCache)
    {
        _db = db;
        _loader = loader;
        _report = report;
        _pdf = pdf;
        _aiCache = aiCache;
        _personaCache = personaCache;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var chats = await _db.Chats
            .AsNoTracking()
            .OrderByDescending(c => c.ImportedAt)
            .Select(c => new { c.Id, c.Name, c.Type, c.MessageCount, c.ImportedAt })
            .ToListAsync();

        return Ok(chats);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var chat = await _db.Chats
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.Id, c.Name, c.Type, c.MessageCount, c.ImportedAt })
            .FirstOrDefaultAsync();

        return chat is null ? NotFound() : Ok(chat);
    }

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id)
    {
        var context = await _loader.LoadAsync(id);
        if (context is null)
            return NotFound("Чат не найден.");

        return Ok(_report.Analyze(context));
    }

    /// <summary>
    /// Отчёт в PDF. По умолчанию AI-секции добавляются только из кэша (быстро).
    /// ai=true — посчитать AI-анализ и портреты при необходимости (дольше).
    /// </summary>
    [HttpGet("{id:guid}/report.pdf")]
    public async Task<IActionResult> ReportPdf(
        Guid id,
        [FromQuery] bool ai = false,
        CancellationToken ct = default)
    {
        var context = await _loader.LoadAsync(id, ct);
        if (context is null)
            return NotFound("Чат не найден.");

        AiInsight? insight;
        List<PersonalityProfile> personas = [];

        if (ai)
        {
            // гарантированно с AI; если Ollama недоступна — PDF без AI-частей
            try
            {
                (insight, _) = await _aiCache.GetOrCreateAsync(context, id, false, ct);
                (personas, _) = await _personaCache.GetOrCreateAsync(context, id, false, ct);
            }
            catch (OllamaUnavailableException)
            {
                insight = await _aiCache.GetCachedAsync(id, ct);
                personas = await _personaCache.GetCachedAsync(id, ct);
            }
        }
        else
        {
            // только то, что уже посчитано — модель не зовём
            insight = await _aiCache.GetCachedAsync(id, ct);
            personas = await _personaCache.GetCachedAsync(id, ct);
        }

        var bytes = _pdf.Build(
            context, insight, personas.Count > 0 ? personas : null);

        var safeName = string.IsNullOrWhiteSpace(context.Export.Name)
            ? "chat"
            : string.Join("_", context.Export.Name.Split(Path.GetInvalidFileNameChars()));

        return File(bytes, "application/pdf", $"report_{safeName}.pdf");
    }
}
