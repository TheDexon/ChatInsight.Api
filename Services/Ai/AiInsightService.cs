using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Ai;
using ChatInsight.Api.Configuration;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Services.Text;
using Microsoft.Extensions.Options;

namespace ChatInsight.Api.Services.Ai;

public class AiInsightService
{
    private readonly OllamaClient _ollama;
    private readonly TelegramTextExtractor _extractor;
    private readonly OllamaOptions _options;

    public AiInsightService(
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
            ["summary"] = new JsonObject { ["type"] = "string" },
            ["emotionalTone"] = new JsonObject { ["type"] = "string" },
            ["topics"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" }
            },
            ["dynamics"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" }
            }
        },
        ["required"] = new JsonArray { "summary", "emotionalTone", "topics", "dynamics" }
    };

    public async Task<AiInsight> AnalyzeAsync(
        ChatAnalysisContext context,
        CancellationToken ct = default)
    {
        var systemPrompt =
            AiPrompts.IronyNote +
            "Ты — аналитик личных переписок. Анализируешь диалог объективно, " +
            "на русском языке. Заполни ВСЕ поля результата:\n" +
            "- summary: 2-4 предложения о характере общения и отношениях;\n" +
            "- emotionalTone: 1-2 предложения об эмоциональном фоне;\n" +
            "- topics: 3-7 основных тем общения (короткие фразы);\n" +
            "- dynamics: 3-5 наблюдений, как менялось общение во времени.\n" +
            "Пиши содержательно и по-русски. Не оставляй поля пустыми.";

        var userPrompt = BuildUserPrompt(context);

        var raw = await _ollama.ChatAsync(systemPrompt, userPrompt, BuildSchema(), ct);

        var insight = ParseInsight(raw);
        insight.Model = _ollama.Model;
        return insight;
    }

    private string BuildUserPrompt(ChatAnalysisContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Чат: {context.Export.Name}");
        sb.AppendLine(
            $"Период: {context.FirstMessageDate:dd.MM.yyyy} — " +
            $"{context.LastMessageDate:dd.MM.yyyy}");
        sb.AppendLine($"Всего сообщений: {context.TotalMessages}");
        sb.AppendLine($"Участники: {string.Join(", ", context.Participants)}");
        sb.AppendLine();
        sb.AppendLine("Выборка сообщений (хронологически):");

        var messages = context.Messages;
        var take = Math.Min(_options.SampleMessages, messages.Count);
        var step = messages.Count <= take ? 1 : messages.Count / take;

        for (int i = 0; i < messages.Count; i += step)
        {
            var m = messages[i];
            var text = _extractor.Extract(m.Text);
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (text.Length > 200) text = text[..200] + "…";
            sb.AppendLine($"[{m.Date:dd.MM.yy}] {m.From}: {text}");
        }

        return sb.ToString();
    }

    private static AiInsight ParseInsight(string raw)
    {
        var json = ExtractJson(raw);
        try
        {
            var doc = JsonSerializer.Deserialize<AiInsight>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (doc is not null) return doc;
        }
        catch (JsonException) { }

        return new AiInsight { Summary = raw.Trim() };
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return (start >= 0 && end > start) ? raw.Substring(start, end - start + 1) : raw;
    }
}
