using ChatInsight.Api.Analysis.Topics;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;

namespace ChatInsight.Api.Services.Analytics;

public class TopicService
{
    private readonly TelegramTextExtractor _extractor;
    private readonly TextCleaner _cleaner;

    public TopicService(
        TelegramTextExtractor extractor,
        TextCleaner cleaner)
    {
        _extractor = extractor;
        _cleaner = cleaner;
    }

    public TopicStatistics Analyze(
        ChatAnalysisContext context)
    {
        var allText = string.Join(
            " ",
            context.Messages
                .Select(x =>
                    _extractor.Extract(x.Text)));

        var words =
            _cleaner.ExtractWords(allText);

        var topics = words
            .GroupBy(x => x)
            .OrderByDescending(x => x.Count())
            .Take(30)
            .Select(x => new TopicItem
            {
                Name = x.Key,
                Count = x.Count()
            })
            .ToList();

        return new TopicStatistics
        {
            Topics = topics
        };
    }
}