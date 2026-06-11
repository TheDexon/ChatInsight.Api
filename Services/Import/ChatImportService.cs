using System.Text.Json;
using ChatInsight.Api.Data;
using ChatInsight.Api.DTOs;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Models.Telegram;
using ChatInsight.Api.Services.Text;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Import;

/// <summary>
/// Сохраняет экспорт в БД. Если чат с таким SourceId уже есть —
/// ДОПОЛНЯЕТ его новыми сообщениями (по TelegramId), а не плодит дубль.
/// При добавлении новых сообщений сбрасывает кэш AI-инсайтов.
/// </summary>
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

    public async Task<ImportResultDto> ImportAsync(
        TelegramExport export,
        CancellationToken ct = default)
    {
        // Ищем существующий чат по SourceId (если он задан)
        Chat? chat = export.Id != 0
            ? await _db.Chats
                .FirstOrDefaultAsync(c => c.SourceId == export.Id, ct)
            : null;

        bool isNew = chat is null;
        int added;

        if (chat is null)
        {
            chat = new Chat
            {
                Id = Guid.NewGuid(),
                SourceId = export.Id,
                Name = export.Name,
                Type = export.Type,
                ImportedAt = DateTime.UtcNow,
                MessageCount = 0
            };
            _db.Chats.Add(chat);

            added = AppendMessages(chat, export.Messages, knownIds: null);
        }
        else
        {
            // какие TelegramId уже есть — чтобы добавить только новые
            var knownIds = await _db.Messages
                .Where(m => m.ChatId == chat.Id)
                .Select(m => m.TelegramId)
                .ToHashSetAsync(ct);

            added = AppendMessages(chat, export.Messages, knownIds);

            chat.Name = export.Name;   // имя могло измениться
            chat.UpdatedAt = DateTime.UtcNow;
        }

        chat.MessageCount += added;

        // Если что-то добавили в существующий чат — кэш инсайтов устарел
        if (!isNew && added > 0)
        {
            var stale = await _db.Insights
                .Where(x => x.ChatId == chat.Id)
                .ToListAsync(ct);

            if (stale.Count > 0)
                _db.Insights.RemoveRange(stale);
        }

        await _db.SaveChangesAsync(ct);

        var dates = export.Messages
            .Select(m => m.Date)
            .OrderBy(d => d)
            .ToList();

        return new ImportResultDto
        {
            ChatId = chat.Id,
            ChatName = chat.Name,
            ChatType = chat.Type,
            MessagesCount = chat.MessageCount,
            NewMessages = added,
            IsNewChat = isNew,
            FirstMessageDate = dates.FirstOrDefault(),
            LastMessageDate = dates.LastOrDefault(),
            Participants = export.Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.From))
                .Select(m => m.From!)
                .Distinct()
                .ToList()
        };
    }

    /// <summary>Добавляет в чат сообщения, которых ещё нет (по TelegramId). Возвращает кол-во добавленных.</summary>
    private int AppendMessages(
        Chat chat,
        List<TelegramMessage> messages,
        HashSet<long>? knownIds)
    {
        int added = 0;

        foreach (var m in messages)
        {
            if (knownIds is not null && knownIds.Contains(m.Id))
                continue;

            chat.Messages.Add(new Message
            {
                ChatId = chat.Id,
                TelegramId = m.Id,
                Type = m.Type,
                Date = m.Date,
                Author = m.From,
                Text = _extractor.Extract(m.Text),
                RawTextJson = SerializeRaw(m.Text)
            });

            knownIds?.Add(m.Id);
            added++;
        }

        return added;
    }

    private static string? SerializeRaw(object? text)
    {
        if (text is null) return null;
        if (text is JsonElement el) return el.GetRawText();
        return JsonSerializer.Serialize(text);
    }
}
