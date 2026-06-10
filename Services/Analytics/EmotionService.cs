using ChatInsight.Api.Analysis.Emotion;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;

namespace ChatInsight.Api.Services.Analytics;

public class EmotionService
{
    private readonly TelegramTextExtractor _extractor;

    private readonly HashSet<string> PositiveWords =
    [
        "люблю",
        "кайф",
        "супер",
        "круто",
        "класс",
        "хорошо",
        "отлично",
        "рад"
    ];

    private readonly HashSet<string> NegativeWords =
    [
        "ненавижу",
        "плохо",
        "ужас",
        "говно",
        "обидно",
        "печально"
    ];

    private readonly HashSet<string> ProfanityWords =
    [
        "бля",
        "блять",
        "пиздец",
        "ебать",
        "нахуй",
        "сука",
        "хуй"
    ];

    public EmotionService(
        TelegramTextExtractor extractor)
    {
        _extractor = extractor;
    }

    public EmotionStatistics Analyze(
        ChatAnalysisContext context)
    {
        var result =
            new EmotionStatistics();

        foreach (var message in context.Messages)
        {
            var text =
                _extractor.Extract(message.Text)
                .ToLower();

            if (PositiveWords.Any(text.Contains))
                result.PositiveMessages++;

            if (NegativeWords.Any(text.Contains))
                result.NegativeMessages++;

            if (ProfanityWords.Any(text.Contains))
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