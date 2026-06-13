using System.Text.Json;
using ChatInsight.Api.Analysis.Rollup;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

public class RollupCacheService
{
    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ChatInsightDbContext _db;
    private readonly DigestService _digest;

    public RollupCacheService(ChatInsightDbContext db, DigestService digest)
    {
        _db = db;
        _digest = digest;
    }

    public async Task<(RollupResult Result, bool FromCache)> GetOrCreateAsync(
        Guid chatId, bool refresh, CancellationToken ct = default)
    {
        var existing = await _db.Rollups.FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (existing is not null && !refresh)
            return (ToDto(existing), true);

        // выжимки уже построены воркером (с прогрессом); тут просто берём их и собираем
        var digests = await _digest.GetOrBuildAsync(chatId, null, ct);
        var generated = await _digest.RollupAsync(digests, ct);
        var resultJson = JsonSerializer.Serialize(generated);

        if (existing is null)
        {
            _db.Rollups.Add(new RollupRecord
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                ResultJson = resultJson,
                DigestCount = generated.DigestCount,
                Model = generated.Model,
                GeneratedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.ResultJson = resultJson;
            existing.DigestCount = generated.DigestCount;
            existing.Model = generated.Model;
            existing.GeneratedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return (generated, false);
    }

    private static RollupResult ToDto(RollupRecord r)
    {
        try { return JsonSerializer.Deserialize<RollupResult>(r.ResultJson, Opts) ?? new(); }
        catch (JsonException) { return new RollupResult(); }
    }
}
