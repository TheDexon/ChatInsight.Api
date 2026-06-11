using ChatInsight.Api.Analysis.Ai;
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

    public ChatsController(
        ChatInsightDbContext db,
        ChatContextLoader loader,
        ReportService report,
        PdfReportService pdf,
        AiInsightCacheService aiCache)
    {
        _db = db;
        _loader = loader;
        _report = report;
        _pdf = pdf;
        _aiCache = aiCache;
    }

    /// <summary>Список сохранённых чатов.</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var chats = await _db.Chats
            .AsNoTracking()
            .OrderByDescending(c => c.ImportedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                c.MessageCount,
                c.ImportedAt
            })
            .ToListAsync();

        return Ok(chats);
    }

    /// <summary>Метаданные одного чата.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var chat = await _db.Chats
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                c.MessageCount,
                c.ImportedAt
            })
            .FirstOrDefaultAsync();

        return chat is null ? NotFound() : Ok(chat);
    }

    /// <summary>Полный отчёт (JSON) из БД по сохранённому чату.</summary>
    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id)
    {
        var context = await _loader.LoadAsync(id);
        if (context is null)
            return NotFound("Чат не найден.");

        return Ok(_report.Analyze(context));
    }

    /// <summary>
    /// Отчёт в PDF. По умолчанию AI-секция добавляется, только если инсайт уже
    /// в кэше (быстро). ai=true — посчитать инсайт при необходимости (дольше).
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
        if (ai)
        {
            // гарантированно с AI; если Ollama недоступна — PDF без AI, не падаем
            try
            {
                (insight, _) = await _aiCache.GetOrCreateAsync(
                    context, id, refresh: false, ct);
            }
            catch (OllamaUnavailableException)
            {
                insight = null;
            }
        }
        else
        {
            // только если уже посчитано раньше — модель не зовём
            insight = await _aiCache.GetCachedAsync(id, ct);
        }

        var bytes = _pdf.Build(context, insight);

        var safeName = string.IsNullOrWhiteSpace(context.Export.Name)
            ? "chat"
            : string.Join("_", context.Export.Name.Split(Path.GetInvalidFileNameChars()));

        return File(bytes, "application/pdf", $"report_{safeName}.pdf");
    }
}
