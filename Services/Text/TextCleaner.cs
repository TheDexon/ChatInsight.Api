using System.Text.RegularExpressions;

namespace ChatInsight.Api.Services.Text;

public class TextCleaner
{
    private static readonly HashSet<string> StopWords =
    [
        "это",
        "что",
        "как",
        "так",
        "мне",
        "все",
        "они",
        "где",
        "кто",
        "если",
        "уже",

        "там",
        "тут",
        "куда",
        "вот",
        "меня",
        "просто",
        "может",

        "the",
        "and",
        "for",
        "with",
        "you",
        "your"
    ];

    public List<string> ExtractWords(string text)
    {
        var words = Regex.Matches(
                text.ToLower(),
                @"\b[\p{L}\p{N}]+\b")
            .Select(x => x.Value)
            .Where(x => x.Length >= 3)
            .Where(x => !StopWords.Contains(x))
            .Where(x => x != "chorus")
            .Where(x => x != "verse")
            .ToList();

        return words;
    }
}