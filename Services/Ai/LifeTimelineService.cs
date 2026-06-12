using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.LifeTimeline;
using ChatInsight.Api.Configuration;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;
using Microsoft.Extensions.Options;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Строит жизненную хронологию по переписке: значимые события, этапы,
/// изменения. Structured output по JSON Schema.
/// </summary>
public class LifeTimelineService
{
    private readonly OllamaClient _ollama;
    private readonly TelegramTextExtractor _extractor;
    private readonly OllamaOptions _options;

    public LifeTimelineService(
        OllamaClient ollama,
        TelegramTextExtractor extractor,
        IOptions<OllamaOptions> options)
    {
        _ollama = ollama;
        _extractor = extractor;
        _options = options.Value;
    }

    private static JsonNode BuildSchema() => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["events"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["period"] = new JsonObject { ["type"] = "string" },
                        ["title"] = new JsonObject { ["type"] = "string" },
                        ["description"] = new JsonObject { ["type"] = "string" },
                        ["category"] = new JsonObject { ["type"] = "string" }
                    },
                    ["required"] = new JsonArray { "period", "title", "description", "category" }
                }
            },
            ["summary"] = new JsonObject { ["type"] = "string" }
        },
        ["required"] = new JsonArray { "events", "summary" }
    };

    public async Task<LifeTimelineResult> AnalyzeAsync(
        ChatAnalysisContext context,
        CancellationToken ct = default)
    {
        var system =
            "Ты — биограф-аналитик. По переписке восстанови хронологию жизни " +
            "участников: значимые события и изменения (работа, учёба, отношения, " +
            "переезды, увлечения, важные решения). Для каждого события укажи period " +
            "(год или месяц-год), title (кратко), description (1-2 предложения), " +
            "category. Расположи события по времени. summary — короткое описание " +
            "жизненного пути за период. Пиши по-русски, опирайся только на факты из " +
            "переписки, не выдумывай.";

        var user = BuildPrompt(context);

        var raw = await _ollama.ChatAsync(system, user, BuildSchema(), ct);

        var result = Parse(raw);
        result.Model = _ollama.Model;
        return result;
    }

    private string BuildPrompt(ChatAnalysisContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Чат: {context.Export.Name}");
        sb.AppendLine(
            $"Период: {context.FirstMessageDate:dd.MM.yyyy} — " +
            $"{context.LastMessageDate:dd.MM.yyyy}");
        sb.AppendLine($"Участники: {string.Join(", ", context.Participants)}");
        sb.AppendLine();
        sb.AppendLine("Сообщения (хронологически, с датами):");

        var messages = context.Messages;
        var take = Math.Min(_options.SampleMessages, messages.Count);
        var step = messages.Count <= take ? 1 : messages.Count / take;

        for (int i = 0; i < messages.Count; i += step)
        {
            var m = messages[i];
            var t = _extractor.Extract(m.Text);
            if (string.IsNullOrWhiteSpace(t)) continue;
            if (t.Length > 200) t = t[..200] + "…";
            sb.AppendLine($"[{m.Date:MM.yyyy}] {m.From}: {t}");
        }

        return sb.ToString();
    }

    private static LifeTimelineResult Parse(string raw)
    {
        var json = Extract(raw);
        try
        {
            var r = JsonSerializer.Deserialize<LifeTimelineResult>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (r is not null) return r;
        }
        catch (JsonException) { }

        return new LifeTimelineResult { Summary = raw.Trim() };
    }

    private static string Extract(string raw)
    {
        var s = raw.IndexOf('{');
        var e = raw.LastIndexOf('}');
        return (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
    }
}
