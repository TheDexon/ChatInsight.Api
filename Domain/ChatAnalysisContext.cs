using ChatInsight.Api.Models.Telegram;

namespace ChatInsight.Api.Domain;

public class ChatAnalysisContext
{
    public TelegramExport Export { get; init; } = null!;

    public List<TelegramMessage> Messages { get; init; } = [];

    public List<string> Participants { get; init; } = [];

    public DateTime FirstMessageDate { get; init; }

    public DateTime LastMessageDate { get; init; }

    public int TotalMessages { get; init; }

    public bool IsEmpty => Messages.Count == 0;

    public static ChatAnalysisContext Create(
        TelegramExport export)
    {
        var messages = export.Messages
            .Where(x =>
                x.Type == "message" &&
                !string.IsNullOrWhiteSpace(x.From))
            .OrderBy(x => x.Date)
            .ToList();

        return new ChatAnalysisContext
        {
            Export = export,

            Messages = messages,

            Participants = messages
                .Select(x => x.From!)
                .Distinct()
                .ToList(),

            // guard: не падаем на пустом чате
            FirstMessageDate =
                messages.Count > 0 ? messages[0].Date : default,

            LastMessageDate =
                messages.Count > 0 ? messages[^1].Date : default,

            TotalMessages = messages.Count
        };
    }
}
