namespace ChatInsight.Api.Models.Domain;

/// <summary>Кэш AI-хронологии (один на чат). События хранятся JSON-строкой.</summary>
public class LifeTimelineRecord
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    /// <summary>Список событий, сериализованный в JSON.</summary>
    public string EventsJson { get; set; } = "[]";

    public string Summary { get; set; } = "";
    public string Model { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}
