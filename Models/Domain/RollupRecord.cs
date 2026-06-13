namespace ChatInsight.Api.Models.Domain;

/// <summary>Кэш итога посуточного анализа (один на чат). Результат — JSON-строкой.</summary>
public class RollupRecord
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public string ResultJson { get; set; } = "{}";
    public int DigestCount { get; set; }
    public string Model { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}
