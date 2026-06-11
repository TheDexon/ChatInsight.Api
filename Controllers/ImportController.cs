using ChatInsight.Api.Parsers;
using ChatInsight.Api.Services.Import;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : AnalysisControllerBase
{
    private readonly ChatImportService _import;

    public ImportController(
        TelegramParser telegramParser,
        ChatImportService import)
        : base(telegramParser)
    {
        _import = import;
    }

    [HttpPost("telegram")]
    public async Task<IActionResult> ImportTelegram(
        IFormFile file,
        CancellationToken ct = default)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

        var result = await _import.ImportAsync(export!, ct);

        return Ok(result);
    }
}
