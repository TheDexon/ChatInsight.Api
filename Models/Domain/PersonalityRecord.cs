namespace ChatInsight.Api.Models.Domain;

/// <summary>Сохранённый AI-портрет участника (кэш). Один на пару (чат, участник).</summary>
public class PersonalityRecord
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public string Participant { get; set; } = "";
    public string Summary { get; set; } = "";
    public string CommunicationStyle { get; set; } = "";
    public List<string> Traits { get; set; } = [];

    public string Model { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
}
