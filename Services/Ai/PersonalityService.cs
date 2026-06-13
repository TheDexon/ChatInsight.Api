using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Personality;
using ChatInsight.Api.Configuration;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Telegram;
using ChatInsight.Api.Services.Text;
using Microsoft.Extensions.Options;

namespace ChatInsight.Api.Services.Ai;

public class PersonalityService
{
    private readonly OllamaClient _ollama;
    private readonly TelegramTextExtractor _extractor;
    private readonly OllamaOptions _options;

    public PersonalityService(
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
            ["communicationStyle"] = new JsonObject { ["type"] = "string" },
            ["traits"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" }
            }
        },
        ["required"] = new JsonArray { "summary", "communicationStyle", "traits" }
    };

    public async Task<List<PersonalityProfile>> AnalyzeAsync(
        ChatAnalysisContext context,
        CancellationToken ct = default)
    {
        var result = new List<PersonalityProfile>();

        foreach (var author in context.Participants)
        {
            var msgs = context.Messages.Where(m => m.From == author).ToList();
            if (msgs.Count == 0) continue;
            result.Add(await AnalyzeAuthorAsync(author, msgs, ct));
        }

        return result;
    }

    private async Task<PersonalityProfile> AnalyzeAuthorAsync(
        string author, List<TelegramMessage> msgs, CancellationToken ct)
    {
        var system =
            AiPrompts.IronyNote +
            "Ты — психолог-аналитик. По сообщениям ОДНОГО участника переписки " +
            "составь его краткий портрет. Заполни все поля: " +
            "summary (2-3 предложения о характере и манере), " +
            "communicationStyle (как он общается), " +
            "traits (4-7 коротких черт характера). " +
            "Пиши по-русски, объективно, без морализаторства и осуждения. " +
            "Помни: мат и грубость в шутку — не признак агрессии.";

        var sb = new StringBuilder();
        sb.AppendLine($"Участник: {author}");
        sb.AppendLine($"Всего его сообщений: {msgs.Count}");
        sb.AppendLine("Выборка его сообщений:");

        var take = Math.Min(_options.SampleMessages, msgs.Count);
        var step = msgs.Count <= take ? 1 : msgs.Count / take;

        for (int i = 0; i < msgs.Count; i += step)
        {
            var t = _extractor.Extract(msgs[i].Text);
            if (string.IsNullOrWhiteSpace(t)) continue;
            if (t.Length > 200) t = t[..200] + "…";
            sb.AppendLine($"- {t}");
        }

        var raw = await _ollama.ChatAsync(system, sb.ToString(), BuildSchema(), ct);

        var profile = Parse(raw);
        profile.Participant = author;
        profile.Model = _ollama.Model;
        return profile;
    }

    private static PersonalityProfile Parse(string raw)
    {
        var json = Extract(raw);
        try
        {
            var d = JsonSerializer.Deserialize<PersonalityProfile>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (d is not null) return d;
        }
        catch (JsonException) { }

        return new PersonalityProfile { Summary = raw.Trim() };
    }

    private static string Extract(string raw)
    {
        var s = raw.IndexOf('{');
        var e = raw.LastIndexOf('}');
        return (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
    }
}
