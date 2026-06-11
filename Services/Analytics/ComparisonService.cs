using ChatInsight.Api.Analysis.Comparison;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Telegram;

namespace ChatInsight.Api.Services.Analytics;

/// <summary>
/// Сравнивает два периода общения («было → стало»).
/// Режет сообщения по дате, для каждого периода строит отдельный
/// ChatAnalysisContext и переиспользует существующие аналитические сервисы.
/// </summary>
public class ComparisonService
{
    private readonly StatisticsService _statistics;
    private readonly EmotionService _emotion;
    private readonly TopicService _topics;
    private readonly ResponseService _response;

    public ComparisonService(
        StatisticsService statistics,
        EmotionService emotion,
        TopicService topics,
        ResponseService response)
    {
        _statistics = statistics;
        _emotion = emotion;
        _topics = topics;
        _response = response;
    }

    public PeriodComparison Analyze(
        ChatAnalysisContext context,
        DateTime? splitDate = null)
    {
        var messages = context.Messages; // отфильтрованы и отсортированы

        if (messages.Count < 2)
            return new PeriodComparison
            {
                Summary = "Недостаточно сообщений для сравнения."
            };

        // Точка разреза: заданная дата или медиана (пополам)
        var split = splitDate ?? messages[messages.Count / 2].Date;

        var firstMsgs = messages.Where(m => m.Date < split).ToList();
        var secondMsgs = messages.Where(m => m.Date >= split).ToList();

        // Если заданная дата вне диапазона — откатываемся на «пополам»
        if (firstMsgs.Count == 0 || secondMsgs.Count == 0)
        {
            split = messages[messages.Count / 2].Date;
            firstMsgs = messages.Where(m => m.Date < split).ToList();
            secondMsgs = messages.Where(m => m.Date >= split).ToList();
        }

        var first = Summarize(SubContext(context, firstMsgs));
        var second = Summarize(SubContext(context, secondMsgs));

        var comparison = new PeriodComparison
        {
            First = first,
            Second = second,
            MessagesDelta = second.Messages - first.Messages,
            ToxicityDelta =
                Math.Round(second.ToxicityScore - first.ToxicityScore, 2),
            ResponseMinutesDelta =
                Math.Round(second.AvgResponseMinutes - first.AvgResponseMinutes, 2),
            NewTopics = second.TopTopics.Except(first.TopTopics).Take(8).ToList(),
            FadedTopics = first.TopTopics.Except(second.TopTopics).Take(8).ToList()
        };

        comparison.Summary = BuildSummary(comparison);
        return comparison;
    }

    private static ChatAnalysisContext SubContext(
        ChatAnalysisContext full,
        List<TelegramMessage> subset)
    {
        var export = new TelegramExport
        {
            Name = full.Export.Name,
            Type = full.Export.Type,
            Messages = subset
        };

        return ChatAnalysisContext.Create(export);
    }

    private PeriodSummary Summarize(ChatAnalysisContext ctx)
    {
        var stats = _statistics.Analyze(ctx);
        var emo = _emotion.Analyze(ctx);
        var topics = _topics.Analyze(ctx);
        var resp = _response.Analyze(ctx);

        var avgResp = resp.AverageResponseMinutes.Count > 0
            ? Math.Round(resp.AverageResponseMinutes.Values.Average(), 2)
            : 0;

        return new PeriodSummary
        {
            From = ctx.IsEmpty ? default : ctx.FirstMessageDate,
            To = ctx.IsEmpty ? default : ctx.LastMessageDate,
            Messages = stats.TotalMessages,
            AvgMessageLength = Math.Round(stats.AverageMessageLength, 1),
            PositiveMessages = emo.PositiveMessages,
            NegativeMessages = emo.NegativeMessages,
            ToxicityScore = emo.ToxicityScore,
            AvgResponseMinutes = avgResp,
            TopTopics = topics.Topics
                .Take(10)
                .Select(t => t.Name)
                .ToList()
        };
    }

    private static string BuildSummary(PeriodComparison c)
    {
        var parts = new List<string>
        {
            c.MessagesDelta >= 0
                ? $"Активность выросла на {c.MessagesDelta} сообщений."
                : $"Активность снизилась на {Math.Abs(c.MessagesDelta)} сообщений."
        };

        if (c.ToxicityDelta > 0)
            parts.Add($"Токсичность выросла на {c.ToxicityDelta}%.");
        else if (c.ToxicityDelta < 0)
            parts.Add($"Токсичность снизилась на {Math.Abs(c.ToxicityDelta)}%.");

        if (c.ResponseMinutesDelta > 0)
            parts.Add($"Отвечать стали медленнее на {c.ResponseMinutesDelta} мин.");
        else if (c.ResponseMinutesDelta < 0)
            parts.Add($"Отвечать стали быстрее на {Math.Abs(c.ResponseMinutesDelta)} мин.");

        if (c.NewTopics.Count > 0)
            parts.Add($"Новые темы: {string.Join(", ", c.NewTopics.Take(5))}.");

        if (c.FadedTopics.Count > 0)
            parts.Add($"Ушли темы: {string.Join(", ", c.FadedTopics.Take(5))}.");

        return string.Join(" ", parts);
    }
}
