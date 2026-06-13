namespace ChatInsight.Api.Models.Domain;

/// <summary>
/// Сохранённая выжимка одного куска переписки (период). Строится один раз,
/// переиспользуется всеми чат-уровневыми анализами (инсайты, хронология, rollup).
/// </summary>
public class PeriodDigestRecord
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public int OrderIndex { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int MessageCount { get; set; }

    public string Summary { get; set; } = "";
    public string Mood { get; set; } = "";

    /// <summary>События куска — JSON-массив строк.</summary>
    public string EventsJson { get; set; } = "[]";

    public string Model { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}
