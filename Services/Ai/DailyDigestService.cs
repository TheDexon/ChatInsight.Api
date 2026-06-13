using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Rollup;
using ChatInsight.Api.Data;
using ChatInsight.Api.Services.Text;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Посуточный (по-кусочный) анализ ВСЕГО чата: режет историю на куски, по каждому
/// делает AI-выжимку (что обсуждали, настроение, события), затем собирает все
/// выжимки в единую хронологию. Видит 100% переписки, а не выборку.
/// </summary>
public class DailyDigestService
{
    private readonly ChatInsightDbContext _db;
    private readonly OllamaClient _ollama;

    /// <summary>Размер куска в сообщениях (все типы; куски ровные независимо от рваных дней).</summary>
    private const int ChunkSize = 250;

    /// <summary>Максимум строк сообщений в один промпт-выжимку (защита от переполнения контекста).</summary>
    private const int MaxLinesPerChunk = 220;

    public DailyDigestService(ChatInsightDbContext db, OllamaClient ollama)
    {
        _db = db;
        _ollama = ollama;
    }

    public async Task<RollupResult> AnalyzeAsync(
        Guid chatId,
        Func<int, int, Task>? onProgress,
        CancellationToken ct = default)
    {
        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId &&
                        m.Type == "message" &&
                        m.Author != null)
            .OrderBy(m => m.Date)
            .Select(m => new MsgRow(m.Date, m.Author!, m.Text))
            .ToListAsync(ct);

        if (messages.Count < 10)
            return new RollupResult { Summary = "Недостаточно данных для анализа." };

        var chunks = Chunk(messages, ChunkSize);
        var digests = new List<PeriodDigest>();

        int total = chunks.Count;
        int done = 0;

        foreach (var chunk in chunks)
        {
            var digest = await DigestChunkAsync(chunk, ct);
            if (digest is not null)
                digests.Add(digest);

            done++;
            if (onProgress is not null)
                await onProgress(done, total);
        }

        if (digests.Count == 0)
            return new RollupResult { Summary = "Не удалось построить выжимки." };

        var rollup = await RollupAsync(digests, ct);
        rollup.DigestCount = digests.Count;
        rollup.Model = _ollama.Model;
        return rollup;
    }

    // ---------- фаза 1: выжимка одного куска ----------

    private async Task<PeriodDigest?> DigestChunkAsync(
        List<MsgRow> chunk, CancellationToken ct)
    {
        var meaningful = chunk
            .Where(m => MeaningfulTextFilter.IsMeaningful(m.Text))
            .ToList();

        // медиа/вложения = сообщения без текста (стикеры, гс, фото, file not included)
        var mediaCount = chunk.Count(m => string.IsNullOrWhiteSpace(m.Text));

        if (meaningful.Count == 0)
            return null; // кусок целиком из медиа/мусора — пропускаем

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
            if (t.Length > 140) t = t[..140] + "…";
            sb.AppendLine($"{m.Author}: {t}");
        }

        var system =
            AiPrompts.IronyNote +
            "Ты — аналитик переписки. Это кусок диалога за период. Сделай краткую " +
            "выжимку. Заполни поля: summary (1-2 предложения О ЧЁМ говорили), " +
            "mood (настроение периода одним-двумя словами), events (0-3 реальных " +
            "события/факта, если были; пустой список, если только болтовня). " +
            "ВАЖНО: бессмысленный набор букв (типа «аоаоао», «хавхав», «пзхпзх») — " +
            "это мусор, игнорируй его. Медиа без текста не выдумывай. Не принимай " +
            "шутки за реальные события. Пиши по-русски.";

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

    // ---------- фаза 2: сборка всех выжимок в итог ----------

    private async Task<RollupResult> RollupAsync(
        List<PeriodDigest> digests, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Ниже — выжимки по последовательным периодам всей переписки " +
                      "(в хронологическом порядке). Собери из них цельную картину.");
        sb.AppendLine();

        foreach (var d in digests)
        {
            sb.AppendLine($"[{d.FromDate:dd.MM.yy}—{d.ToDate:dd.MM.yy}] " +
                          $"({d.Mood}) {d.Summary}");
            foreach (var e in d.Events.Where(e => !string.IsNullOrWhiteSpace(e)))
                sb.AppendLine($"  • {e}");
        }

        var system =
            AiPrompts.IronyNote +
            "Ты — биограф-аналитик. На вход — выжимки по всем периодам переписки " +
            "(полный охват). Построй ИТОГ: summary (4-6 предложений: как развивалось " +
            "общение и отношения за весь срок, ключевые перемены) и timeline — " +
            "хронология из 5-10 главных событий: period (например «01.2026» или " +
            "«01—02.2026»), title (коротко), description (1-2 предложения). " +
            "Бери только реальные события, не повторяйся, не выдумывай из шуток. " +
            "Пиши по-русски.";

        var raw = await _ollama.ChatAsync(system, sb.ToString(), RollupSchema(), ct);
        return ParseRollup(raw);
    }

    // ---------- разбиение ----------

    private static List<List<MsgRow>> Chunk(List<MsgRow> msgs, int size)
    {
        var result = new List<List<MsgRow>>();
        for (int i = 0; i < msgs.Count; i += size)
            result.Add(msgs.GetRange(i, Math.Min(size, msgs.Count - i)));
        return result;
    }

    // ---------- схемы ----------

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

    // ---------- парсинг ----------

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
