using ChatInsight.Api.Data;
using ChatInsight.Api.Reports;
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

    public ChatsController(
        ChatInsightDbContext db,
        ChatContextLoader loader,
        ReportService report,
        PdfReportService pdf)
    {
        _db = db;
        _loader = loader;
        _report = report;
        _pdf = pdf;
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

    /// <summary>Отчёт в PDF из БД по сохранённому чату.</summary>
    [HttpGet("{id:guid}/report.pdf")]
    public async Task<IActionResult> ReportPdf(Guid id)
    {
        var context = await _loader.LoadAsync(id);
        if (context is null)
            return NotFound("Чат не найден.");

        var bytes = _pdf.Build(context);

        var safeName = string.IsNullOrWhiteSpace(context.Export.Name)
            ? "chat"
            : string.Join("_", context.Export.Name.Split(Path.GetInvalidFileNameChars()));

        return File(bytes, "application/pdf", $"report_{safeName}.pdf");
    }
}
