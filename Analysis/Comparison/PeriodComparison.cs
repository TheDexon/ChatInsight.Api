namespace ChatInsight.Api.Analysis.Comparison;

/// <summary>Сравнение двух периодов общения: «было → стало».</summary>
public class PeriodComparison
{
    public PeriodSummary First { get; set; } = new();
    public PeriodSummary Second { get; set; } = new();

    // Дельты (второй период минус первый)
    public int MessagesDelta { get; set; }
    public double ToxicityDelta { get; set; }
    public double ResponseMinutesDelta { get; set; }

    /// <summary>Темы, появившиеся во втором периоде.</summary>
    public List<string> NewTopics { get; set; } = [];

    /// <summary>Темы, пропавшие во втором периоде.</summary>
    public List<string> FadedTopics { get; set; } = [];

    /// <summary>Короткий человекочитаемый вывод.</summary>
    public string Summary { get; set; } = "";
}
