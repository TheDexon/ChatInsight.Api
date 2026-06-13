using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.LifeTimeline;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Хронология жизни: выделяет ЖИЗНЕННЫЕ ВЕХИ (отношения, работа, здоровье, учёба,
/// переезды, увлечения, проекты) поверх выжимок DigestService — полный охват чата.
/// </summary>
public class LifeTimelineService
{
    private readonly OllamaClient _ollama;
    private readonly DigestService _digest;

    public LifeTimelineService(OllamaClient ollama, DigestService digest)
    {
        _ollama = ollama;
        _digest = digest;
    }

    public async Task<LifeTimelineResult> AnalyzeAsync(Guid chatId, CancellationToken ct = default)
    {
        var digests = await _digest.GetOrBuildAsync(chatId, null, ct);
        if (digests.Count == 0)
            return new LifeTimelineResult { Summary = "Недостаточно данных.", Model = _ollama.Model };

        var user =
            "Ниже — выжимки по всем периодам переписки (полный охват, хронологически):\n\n" +
            _digest.ToPromptText(digests);

        var system =
            AiPrompts.IronyNote +
            "Ты — биограф. По выжимкам построй ХРОНОЛОГИЮ ЖИЗНИ участников: выдели " +
            "значимые ЖИЗНЕННЫЕ ВЕХИ и этапы (а не пересказ болтовни). Заполни:\n" +
            "- events: 6-12 вех. Каждая: period («01.2026» или «весна 2026»), title " +
            "(коротко), description (1-2 предложения с конкретикой и именами), category " +
            "(одно слово: отношения, работа, здоровье, учёба, переезд, увлечения, проект, финансы).\n" +
            "- summary: 3-4 предложения об общем жизненном пути за период.\n" +
            "Распределяй вехи РАВНОМЕРНО по всему сроку. Только реальные события, " +
            "не выдумывай из шуток. Пиши по-русски.";

        var raw = await _ollama.ChatAsync(system, user, Schema(), ct);
        var result = Parse(raw);
        result.Model = _ollama.Model;
        return result;
    }

    private static JsonNode Schema() => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["summary"] = new JsonObject { ["type"] = "string" },
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
            }
        },
        ["required"] = new JsonArray { "summary", "events" }
    };

    private static LifeTimelineResult Parse(string raw)
    {
        try
        {
            var s = raw.IndexOf('{'); var e = raw.LastIndexOf('}');
            var json = (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
            var doc = JsonSerializer.Deserialize<LifeTimelineResult>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (doc is not null) return doc;
        }
        catch (JsonException) { }
        return new LifeTimelineResult { Summary = raw.Trim() };
    }
}
