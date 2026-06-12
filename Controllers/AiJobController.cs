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
    public async Task<IActionResult> StartInsights(Guid id, CancellationToken ct) =>
        Ok(new { jobId = await _jobs.EnqueueAsync(id, AiJobType.Insights, ct) });

    [HttpPost("chats/{id:guid}/personality/async")]
    public async Task<IActionResult> StartPersonality(Guid id, CancellationToken ct) =>
        Ok(new { jobId = await _jobs.EnqueueAsync(id, AiJobType.Personality, ct) });

    [HttpPost("chats/{id:guid}/lifetimeline/async")]
    public async Task<IActionResult> StartTimeline(Guid id, CancellationToken ct) =>
        Ok(new { jobId = await _jobs.EnqueueAsync(id, AiJobType.Timeline, ct) });

    [HttpPost("chats/{id:guid}/evolution/async")]
    public async Task<IActionResult> StartEvolution(Guid id, CancellationToken ct) =>
        Ok(new { jobId = await _jobs.EnqueueAsync(id, AiJobType.Evolution, ct) });

    [HttpGet("jobs/{jobId:guid}")]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken ct)
    {
        var job = await _db.AiJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (job is null) return NotFound();

        JsonNode? result =
            job.Status == AiJobStatus.Done && job.ResultJson is not null
                ? JsonNode.Parse(job.ResultJson)
                : null;

        return Ok(new { id = job.Id, type = job.JobType, status = job.Status, result, error = job.Error });
    }
}
