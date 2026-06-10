using ChatInsight.Api.Analysis.Report;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Telegram;

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

    public ReportStatistics Analyze(
        TelegramExport export,
        ChatAnalysisContext context)
    {
        return new ReportStatistics
        {
            Statistics =
                _statistics.Analyze(context),

            Timeline =
                _timeline.Analyze(export),

            Response =
                _response.Analyze(export),

            Initiative =
                _initiative.Analyze(export),

            Topics =
                _topics.Analyze(context),

            Emotion =
                _emotion.Analyze(context),

            Summary =
                $"Сообщений: {context.TotalMessages}"
        };
    }
}