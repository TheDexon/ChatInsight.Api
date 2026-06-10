using ChatInsight.Api.Analysis.Emotion;
using ChatInsight.Api.Configuration;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;
using Microsoft.Extensions.Options;

namespace ChatInsight.Api.Services.Analytics;

public class EmotionService
{
    private readonly TelegramTextExtractor _extractor;

    private readonly string[] _positiveWords;
    private readonly string[] _negativeWords;
    private readonly string[] _profanityWords;

    public EmotionService(
        TelegramTextExtractor extractor,
        IOptions<EmotionAnalysisOptions> options)
    {
        _extractor = extractor;

        var o = options.Value;
        _positiveWords = Normalize(o.PositiveWords);
        _negativeWords = Normalize(o.NegativeWords);
        _profanityWords = Normalize(o.ProfanityWords);
    }

    private static string[] Normalize(IEnumerable<string> words) =>
        words
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToArray();

    public EmotionStatistics Analyze(
        ChatAnalysisContext context)
    {
        var result = new EmotionStatistics();

        foreach (var message in context.Messages)
        {
            var text =
                _extractor.Extract(message.Text)
                .ToLowerInvariant();

            if (_positiveWords.Any(text.Contains))
                result.PositiveMessages++;

            if (_negativeWords.Any(text.Contains))
                result.NegativeMessages++;

            if (_profanityWords.Any(text.Contains))
                result.ProfanityMessages++;
        }

        result.ToxicityScore =
            context.TotalMessages == 0
                ? 0
                : Math.Round(
                    result.ProfanityMessages * 100.0 /
                    context.TotalMessages,
                    2);

        return result;
    }
}
