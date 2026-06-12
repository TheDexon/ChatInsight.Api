namespace ChatInsight.Api.Analysis.LifeTimeline;

/// <summary>AI-хронология жизни по переписке.</summary>
public class LifeTimelineResult
{
    public List<LifeTimelineEvent> Events { get; set; } = [];

    /// <summary>Итоговое описание жизненного пути за период.</summary>
    public string Summary { get; set; } = "";

    public string Model { get; set; } = "";
}
