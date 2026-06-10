namespace ChatInsight.Api.DTOs;

public class ImportResultDto
{
    public Guid ChatId { get; set; }

    public string ChatName { get; set; } = "";

    public string ChatType { get; set; } = "";

    public int MessagesCount { get; set; }

    public DateTime? FirstMessageDate { get; set; }

    public DateTime? LastMessageDate { get; set; }

    public List<string> Participants { get; set; } = [];
}
