using ChatInsight.Api.Analysis.Initiative;
using ChatInsight.Api.Models.Telegram;

namespace ChatInsight.Api.Services.Analytics;

public class InitiativeService
{
    public InitiativeStatistics Analyze(
        TelegramExport export)
    {
        var result =
            new InitiativeStatistics();

        var messages = export.Messages
            .Where(x =>
                x.Type == "message" &&
                !string.IsNullOrWhiteSpace(x.From))
            .OrderBy(x => x.Date)
            .ToList();

        foreach (var author in
                 messages.Select(x => x.From!)
                     .Distinct())
        {
            result.ConversationStarts[author] = 0;
            result.DailyStarts[author] = 0;
            result.LongPauseStarts[author] = 0;
        }

        // Первое сообщение дня

        var dailyGroups = messages
            .GroupBy(x => x.Date.Date);

        foreach (var day in dailyGroups)
        {
            var first =
                day.OrderBy(x => x.Date)
                    .First();

            result.DailyStarts[first.From!] += 1;
        }

        // После паузы

        for (int i = 1; i < messages.Count; i++)
        {
            var gap =
                messages[i].Date -
                messages[i - 1].Date;

            if (gap.TotalHours >= 8)
            {
                var author =
                    messages[i].From!;

                result.ConversationStarts[author] += 1;

                result.LongPauseStarts[author] += 1;
            }
        }

        return result;
    }
}