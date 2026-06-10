using ChatInsight.Api.Analysis.Relationship;
using ChatInsight.Api.Models.Telegram;

namespace ChatInsight.Api.Services.Analytics;

public class RelationshipService
{
    public RelationshipReport Analyze(
        TelegramExport export)
    {
        var report =
            new RelationshipReport();

        var messages = export.Messages
            .Where(x =>
                x.Type == "message" &&
                !string.IsNullOrWhiteSpace(x.From))
            .ToList();

        var authors = messages
            .GroupBy(x => x.From!)
            .OrderByDescending(x => x.Count())
            .ToList();

        if (authors.Count < 2)
            return report;

        var first = authors[0];
        var second = authors[1];

        var total =
            first.Count() +
            second.Count();

        report.ActivityBalance =
            (int)Math.Round(
                first.Count() * 100.0 / total);

        report.DominantParticipant =
            first.Key;

        report.Summary =
            $"{first.Key} написал больше сообщений.";

        return report;
    }
}