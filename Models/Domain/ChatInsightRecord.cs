namespace ChatInsight.Api.Models.Domain;

/// <summary>
/// Сохранённый результат AI-анализа чата (кэш).
/// Один-к-одному с Chat: считаем один раз, дальше отдаём из БД.
/// </summary>
public class ChatInsightRecord
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public string Summary { get; set; } = "";
    public string EmotionalTone { get; set; } = "";

    /// <summary>Списки строк хранятся JSON-колонкой (jsonb).</summary>
    public List<string> Topics { get; set; } = [];
    public List<string> Dynamics { get; set; } = [];

    public string Model { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}
