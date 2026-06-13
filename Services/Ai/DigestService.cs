using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Rollup;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Services.Text;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Единый фундамент анализа: режет ВЕСЬ чат на куски (~250 сообщений), по каждому
/// делает AI-выжимку (о чём, настроение, события) и СОХРАНЯЕТ её. Строится один
/// раз — дальше инсайты, хронология и rollup строятся поверх выжимок (100% охват,
/// а не выборка). Также умеет собирать выжимки в итоговую хронологию (RollupAsync).
/// </summary>
public class DigestService
{
    private readonly ChatInsightDbContext _db;
    private readonly OllamaClient _ollama;
    private readonly ILogger<DigestService> _logger;

    private const int ChunkSize = 400;        // крупнее куски = меньше запросов к модели
    private const int MaxLinesPerChunk = 200;
    private const int MaxCharsPerLine = 90;   // короче строки = легче и быстрее запрос

    public DigestService(
        ChatInsightDbContext db,
        OllamaClient ollama,
        ILogger<DigestService> logger)
    {
        _db = db;
        _ollama = ollama;
        _logger = logger;
    }

    /// <summary>Отдаёт сохранённые выжимки, а если их нет — строит и сохраняет.</summary>
    public async Task<List<PeriodDigest>> GetOrBuildAsync(
        Guid chatId, Func<int, int, Task>? onProgress, CancellationToken ct = default)
    {
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId && m.Type == "message" && m.Author != null)
            .OrderBy(m => m.Date)
            .Select(m => new MsgRow(m.Date, m.Author!, m.Text))
            .ToListAsync(ct);

        if (messages.Count < 10)
            return [];

        var chunks = Chunk(messages, ChunkSize);
        int total = chunks.Count;

        // уже построенные куски (по позиции) — чтобы продолжить с места обрыва
        var doneIndices = (await _db.PeriodDigests
                .Where(d => d.ChatId == chatId)
                .Select(d => d.OrderIndex)
                .ToListAsync(ct))
            .ToHashSet();

        if (doneIndices.Count > 0)
            _logger.LogInformation(
                "Digest {ChatId}: продолжаю, уже готово {Done}/{Total} кусков",
                chatId, doneIndices.Count, total);
        else
            _logger.LogInformation(
                "Digest {ChatId}: старт, {Total} кусков по ~{Size} сообщений",
                chatId, total, ChunkSize);

        for (int i = 0; i < chunks.Count; i++)
        {
            if (!doneIndices.Contains(i))
            {
                try
                {
                    var digest = await DigestChunkAsync(chunks[i], ct);
                    if (digest is not null)
                    {
                        _db.PeriodDigests.Add(ToRecord(chatId, i, digest));
                        await _db.SaveChangesAsync(ct);
                    }
                    _logger.LogInformation("Digest {ChatId}: кусок {I}/{Total} готов", chatId, i + 1, total);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // один проблемный кусок не должен валить весь анализ — пропускаем
                    _logger.LogWarning(ex, "Digest {ChatId}: кусок {I}/{Total} пропущен ({Err})",
                        chatId, i + 1, total, ex.Message);
                }
            }

            if (onProgress is not null)
                await onProgress(i + 1, total);
        }

        var all = await _db.PeriodDigests
            .Where(d => d.ChatId == chatId)
            .OrderBy(d => d.OrderIndex)
            .ToListAsync(ct);

