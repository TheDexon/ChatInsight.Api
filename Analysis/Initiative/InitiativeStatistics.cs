namespace ChatInsight.Api.Analysis.Initiative;

public class InitiativeStatistics
{
    public Dictionary<string, int> ConversationStarts { get; set; } = [];

    public Dictionary<string, int> DailyStarts { get; set; } = [];

    public Dictionary<string, int> LongPauseStarts { get; set; } = [];
}