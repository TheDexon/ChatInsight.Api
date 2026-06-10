namespace ChatInsight.Api.Models.Domain;

/// <summary>Сохранённый чат (один импорт Telegram Export).</summary>
public class Chat
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Type { get; set; } = "";

    public DateTime ImportedAt { get; set; }

    public int MessageCount { get; set; }

    public List<Message> Messages { get; set; } = [];
}
