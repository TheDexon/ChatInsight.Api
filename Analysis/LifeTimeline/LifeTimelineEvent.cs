namespace ChatInsight.Api.Analysis.LifeTimeline;

/// <summary>Одно событие/этап жизненной хронологии, выделенный ИИ.</summary>
public class LifeTimelineEvent
{
    /// <summary>Период или дата: "2023", "Май 2024", "лето 2025".</summary>
    public string Period { get; set; } = "";

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Категория: работа, отношения, учёба, переезд, увлечения…</summary>
    public string Category { get; set; } = "";
}
