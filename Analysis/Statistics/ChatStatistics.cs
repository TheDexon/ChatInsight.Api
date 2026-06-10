namespace ChatInsight.Api.Analysis.Statistics;

public class ChatStatistics
{
    public int TotalMessages { get; set; }

    public double AverageMessageLength { get; set; }

    public int MostActiveHour { get; set; }

    public Dictionary<string, int> MessagesByAuthor { get; set; } = [];

    public Dictionary<int, int> MessagesByHour { get; set; } = [];

    public Dictionary<DateOnly, int> MessagesByDay { get; set; } = [];
}