namespace ChatInsight.Api.Models.Domain;

public static class AiJobType
{
    public const string Insights = "insights";
    public const string Personality = "personality";
    public const string Timeline = "timeline";
    public const string Evolution = "evolution";
}

public static class AiJobStatus
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Done = "done";
    public const string Failed = "failed";
}

public class AiJob
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }

    public string JobType { get; set; } = "";
    public string Status { get; set; } = AiJobStatus.Pending;

    public string? ResultJson { get; set; }
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
