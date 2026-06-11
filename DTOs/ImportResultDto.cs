namespace ChatInsight.Api.DTOs;

public class ImportResultDto
{
    public Guid ChatId { get; set; }

    public string ChatName { get; set; } = "";

    public string ChatType { get; set; } = "";

    /// <summary>Всего сообщений в чате после импорта.</summary>
    public int MessagesCount { get; set; }

    /// <summary>Сколько новых сообщений добавлено этим импортом.</summary>
    public int NewMessages { get; set; }

    /// <summary>true — создан новый чат; false — дополнен существующий.</summary>
    public bool IsNewChat { get; set; }

    public DateTime? FirstMessageDate { get; set; }

    public DateTime? LastMessageDate { get; set; }

    public List<string> Participants { get; set; } = [];
}
