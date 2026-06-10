using ChatInsight.Api.Analysis.Report;
using ChatInsight.Api.Domain;

namespace ChatInsight.Api.Services.Analytics;

public class ReportService
{
    private readonly StatisticsService _statistics;
    private readonly TimelineService _timeline;
    private readonly ResponseService _response;
    private readonly InitiativeService _initiative;
    private readonly TopicService _topics;
    private readonly EmotionService _emotion;

    public ReportService(
        StatisticsService statistics,
        TimelineService timeline,
        ResponseService response,
        InitiativeService initiative,
        TopicService topics,
        EmotionService emotion)
    {
        _statistics = statistics;
        _timeline = timeline;
        _response = response;
        _initiative = initiative;
        _topics = topics;
        _emotion = emotion;
    }

    // Теперь только context — больше не таскаем export отдельно
    public ReportStatistics Analyze(ChatAnalysisContext context)
    {
        return new ReportStatistics
        {
            Statistics = _statistics.Analyze(context),
            Timeline = _timeline.Analyze(context),
            Response = _response.Analyze(context),
            Initiative = _initiative.Analyze(context),
            Topics = _topics.Analyze(context),
            Emotion = _emotion.Analyze(context),
            Summary = $"Сообщений: {context.TotalMessages}"
        };
    }
}
