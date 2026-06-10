using ChatInsight.Api.Analysis.Response;
using ChatInsight.Api.Domain;

namespace ChatInsight.Api.Services.Analytics;

public class ResponseService
{
    public ResponseStatistics Analyze(ChatAnalysisContext context)
    {
        var result = new ResponseStatistics();

        // уже отфильтровано и отсортировано по дате
        var messages = context.Messages;

        var responseTimes = new Dictionary<string, List<double>>();

        for (int i = 1; i < messages.Count; i++)
        {
            var previous = messages[i - 1];
            var current = messages[i];

            if (previous.From == current.From)
                continue;

            var minutes =
                (current.Date - previous.Date).TotalMinutes;

            if (minutes <= 0)
                continue;

            if (minutes > 1440)
                continue;

            if (!responseTimes.TryGetValue(current.From!, out var list))
            {
                list = [];
                responseTimes[current.From!] = list;
            }

            list.Add(minutes);
        }

        result.AverageResponseMinutes =
            responseTimes.ToDictionary(
                x => x.Key,
                x => Math.Round(x.Value.Average(), 2));

        result.ResponseCount =
            responseTimes.ToDictionary(
                x => x.Key,
                x => x.Value.Count);

        return result;
    }
}
