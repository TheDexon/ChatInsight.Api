using ChatInsight.Api.DTOs;
using ChatInsight.Api.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly TelegramParser _telegramParser;

    public ImportController(TelegramParser telegramParser)
    {
        _telegramParser = telegramParser;
    }

    [HttpPost("telegram")]
    public async Task<IActionResult> ImportTelegram(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не загружен.");

        await using var stream = file.OpenReadStream();

        var export = await _telegramParser.ParseAsync(stream);

        if (export == null)
            return BadRequest("Не удалось прочитать Telegram Export.");

        var participants = export.Messages
            .Where(m => !string.IsNullOrWhiteSpace(m.From))
            .Select(m => m.From!)
            .Distinct()
            .ToList();

        var dates = export.Messages
            .Select(m => m.Date)
            .OrderBy(d => d)
            .ToList();

        var result = new ImportResultDto
        {
            ChatName = export.Name,
            ChatType = export.Type,
            MessagesCount = export.Messages.Count,
            FirstMessageDate = dates.FirstOrDefault(),
            LastMessageDate = dates.LastOrDefault(),
            Participants = participants
        };

        return Ok(result);
    }
}