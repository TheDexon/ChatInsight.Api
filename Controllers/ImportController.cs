using ChatInsight.Api.DTOs;
using ChatInsight.Api.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : AnalysisControllerBase
{
    public ImportController(TelegramParser telegramParser)
        : base(telegramParser)
    {
    }

    [HttpPost("telegram")]
    public async Task<IActionResult> ImportTelegram(IFormFile file)
    {
        var (export, error) = await ReadExportAsync(file);
        if (error is not null) return error;

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
