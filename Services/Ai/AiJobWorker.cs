using System.Text.Json;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Services.Analytics;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Фоновый обработчик AI-задач. Берёт id из очереди, в отдельном scope
/// гоняет модель через кэш-сервисы и пишет результат/ошибку в БД.
/// </summary>
public class AiJobWorker : BackgroundService
{
    private readonly AiJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiJobWorker> _logger;

    // camelCase — чтобы JSON в ResultJson совпадал с тем, что ждёт фронт
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AiJobWorker(
        AiJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AiJobWorker> logger)
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
            try
            {
                await ProcessAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI-задача {JobId} упала с ошибкой", jobId);
            }
        }
    }

    private async Task RequeuePendingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatInsightDbContext>();

        var pending = await db.AiJobs
            .Where(j => j.Status == AiJobStatus.Pending ||
                        j.Status == AiJobStatus.Running)
            .ToListAsync(ct);

        foreach (var j in pending)
        {
            j.Status = AiJobStatus.Pending;
            await _queue.EnqueueAsync(j.Id, ct);
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }

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
            var context = await loader.LoadAsync(job.ChatId, ct);
            if (context is null || context.IsEmpty)
                throw new InvalidOperationException("Чат не найден или пуст.");

            string resultJson = job.JobType switch
            {
                AiJobType.Insights => await InsightsJson(sp, context, job.ChatId, ct),
                AiJobType.Personality => await PersonalityJson(sp, context, job.ChatId, ct),
                AiJobType.Timeline => await TimelineJson(sp, context, job.ChatId, ct),
                _ => throw new InvalidOperationException($"Неизвестный тип задачи: {job.JobType}")
            };

            job.ResultJson = resultJson;
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
            _logger.LogWarning("AI-задача {JobId} завершилась ошибкой: {Error}", jobId, ex.Message);
        }
    }

    private static async Task<string> InsightsJson(
        IServiceProvider sp, Domain.ChatAnalysisContext ctx, Guid chatId, CancellationToken ct)
    {
        var cache = sp.GetRequiredService<AiInsightCacheService>();
        var (insight, _) = await cache.GetOrCreateAsync(ctx, chatId, false, ct);
        return JsonSerializer.Serialize(insight, JsonOpts);
    }

    private static async Task<string> PersonalityJson(
        IServiceProvider sp, Domain.ChatAnalysisContext ctx, Guid chatId, CancellationToken ct)
    {
        var cache = sp.GetRequiredService<PersonalityCacheService>();
        var (profiles, _) = await cache.GetOrCreateAsync(ctx, chatId, false, ct);
        return JsonSerializer.Serialize(profiles, JsonOpts);
    }

    private static async Task<string> TimelineJson(
        IServiceProvider sp, Domain.ChatAnalysisContext ctx, Guid chatId, CancellationToken ct)
    {
        var cache = sp.GetRequiredService<LifeTimelineCacheService>();
        var (result, _) = await cache.GetOrCreateAsync(ctx, chatId, false, ct);
        return JsonSerializer.Serialize(result, JsonOpts);
    }
}
