using ChatInsight.Api.Analysis.Initiative;
using ChatInsight.Api.Domain;

namespace ChatInsight.Api.Services.Analytics;

public class InitiativeService
{
    public InitiativeStatistics Analyze(ChatAnalysisContext context)
    {
        var result = new InitiativeStatistics();

        var messages = context.Messages;

        foreach (var author in
                 messages.Select(x => x.From!).Distinct())
        {
            result.ConversationStarts[author] = 0;
            result.DailyStarts[author] = 0;
            result.LongPauseStarts[author] = 0;
        }

        // Первое сообщение дня
        foreach (var day in messages.GroupBy(x => x.Date.Date))
        {
            var first = day.OrderBy(x => x.Date).First();
            result.DailyStarts[first.From!] += 1;
        }

        // После паузы >= 8 часов
        for (int i = 1; i < messages.Count; i++)
        {
            var gap = messages[i].Date - messages[i - 1].Date;

            if (gap.TotalHours >= 8)
            {
                var author = messages[i].From!;
                result.ConversationStarts[author] += 1;
                result.LongPauseStarts[author] += 1;
            }
        }

        return result;
    }
}
