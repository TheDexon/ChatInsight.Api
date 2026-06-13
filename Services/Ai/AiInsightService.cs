using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Ai;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// AI-инсайты по чату. Строится поверх выжимок DigestService (полный охват чата,
/// а не выборка): сводка, эмоциональный фон, темы, динамика.
/// </summary>
public class AiInsightService
{
    private readonly OllamaClient _ollama;
    private readonly DigestService _digest;

    public AiInsightService(OllamaClient ollama, DigestService digest)
    {
        _ollama = ollama;
        _digest = digest;
    }

    public async Task<AiInsight> AnalyzeAsync(Guid chatId, CancellationToken ct = default)
    {
        var digests = await _digest.GetOrBuildAsync(chatId, null, ct);
        if (digests.Count == 0)
            return new AiInsight { Summary = "Недостаточно данных.", Model = _ollama.Model };

        var user =
            "Ниже — выжимки по всем периодам переписки (полный охват, хронологически):\n\n" +
            _digest.ToPromptText(digests);

        var system =
            AiPrompts.IronyNote +
            "Ты — аналитик личных переписок. На вход — выжимки по ВСЕМ периодам диалога. " +
            "Сделай объективный разбор на русском. Заполни ВСЕ поля:\n" +
            "- summary: 3-5 предложений о характере общения и отношениях (с конкретикой и именами);\n" +
            "- emotionalTone: 1-2 предложения об эмоциональном фоне;\n" +
            "- topics: 5-8 реальных тем общения (короткие фразы, по существу);\n" +
            "- dynamics: 3-5 наблюдений, как менялось общение от ранних периодов к поздним.\n" +
            "Не выдумывай из шуток, не оставляй поля пустыми.";

        var raw = await _ollama.ChatAsync(system, user, Schema(), ct);
        var insight = Parse(raw);
        insight.Model = _ollama.Model;
        return insight;
    }

    private static JsonNode Schema() => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["summary"] = new JsonObject { ["type"] = "string" },
            ["emotionalTone"] = new JsonObject { ["type"] = "string" },
            ["topics"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
            ["dynamics"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } }
        },
        ["required"] = new JsonArray { "summary", "emotionalTone", "topics", "dynamics" }
    };

    private static AiInsight Parse(string raw)
    {
        try
        {
            var s = raw.IndexOf('{'); var e = raw.LastIndexOf('}');
            var json = (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
            var doc = JsonSerializer.Deserialize<AiInsight>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (doc is not null) return doc;
        }
        catch (JsonException) { }
        return new AiInsight { Summary = raw.Trim() };
    }
}
