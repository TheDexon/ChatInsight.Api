namespace ChatInsight.Api.Models.Domain;

/// <summary>Одно сообщение чата в БД.</summary>
public class Message
{
    public long Id { get; set; }                 // PK (identity)

    public Guid ChatId { get; set; }             // FK -> Chat
    public Chat Chat { get; set; } = null!;

    public long TelegramId { get; set; }         // оригинальный id из экспорта
    public string Type { get; set; } = "";       // "message", "service", ...
    public DateTime Date { get; set; }
    public string? Author { get; set; }          // from

    public string Text { get; set; } = "";       // плоский текст — для анализа
    public string? RawTextJson { get; set; }     // исходный text (JSON) — на будущее
}
