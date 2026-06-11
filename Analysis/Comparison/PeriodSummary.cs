namespace ChatInsight.Api.Analysis.Comparison;

/// <summary>Снимок метрик за один период.</summary>
public class PeriodSummary
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int Messages { get; set; }
    public double AvgMessageLength { get; set; }

    public int PositiveMessages { get; set; }
    public int NegativeMessages { get; set; }
    public double ToxicityScore { get; set; }

    public double AvgResponseMinutes { get; set; }

    /// <summary>Топ-темы (слова) периода.</summary>
    public List<string> TopTopics { get; set; } = [];
}
