namespace ChatInsight.Api.Analysis.Text;

public class TextStatistics
{
    public List<string> TopWords { get; set; } = [];

    public int TotalCharacters { get; set; }

    public int TotalWords { get; set; }

    public double AverageWordsPerMessage { get; set; }
}