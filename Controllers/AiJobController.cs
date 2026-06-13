using System.Text.Json.Nodes;
using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Services.Ai;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Controllers;

[ApiController]
[Route("api")]
public class AiJobController : ControllerBase
{
    private readonly AiJobService _jobs;
    private readonly ChatInsightDbContext _db;

    public AiJobController(AiJobService jobs, ChatInsightDbContext db)
    {
        _jobs = jobs;
        _db = db;
    }

    [HttpPost("chats/{id:guid}/insights/async")]
    public Task<IActionResult> StartInsights(Guid id, [FromQuery] bool refresh = false, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Insights, refresh, ct);

    [HttpPost("chats/{id:guid}/personality/async")]
    public Task<IActionResult> StartPersonality(Guid id, [FromQuery] bool refresh = false, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Personality, refresh, ct);

    [HttpPost("chats/{id:guid}/lifetimeline/async")]
    public Task<IActionResult> StartTimeline(Guid id, [FromQuery] bool refresh = false, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Timeline, refresh, ct);

    [HttpPost("chats/{id:guid}/evolution/async")]
    public Task<IActionResult> StartEvolution(Guid id, [FromQuery] bool refresh = false, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Evolution, refresh, ct);

    [HttpPost("chats/{id:guid}/embeddings/async")]
    public Task<IActionResult> StartEmbeddings(Guid id, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Embeddings, false, ct);

    [HttpPost("chats/{id:guid}/clusters/async")]
    public Task<IActionResult> StartClusters(Guid id, [FromQuery] bool refresh = false, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Clusters, refresh, ct);

    [HttpPost("chats/{id:guid}/rollup/async")]
    public Task<IActionResult> StartRollup(Guid id, [FromQuery] bool refresh = false, CancellationToken ct = default) =>
        StartAsync(id, AiJobType.Rollup, refresh, ct);

    private async Task<IActionResult> StartAsync(
        Guid id, string jobType, bool refresh, CancellationToken ct)
    {
        if (refresh)
            await ClearCacheAsync(id, jobType, ct);

        var jobId = await _jobs.EnqueueAsync(id, jobType, ct);
        return Ok(new { jobId });
    }

    /// <summary>Сбрасывает кэш результата нужного типа — следующая задача пересчитает.</summary>
    private async Task ClearCacheAsync(Guid chatId, string jobType, CancellationToken ct)
    {
        switch (jobType)
        {
            case AiJobType.Insights:
                await _db.Insights.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                break;
            case AiJobType.Personality:
                await _db.Personalities.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                break;
            case AiJobType.Timeline:
                await _db.LifeTimelines.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                break;
            case AiJobType.Evolution:
                await _db.PersonalityEvolutions.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                break;
            case AiJobType.Clusters:
                await _db.TopicClusters.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                break;
            case AiJobType.Rollup:
                // полная пересборка: сбрасываем выжимки и всё, что на них построено
                await _db.PeriodDigests.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                await _db.Rollups.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                await _db.Insights.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                await _db.LifeTimelines.Where(x => x.ChatId == chatId).ExecuteDeleteAsync(ct);
                break;
        }
    }

    [HttpGet("jobs/{jobId:guid}")]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken ct)
    {
        var job = await _db.AiJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return NotFound();

        JsonNode? result =
            job.Status == AiJobStatus.Done && job.ResultJson is not null
                ? JsonNode.Parse(job.ResultJson)
                : null;

        return Ok(new { id = job.Id, type = job.JobType, status = job.Status, result, error = job.Error, progress = job.Progress });
    }
}
