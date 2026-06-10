using ChatInsight.Api.DTOs;
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
    public async Task<IActionResult> ImportTelegram(IFormFile file)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

        var chat = await _import.ImportAsync(export!);

        var validated = export!;

        var participants = validated.Messages
            .Where(m => !string.IsNullOrWhiteSpace(m.From))
            .Select(m => m.From!)
            .Distinct()
            .ToList();

        var dates = validated.Messages
            .Select(m => m.Date)
            .OrderBy(d => d)
            .ToList();

        var result = new ImportResultDto
        {
            ChatId = chat.Id,
            ChatName = validated.Name,
            ChatType = validated.Type,
            MessagesCount = validated.Messages.Count,
            FirstMessageDate = dates.FirstOrDefault(),
            LastMessageDate = dates.LastOrDefault(),
            Participants = participants
        };

        return Ok(result);
    }
}
