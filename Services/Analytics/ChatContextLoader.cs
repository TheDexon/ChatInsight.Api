using ChatInsight.Api.Data;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Telegram;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Analytics;

/// <summary>
/// Строит ChatAnalysisContext из сохранённого чата (по chatId).
/// Маппит сущности БД обратно в TelegramExport, чтобы переиспользовать
/// весь существующий аналитический конвейер без изменений.
/// </summary>
public class ChatContextLoader
{
    private readonly ChatInsightDbContext _db;

    public ChatContextLoader(ChatInsightDbContext db)
    {
        _db = db;
    }

    public async Task<ChatAnalysisContext?> LoadAsync(
        Guid chatId,
        CancellationToken ct = default)
    {
        var chat = await _db.Chats
            .Include(c => c.Messages)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == chatId, ct);

        if (chat is null)
            return null;

        var export = new TelegramExport
        {
            Name = chat.Name,
            Type = chat.Type,
            Messages = chat.Messages
                .Select(m => new TelegramMessage
                {
                    Id = m.TelegramId,
                    Type = m.Type,
                    Date = m.Date,
                    From = m.Author,
                    Text = m.Text   // уже плоский; extractor вернёт строку как есть
                })
                .ToList()
        };

        return ChatAnalysisContext.Create(export);
    }
}
