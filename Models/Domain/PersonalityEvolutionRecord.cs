namespace ChatInsight.Api.Models.Domain;

/// <summary>Кэш анализа эволюции личности (один на чат). Результат — JSON-строкой.</summary>
public class PersonalityEvolutionRecord
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public string ResultJson { get; set; } = "{}";
    public string Model { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}
