using System.Text.Json;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Services.Analytics;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

public class AiJobWorker : BackgroundService
{
    private readonly AiJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiJobWorker> _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AiJobWorker(AiJobQueue queue, IServiceScopeFactory scopeFactory, ILogger<AiJobWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeuePendingAsync(stoppingToken);
        await foreach (var jobId in _queue.ReadAllAsync(stoppingToken))
        {
            try { await ProcessAsync(jobId, stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "AI-задача {JobId} упала", jobId); }
        }
    }

    private async Task RequeuePendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatInsightDbContext>();
        var pending = await db.AiJobs
            .Where(j => j.Status == AiJobStatus.Pending || j.Status == AiJobStatus.Running)
            .ToListAsync(ct);
        foreach (var j in pending)
        {
            j.Status = AiJobStatus.Pending;
            await _queue.EnqueueAsync(j.Id, ct);
        }
        if (pending.Count > 0) await db.SaveChangesAsync(ct);
    }

    // типы анализа, которые строятся поверх выжимок (полный охват)
    private static bool UsesDigests(string jobType) =>
        jobType is AiJobType.Insights or AiJobType.Timeline or AiJobType.Rollup;

    private async Task ProcessAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ChatInsightDbContext>();
        var loader = sp.GetRequiredService<ChatContextLoader>();

        var job = await db.AiJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return;

        job.Status = AiJobStatus.Running;
        await db.SaveChangesAsync(ct);

        try
        {
            // общий фундамент: строим выжимки один раз, с прогрессом «N/M периодов»
            if (UsesDigests(job.JobType))
            {
                await sp.GetRequiredService<DigestService>().GetOrBuildAsync(
                    job.ChatId,
                    async (done, total) =>
                    {
                        job.Progress = $"{done}/{total}";
                        await db.SaveChangesAsync(ct);
                    },
                    ct);
            }

            if (job.JobType == AiJobType.Embeddings)
            {
                var n = await sp.GetRequiredService<EmbeddingService>().BuildAsync(job.ChatId, ct);
                job.ResultJson = JsonSerializer.Serialize(new { built = n }, JsonOpts);
            }
            else if (job.JobType == AiJobType.Clusters)
            {
                var (res, _) = await sp.GetRequiredService<TopicClusterCacheService>()
                    .GetOrCreateAsync(job.ChatId, false, ct);
                job.ResultJson = JsonSerializer.Serialize(res, JsonOpts);
            }
            else if (job.JobType == AiJobType.Rollup)
            {
                var (res, _) = await sp.GetRequiredService<RollupCacheService>()
                    .GetOrCreateAsync(job.ChatId, false, ct);
                job.ResultJson = JsonSerializer.Serialize(res, JsonOpts);
            }
            else
            {
                var context = await loader.LoadAsync(job.ChatId, ct);
                if (context is null || context.IsEmpty)
                    throw new InvalidOperationException("Чат не найден или пуст.");

                job.ResultJson = job.JobType switch
                {
                    AiJobType.Insights => JsonSerializer.Serialize(
                        (await sp.GetRequiredService<AiInsightCacheService>()
                            .GetOrCreateAsync(context, job.ChatId, false, ct)).Insight, JsonOpts),
                    AiJobType.Personality => JsonSerializer.Serialize(
                        (await sp.GetRequiredService<PersonalityCacheService>()
                            .GetOrCreateAsync(context, job.ChatId, false, ct)).Profiles, JsonOpts),
                    AiJobType.Timeline => JsonSerializer.Serialize(
                        (await sp.GetRequiredService<LifeTimelineCacheService>()
                            .GetOrCreateAsync(context, job.ChatId, false, ct)).Result, JsonOpts),
                    AiJobType.Evolution => JsonSerializer.Serialize(
                        (await sp.GetRequiredService<PersonalityEvolutionCacheService>()
                            .GetOrCreateAsync(context, job.ChatId, false, ct)).Result, JsonOpts),
                    _ => throw new InvalidOperationException($"Неизвестный тип задачи: {job.JobType}")
                };
            }

            job.Status = AiJobStatus.Done;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            job.Status = AiJobStatus.Failed;
            job.Error = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            _logger.LogWarning("AI-задача {JobId} ошибка: {Error}", jobId, ex.Message);
        }
    }
}