        _logger.LogInformation("Digest {ChatId}: готово, {Count} выжимок", chatId, all.Count);
        return all.Select(FromRecord).ToList();
    }

    // ---------- фаза 1: выжимка куска ----------

    private async Task<PeriodDigest?> DigestChunkAsync(List<MsgRow> chunk, CancellationToken ct)
    {
        var meaningful = chunk.Where(m => MeaningfulTextFilter.IsMeaningful(m.Text)).ToList();
        var mediaCount = chunk.Count(m => string.IsNullOrWhiteSpace(m.Text));

        if (meaningful.Count == 0) return null;

        var from = chunk.First().Date;
        var to = chunk.Last().Date;

        var lines = meaningful;
        if (lines.Count > MaxLinesPerChunk)
        {
            var step = lines.Count / MaxLinesPerChunk;
            lines = lines.Where((_, i) => i % step == 0).Take(MaxLinesPerChunk).ToList();
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Период: {from:dd.MM.yyyy} — {to:dd.MM.yyyy}");
        sb.AppendLine($"Текстовых сообщений: {meaningful.Count}" +
                      (mediaCount > 0 ? $", плюс {mediaCount} медиа без текста (стикеры/голосовые/фото)." : "."));
        sb.AppendLine("Сообщения:");
        foreach (var m in lines)
        {
            var t = m.Text.Trim();
            if (t.Length > MaxCharsPerLine) t = t[..MaxCharsPerLine] + "…";
            sb.AppendLine($"{m.Author}: {t}");
        }

        var system =
            AiPrompts.IronyNote +
            "Ты — аналитик переписки. Это кусок диалога за период. Сделай краткую, но " +
            "КОНКРЕТНУЮ выжимку. Заполни поля:\n" +
            "- summary: 1-2 предложения О ЧЁМ конкретно говорили. Называй конкретику: " +
            "имена людей, места, предметы, события (не «обсуждали разное», а ЧТО именно). " +
            "Если упомянуты имена — сохрани их.\n" +
            "- mood: настроение периода одним-двумя словами.\n" +
            "- events: 1-3 конкретных факта/события (что произошло, о чём договорились, " +
            "что решили). Если совсем ничего конкретного — пустой список.\n" +
            "ВАЖНО: бессмысленный набор букв (типа «аоаоао», «хавхав», «пзхпзх») — мусор, " +
            "игнорируй. Медиа без текста не выдумывай. Шутки не принимай за реальные " +
            "события, но реальные факты внутри шуток сохраняй. Пиши по-русски.";

        var raw = await _ollama.ChatAsync(system, sb.ToString(), DigestSchema(), ct);
        var parsed = ParseDigest(raw);

        return new PeriodDigest
        {
            FromDate = from,
            ToDate = to,
            MessageCount = meaningful.Count,
            Summary = parsed.Summary,
            Mood = parsed.Mood,
            Events = parsed.Events
        };
    }

    // ---------- текст выжимок для промптов потребителей ----------

    public string ToPromptText(List<PeriodDigest> digests)
    {
        var sb = new StringBuilder();
        foreach (var d in digests)
        {
            sb.AppendLine($"[{d.FromDate:dd.MM.yy}—{d.ToDate:dd.MM.yy}] ({d.Mood}) {d.Summary}");
            foreach (var e in d.Events.Where(e => !string.IsNullOrWhiteSpace(e)))
                sb.AppendLine($"  • {e}");
        }
        return sb.ToString();
    }

    // ---------- фаза 2: сборка в итоговую хронологию (для Rollup) ----------

    public async Task<RollupResult> RollupAsync(List<PeriodDigest> digests, CancellationToken ct = default)
    {
        if (digests.Count == 0)
            return new RollupResult { Summary = "Недостаточно данных." };

        var sb = new StringBuilder();
        sb.AppendLine("Ниже — выжимки по последовательным периодам всей переписки " +
                      "(в хронологическом порядке, от ранних к поздним). Собери из них цельную картину.");
        sb.AppendLine();
        sb.Append(ToPromptText(digests));

        var system =
            AiPrompts.IronyNote +
            "Ты — биограф-аналитик. На вход — выжимки по всем периодам переписки " +
            "(полный охват). Построй ИТОГ:\n" +
            "- summary: 4-6 предложений о том, как развивалось общение и отношения за " +
            "весь срок. ОБЯЗАТЕЛЬНО выдели СКВОЗНЫЕ ЛИНИИ — сюжеты (отношения с конкретным " +
            "человеком, работа, здоровье, проекты), тянущиеся через периоды: как начинались, " +
            "менялись и чем закончились. Называй конкретные имена и факты.\n" +
            "- timeline: 6-12 главных событий, РАВНОМЕРНО по всему сроку (ранние важны не " +
            "меньше поздних). Каждое: period («01.2026» или «01—02.2026»), title (коротко), " +
            "description (1-2 предложения с фактами).\n" +
            "Только реальные события, не повторяйся, не выдумывай из шуток. Пиши по-русски.";

        var raw = await _ollama.ChatAsync(system, sb.ToString(), RollupSchema(), ct);
        var res = ParseRollup(raw);
        res.DigestCount = digests.Count;
        res.Model = _ollama.Model;
        return res;
    }

    // ---------- разбиение / записи ----------

    private static List<List<MsgRow>> Chunk(List<MsgRow> msgs, int size)
    {
        var result = new List<List<MsgRow>>();
        for (int i = 0; i < msgs.Count; i += size)
            result.Add(msgs.GetRange(i, Math.Min(size, msgs.Count - i)));
        return result;
    }

    private PeriodDigestRecord ToRecord(Guid chatId, int order, PeriodDigest d) => new()
    {
        Id = Guid.NewGuid(),
        ChatId = chatId,
        OrderIndex = order,
        FromDate = d.FromDate,
        ToDate = d.ToDate,
        MessageCount = d.MessageCount,
        Summary = d.Summary,
        Mood = d.Mood,
        EventsJson = JsonSerializer.Serialize(d.Events),
        Model = _ollama.Model,
        GeneratedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
    };

    private static PeriodDigest FromRecord(PeriodDigestRecord r) => new()
    {
        FromDate = r.FromDate,
        ToDate = r.ToDate,
        MessageCount = r.MessageCount,
        Summary = r.Summary,
        Mood = r.Mood,
        Events = SafeList(r.EventsJson)
    };

    private static List<string> SafeList(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    // ---------- схемы / парсинг ----------

    private static JsonNode DigestSchema() => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["summary"] = new JsonObject { ["type"] = "string" },
            ["mood"] = new JsonObject { ["type"] = "string" },
            ["events"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject { ["type"] = "string" }
            }
        },
        ["required"] = new JsonArray { "summary", "mood", "events" }
    };

    private static JsonNode RollupSchema() => new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["summary"] = new JsonObject { ["type"] = "string" },
            ["timeline"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["period"] = new JsonObject { ["type"] = "string" },
                        ["title"] = new JsonObject { ["type"] = "string" },
                        ["description"] = new JsonObject { ["type"] = "string" }
                    },
                    ["required"] = new JsonArray { "period", "title", "description" }
                }
            }
        },
        ["required"] = new JsonArray { "summary", "timeline" }
    };

    private static DigestDto ParseDigest(string raw)
    {
        try
        {
            var d = JsonSerializer.Deserialize<DigestDto>(
                Extract(raw), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (d is not null) return d;
        }
        catch (JsonException) { }
        return new DigestDto { Summary = raw.Trim() };
    }

    private static RollupResult ParseRollup(string raw)
    {
        try
        {
            var d = JsonSerializer.Deserialize<RollupResult>(
                Extract(raw), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (d is not null) return d;
        }
        catch (JsonException) { }
        return new RollupResult { Summary = raw.Trim() };
    }

    private static string Extract(string raw)
    {
        var s = raw.IndexOf('{');
        var e = raw.LastIndexOf('}');
        return (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
    }

    private record MsgRow(DateTime Date, string Author, string Text);

    private class DigestDto
    {
        public string Summary { get; set; } = "";
        public string Mood { get; set; } = "";
        public List<string> Events { get; set; } = [];
    }
}
