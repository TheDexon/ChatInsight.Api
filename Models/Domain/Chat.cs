namespace ChatInsight.Api.Models.Domain;

/// <summary>Сохранённый чат. SourceId — стабильный id из Telegram-экспорта,
/// по нему распознаём повторный импорт того же чата.</summary>
public class Chat
{
    public Guid Id { get; set; }

    /// <summary>Telegram id чата (стабилен между экспортами). 0 — если не указан.</summary>
    public long SourceId { get; set; }

    public string Name { get; set; } = "";

    public string Type { get; set; } = "";

    public DateTime ImportedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int MessageCount { get; set; }

    public List<Message> Messages { get; set; } = [];
}
