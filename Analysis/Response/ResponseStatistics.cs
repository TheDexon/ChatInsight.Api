namespace ChatInsight.Api.Analysis.Response;

public class ResponseStatistics
{
    public Dictionary<string, double> AverageResponseMinutes { get; set; } = [];

    public Dictionary<string, int> ResponseCount { get; set; } = [];
}