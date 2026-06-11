using ChatInsight.Api.Analysis.Ai;
using ChatInsight.Api.Data;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Кэш AI-инсайтов: считаем один раз через AiInsightService, сохраняем в БД,
/// дальше отдаём мгновенно. refresh=true — принудительный пересчёт.
/// </summary>
public class AiInsightCacheService
{
    private readonly ChatInsightDbContext _db;
    private readonly AiInsightService _ai;

    public AiInsightCacheService(
        ChatInsightDbContext db,
        AiInsightService ai)
    {
        _db = db;
        _ai = ai;
    }

    /// <summary>Только чтение из кэша. null — если ещё не считали (модель НЕ зовётся).</summary>
    public async Task<AiInsight?> GetCachedAsync(
        Guid chatId,
        CancellationToken ct = default)
    {
        var existing = await _db.Insights
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        return existing is null ? null : ToDto(existing);
    }

    /// <returns>(insight, fromCache)</returns>
    public async Task<(AiInsight Insight, bool FromCache)> GetOrCreateAsync(
        ChatAnalysisContext context,
        Guid chatId,
        bool refresh,
        CancellationToken ct = default)
    {
        var existing = await _db.Insights
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (existing is not null && !refresh)
            return (ToDto(existing), true);

        var generated = await _ai.AnalyzeAsync(context, ct);

        if (existing is null)
        {
            _db.Insights.Add(new ChatInsightRecord
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                Summary = generated.Summary,
                EmotionalTone = generated.EmotionalTone,
                Topics = generated.Topics,
                Dynamics = generated.Dynamics,
                Model = generated.Model,
                GeneratedAt = DateTime.UtcNow
            });
        }
        else
        {
            var tracked = await _db.Insights
                .FirstAsync(x => x.Id == existing.Id, ct);

            tracked.Summary = generated.Summary;
            tracked.EmotionalTone = generated.EmotionalTone;
            tracked.Topics = generated.Topics;
            tracked.Dynamics = generated.Dynamics;
            tracked.Model = generated.Model;
            tracked.GeneratedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return (generated, false);
    }

    private static AiInsight ToDto(ChatInsightRecord r) => new()
    {
        Summary = r.Summary,
        EmotionalTone = r.EmotionalTone,
        Topics = r.Topics,
        Dynamics = r.Dynamics,
        Model = r.Model
    };
}
