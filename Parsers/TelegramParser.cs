using System.Text.Json;
using ChatInsight.Api.Models.Telegram;

namespace ChatInsight.Api.Parsers;

public class TelegramParser
{
    public async Task<TelegramExport?> ParseAsync(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<TelegramExport>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }
}