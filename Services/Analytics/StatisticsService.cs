using ChatInsight.Api.Analysis.Statistics;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;

namespace ChatInsight.Api.Services.Analytics;

public class StatisticsService
{
    private readonly TelegramTextExtractor _extractor;

    public StatisticsService(
        TelegramTextExtractor extractor)
    {
        _extractor = extractor;
    }

    public ChatStatistics Analyze(ChatAnalysisContext context)
    {
        // context.Messages уже отфильтрованы (type=message, from != null)
        var messages = context.Messages;

        var stats = new ChatStatistics
        {
            TotalMessages = messages.Count
        };

        stats.MessagesByAuthor = messages
            .GroupBy(x => x.From!)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        stats.MessagesByHour = messages
            .GroupBy(x => x.Date.Hour)
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        stats.MessagesByDay = messages
            .GroupBy(x => DateOnly.FromDateTime(x.Date))
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        stats.MostActiveHour = stats.MessagesByHour
            .OrderByDescending(x => x.Value)
            .FirstOrDefault()
            .Key;

        var textLengths = messages
            .Select(x => _extractor.Extract(x.Text).Length)
            .ToList();

        stats.AverageMessageLength =
            textLengths.Count == 0
                ? 0
                : textLengths.Average();

        return stats;
    }
}
