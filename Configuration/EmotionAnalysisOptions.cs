namespace ChatInsight.Api.Configuration;

/// <summary>
/// Словари для keyword-анализа эмоций. Читаются из appsettings.json,
/// секция "EmotionAnalysis". Раньше были захардкожены в EmotionService.
/// </summary>
public class EmotionAnalysisOptions
{
    public const string SectionName = "EmotionAnalysis";

    public List<string> PositiveWords { get; set; } = [];

    public List<string> NegativeWords { get; set; } = [];

    public List<string> ProfanityWords { get; set; } = [];
}
