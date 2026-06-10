using System.Text.Json;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Models.Telegram;
using ChatInsight.Api.Services.Text;

namespace ChatInsight.Api.Services.Import;

/// <summary>Парсит уже прочитанный экспорт в сущности и сохраняет в БД.</summary>
public class ChatImportService
{
    private readonly ChatInsightDbContext _db;
    private readonly TelegramTextExtractor _extractor;

    public ChatImportService(
        ChatInsightDbContext db,
        TelegramTextExtractor extractor)
    {
        _db = db;
        _extractor = extractor;
    }

    public async Task<Chat> ImportAsync(
        TelegramExport export,
        CancellationToken ct = default)
    {
        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = export.Name,
            Type = export.Type,
            ImportedAt = DateTime.UtcNow,
            MessageCount = export.Messages.Count
        };

        foreach (var m in export.Messages)
        {
            chat.Messages.Add(new Message
            {
                ChatId = chat.Id,
                TelegramId = m.Id,
                Type = m.Type,
                Date = m.Date,                       // Unspecified -> timestamp without tz
                Author = m.From,
                Text = _extractor.Extract(m.Text),   // плоский текст
                RawTextJson = SerializeRaw(m.Text)   // оригинал на будущее
            });
        }

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync(ct);

        return chat;
    }

    private static string? SerializeRaw(object? text)
    {
        if (text is null) return null;
        if (text is JsonElement el) return el.GetRawText();
        return JsonSerializer.Serialize(text);
    }
}
