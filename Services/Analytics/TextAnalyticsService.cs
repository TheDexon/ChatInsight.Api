using System.Text.RegularExpressions;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;
using ChatInsight.Api.Analysis.Text;



namespace ChatInsight.Api.Services.Analytics;

public class TextAnalyticsService
{
    private readonly TelegramTextExtractor _extractor;
    private readonly TextCleaner _cleaner;

    public TextStatistics Analyze(
        ChatAnalysisContext context)
    {
        var texts = context.Messages
            .Select(x => _extractor.Extract(x.Text))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var allText = string.Join(" ", texts);

        var words = _cleaner.ExtractWords(allText);

        var topWords = words
            .GroupBy(x => x)
            .OrderByDescending(x => x.Count())
            .Take(20)
            .Select(x => $"{x.Key} ({x.Count()})")
            .ToList();

        return new TextStatistics
        {
            TopWords = topWords,
            TotalCharacters = allText.Length,
            TotalWords = words.Count,
            AverageWordsPerMessage =
                texts.Count == 0
                    ? 0
                    : (double)words.Count / texts.Count
        };
    }

            public TextAnalyticsService(
                TelegramTextExtractor extractor,
                TextCleaner cleaner)
            {
                _extractor = extractor;
                _cleaner = cleaner;
            }
}