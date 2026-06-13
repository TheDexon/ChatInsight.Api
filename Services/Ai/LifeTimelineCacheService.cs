using System.Text.Json;
using ChatInsight.Api.Analysis.LifeTimeline;
using ChatInsight.Api.Data;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>Кэш AI-хронологии: считаем раз на чат, дальше из БД.</summary>
public class LifeTimelineCacheService
{
    private readonly ChatInsightDbContext _db;
    private readonly LifeTimelineService _svc;

    public LifeTimelineCacheService(
        ChatInsightDbContext db,
        LifeTimelineService svc)
    {
        _db = db;
        _svc = svc;
    }

    public async Task<LifeTimelineResult> GetCachedAsync(
        Guid chatId, CancellationToken ct = default)
    {
        var rec = await _db.LifeTimelines
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        return rec is null ? new LifeTimelineResult() : ToDto(rec);
    }

    public async Task<(LifeTimelineResult Result, bool FromCache)> GetOrCreateAsync(
        ChatAnalysisContext context,
        Guid chatId,
        bool refresh,
        CancellationToken ct = default)
    {
        var existing = await _db.LifeTimelines
            .FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (existing is not null && !refresh)
            return (ToDto(existing), true);

        var generated = await _svc.AnalyzeAsync(chatId, ct);
        var eventsJson = JsonSerializer.Serialize(generated.Events);

        if (existing is null)
        {
            _db.LifeTimelines.Add(new LifeTimelineRecord
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                EventsJson = eventsJson,
                Summary = generated.Summary,
                Model = generated.Model,
                GeneratedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.EventsJson = eventsJson;
            existing.Summary = generated.Summary;
            existing.Model = generated.Model;
            existing.GeneratedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return (generated, false);
    }

    private static LifeTimelineResult ToDto(LifeTimelineRecord r)
    {
        List<LifeTimelineEvent> events;
        try
        {
            events = JsonSerializer.Deserialize<List<LifeTimelineEvent>>(
                r.EventsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? [];
        }
        catch (JsonException)
        {
            events = [];
        }

        return new LifeTimelineResult
        {
            Events = events,
            Summary = r.Summary,
            Model = r.Model
        };
    }
}
