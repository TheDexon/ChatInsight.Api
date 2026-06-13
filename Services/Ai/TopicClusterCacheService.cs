using System.Text.Json;
using ChatInsight.Api.Analysis.Clusters;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

public class TopicClusterCacheService
{
    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ChatInsightDbContext _db;
    private readonly TopicClusterService _svc;

    public TopicClusterCacheService(ChatInsightDbContext db, TopicClusterService svc)
    {
        _db = db;
        _svc = svc;
    }

    public async Task<(TopicClusterResult Result, bool FromCache)> GetOrCreateAsync(
        Guid chatId, bool refresh, CancellationToken ct = default)
    {
        var existing = await _db.TopicClusters
            .FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (existing is not null && !refresh)
            return (ToDto(existing), true);

        var generated = await _svc.AnalyzeAsync(chatId, ct);
        var resultJson = JsonSerializer.Serialize(generated);

        if (existing is null)
        {
            _db.TopicClusters.Add(new TopicClusterRecord
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                ResultJson = resultJson,
                Model = generated.Model,
                GeneratedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.ResultJson = resultJson;
            existing.Model = generated.Model;
            existing.GeneratedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return (generated, false);
    }

    private static TopicClusterResult ToDto(TopicClusterRecord r)
    {
        try
        {
            return JsonSerializer.Deserialize<TopicClusterResult>(r.ResultJson, Opts)
                ?? new TopicClusterResult();
        }
        catch (JsonException)
        {
            return new TopicClusterResult();
        }
    }
}
