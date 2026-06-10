using System.Text.Json;
using ChatInsight.Api.Models.Telegram;
using ChatInsight.Api.Parsers;
using Microsoft.AspNetCore.Mvc;

namespace ChatInsight.Api.Controllers;

/// <summary>
/// База для всех контроллеров анализа. Держит парсер и единую
/// точку чтения + валидации загруженного Telegram Export,
/// чтобы каждый контроллер не дублировал проверки файла.
/// </summary>
public abstract class AnalysisControllerBase : ControllerBase
{
    protected readonly TelegramParser Parser;

    protected AnalysisControllerBase(TelegramParser parser)
    {
        Parser = parser;
    }

    /// <returns>
    /// (Export, null) при успехе, либо (null, Error) с готовым BadRequest.
    /// </returns>
    protected async Task<(TelegramExport? Export, IActionResult? Error)>
        ReadExportAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return (null, BadRequest("Файл не загружен."));

        TelegramExport? export;

        try
        {
            await using var stream = file.OpenReadStream();
            export = await Parser.ParseAsync(stream);
        }
        catch (JsonException)
        {
            return (null, BadRequest(
                "Файл не является корректным JSON Telegram Export."));
        }

        if (export is null)
            return (null, BadRequest(
                "Не удалось прочитать Telegram Export."));

        if (export.Messages.Count == 0)
            return (null, BadRequest(
                "В экспорте нет сообщений."));

        return (export, null);
    }
}
