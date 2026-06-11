using System.Text;
using ChatInsight.Api.Analysis.Relationship;
using ChatInsight.Api.Domain;

namespace ChatInsight.Api.Services.Analytics;

/// <summary>
/// Баланс отношений между двумя самыми активными участниками:
/// активность, инициатива (кто начинает диалог) и ответы (кто чаще отвечает).
/// Переиспользует InitiativeService и ResponseService.
/// </summary>
public class RelationshipService
{
    private readonly InitiativeService _initiative;
    private readonly ResponseService _response;

    public RelationshipService(
        InitiativeService initiative,
        ResponseService response)
    {
        _initiative = initiative;
        _response = response;
    }

    public RelationshipReport Analyze(ChatAnalysisContext context)
    {
        var report = new RelationshipReport();

        var authors = context.Messages
            .GroupBy(x => x.From!)
            .OrderByDescending(x => x.Count())
            .ToList();

        if (authors.Count < 2)
            return report;

        var first = authors[0].Key;
        var second = authors[1].Key;

        // --- Активность ---
        var msgTotal = authors[0].Count() + authors[1].Count();
        report.ActivityBalance = Pct(authors[0].Count(), msgTotal);
        report.DominantParticipant = first;
        report.SecondaryParticipant = second;

        // --- Инициатива (старты диалога после паузы) ---
        var ini = _initiative.Analyze(context);
        var iniFirst = ini.ConversationStarts.GetValueOrDefault(first);
        var iniSecond = ini.ConversationStarts.GetValueOrDefault(second);
        var iniTotal = iniFirst + iniSecond;
        report.InitiativeBalance = iniTotal > 0 ? Pct(iniFirst, iniTotal) : 50;

        // --- Ответы (кто чаще отвечает) ---
        var resp = _response.Analyze(context);
        var rFirst = resp.ResponseCount.GetValueOrDefault(first);
        var rSecond = resp.ResponseCount.GetValueOrDefault(second);
        var rTotal = rFirst + rSecond;
        report.ResponseBalance = rTotal > 0 ? Pct(rFirst, rTotal) : 50;

        report.Summary = BuildSummary(report, first, second, resp);
        return report;
    }

    private static int Pct(int part, int total) =>
        total == 0 ? 0 : (int)Math.Round(part * 100.0 / total);

    private static string BuildSummary(
        RelationshipReport r,
        string first,
        string second,
        Analysis.Response.ResponseStatistics resp)
    {
        var sb = new StringBuilder();

        sb.Append($"{first} активнее: {r.ActivityBalance}% сообщений. ");

        if (r.InitiativeBalance > 55)
            sb.Append($"Чаще начинает диалог {first}. ");
        else if (r.InitiativeBalance < 45)
            sb.Append($"Чаще начинает диалог {second}. ");
        else
            sb.Append("Инициатива распределена примерно поровну. ");

        var firstAvg = resp.AverageResponseMinutes.GetValueOrDefault(first);
        var secondAvg = resp.AverageResponseMinutes.GetValueOrDefault(second);

        if (firstAvg > 0 && secondAvg > 0)
        {
            var faster = firstAvg < secondAvg ? first : second;
            sb.Append($"Быстрее отвечает {faster}.");
        }

        return sb.ToString().Trim();
    }
}
