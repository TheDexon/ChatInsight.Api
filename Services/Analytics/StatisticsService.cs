using ChatInsight.Api.Analysis.Statistics;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Telegram;
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
        var messages = context.Messages
            .Where(x => x.Type == "message")
            .ToList();

        var stats = new ChatStatistics();

        stats.TotalMessages = messages.Count;

        stats.MessagesByAuthor = messages
            .Where(x => !string.IsNullOrWhiteSpace(x.From))
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
        .Select(x =>
            _extractor
                .Extract(x.Text)
                .Length)
        .ToList();

        stats.AverageMessageLength =
            textLengths.Count == 0
                ? 0
                : textLengths.Average();

        return stats;
    }
}