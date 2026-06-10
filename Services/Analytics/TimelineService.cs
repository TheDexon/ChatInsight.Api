using ChatInsight.Api.Analysis.Timeline;
using ChatInsight.Api.Domain;

namespace ChatInsight.Api.Services.Analytics;

public class TimelineService
{
    public List<TimelineEvent> Analyze(ChatAnalysisContext context)
    {
        var events = new List<TimelineEvent>();

        // уже отфильтровано и отсортировано по дате
        var messages = context.Messages;

        if (messages.Count == 0)
            return events;

        // Начало общения
        events.Add(new TimelineEvent
        {
            Title = "Начало общения",
            Description = "Первое сообщение",
            Date = messages[0].Date
        });

        // Пик активности
        var mostActiveDay = messages
            .GroupBy(x => x.Date.Date)
            .OrderByDescending(g => g.Count())
            .First();

        events.Add(new TimelineEvent
        {
            Title = "Пик активности",
            Description = $"{mostActiveDay.Count()} сообщений",
            Date = mostActiveDay.Key
        });

        // Самая длинная пауза
        TimeSpan longestGap = TimeSpan.Zero;
        DateTime gapStart = messages[0].Date;

        for (int i = 1; i < messages.Count; i++)
        {
            var gap = messages[i].Date - messages[i - 1].Date;

            if (gap > longestGap)
            {
                longestGap = gap;
                gapStart = messages[i - 1].Date;
            }
        }

        events.Add(new TimelineEvent
        {
            Title = "Самая длинная пауза",
            Description =
                $"{longestGap.Days} дн. {longestGap.Hours} ч.",
            Date = gapStart
        });

        // Всплески активности
        var dailyActivity = messages
            .GroupBy(x => x.Date.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        var averageMessagesPerDay =
            dailyActivity.Average(x => x.Count);

        foreach (var day in dailyActivity)
        {
            if (day.Count >= averageMessagesPerDay * 2)
            {
                events.Add(new TimelineEvent
                {
                    Title = "Всплеск активности",
                    Description = $"{day.Count} сообщений",
                    Date = day.Date
                });
            }
        }

        return events
            .OrderBy(x => x.Date)
            .ToList();
    }
}
