using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Evolution;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Telegram;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Анализ эволюции личности: режет переписку на два периода, строит портрет
/// каждого участника в каждом периоде (через PersonalityService) и просит модель
/// описать, что изменилось.
/// </summary>
public class PersonalityEvolutionService
{
    private readonly PersonalityService _personality;
    private readonly OllamaClient _ollama;

    public PersonalityEvolutionService(
        PersonalityService personality,
        OllamaClient ollama)
    {
        _personality = personality;
        _ollama = ollama;
    }

    public async Task<PersonalityEvolutionResult> AnalyzeAsync(
        ChatAnalysisContext context,
        DateTime? splitDate = null,
        CancellationToken ct = default)
    {
        var messages = context.Messages;
        if (messages.Count < 4)
            return new() { Summary = "Недостаточно сообщений для анализа эволюции." };

        var split = splitDate ?? messages[messages.Count / 2].Date;
        var before = messages.Where(m => m.Date < split).ToList();
        var after = messages.Where(m => m.Date >= split).ToList();

        if (before.Count == 0 || after.Count == 0)
        {
            var mid = messages.Count / 2;
            before = messages.Take(mid).ToList();
            after = messages.Skip(mid).ToList();
        }

        if (before.Count == 0 || after.Count == 0)
            return new() { Summary = "Не удалось разделить переписку на периоды." };

        var beforeProfiles = await _personality.AnalyzeAsync(Sub(context, before), ct);
        var afterProfiles = await _personality.AnalyzeAsync(Sub(context, after), ct);

        var entries = new List<EvolutionEntry>();
        foreach (var participant in context.Participants)
        {
            var b = beforeProfiles.FirstOrDefault(x => x.Participant == participant);
            var a = afterProfiles.FirstOrDefault(x => x.Participant == participant);
            if (b is null || a is null) continue;

            entries.Add(new EvolutionEntry
            {
                Participant = participant,
                Before = b,
                After = a
            });
        }

        if (entries.Count == 0)
            return new() { Summary = "Не нашлось участников, активных в обоих периодах." };

        var summary = await DescribeChangesAsync(entries, ct);

        return new PersonalityEvolutionResult
        {
            Entries = entries,
            Summary = summary,
            Model = _ollama.Model
        };
    }

    private static ChatAnalysisContext Sub(
        ChatAnalysisContext full, List<TelegramMessage> subset) =>
        ChatAnalysisContext.Create(new TelegramExport
        {
            Name = full.Export.Name,
            Type = full.Export.Type,
            Messages = subset
        });

    /// <summary>Один AI-запрос: заполняет Change у каждого entry, возвращает общий вывод.</summary>
    private async Task<string> DescribeChangesAsync(
        List<EvolutionEntry> entries, CancellationToken ct)
    {
        var system =
            AiPrompts.IronyNote +
            "Ты — психолог. Для каждого участника даны два портрета: РАНЬШЕ и ПОЗЖЕ. " +
            "Опиши, как человек изменился между периодами: change — 1-2 предложения о " +
            "ключевых изменениях характера/стиля/настроя. summary — общий вывод об " +
            "эволюции участников. Пиши по-русски, объективно, опираясь на портреты.";

        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine($"Участник: {e.Participant}");
            sb.AppendLine($"  РАНЬШЕ: {e.Before.Summary} Стиль: {e.Before.CommunicationStyle}. " +
                          $"Черты: {string.Join(", ", e.Before.Traits)}");
            sb.AppendLine($"  ПОЗЖЕ: {e.After.Summary} Стиль: {e.After.CommunicationStyle}. " +
                          $"Черты: {string.Join(", ", e.After.Traits)}");
            sb.AppendLine();
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["changes"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["participant"] = new JsonObject { ["type"] = "string" },
                            ["change"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray { "participant", "change" }
                    }
                },
                ["summary"] = new JsonObject { ["type"] = "string" }
            },
            ["required"] = new JsonArray { "changes", "summary" }
        };

        var raw = await _ollama.ChatAsync(system, sb.ToString(), schema, ct);
        return ApplyChanges(entries, raw);
    }

    private static string ApplyChanges(List<EvolutionEntry> entries, string raw)
    {
        var json = Extract(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<ChangesDto>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is not null)
            {
                foreach (var c in parsed.Changes)
                {
                    var entry = entries.FirstOrDefault(e => e.Participant == c.Participant);
                    if (entry is not null)
                        entry.Change = c.Change;
                }
                return parsed.Summary;
            }
        }
        catch (JsonException) { }

        return "";
    }

    private static string Extract(string raw)
    {
        var s = raw.IndexOf('{');
        var e = raw.LastIndexOf('}');
        return (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
    }

    private class ChangesDto
    {
        public List<ChangeItem> Changes { get; set; } = [];
        public string Summary { get; set; } = "";
    }

    private class ChangeItem
    {
        public string Participant { get; set; } = "";
        public string Change { get; set; } = "";
    }
}
