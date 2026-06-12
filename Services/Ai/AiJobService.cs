using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Ставит AI-задачу в очередь. Если для чата+типа уже есть незавершённая
/// задача — возвращает её (не плодим дубли, не спамим модель).
/// </summary>
public class AiJobService
{
    private readonly ChatInsightDbContext _db;
    private readonly AiJobQueue _queue;

    public AiJobService(ChatInsightDbContext db, AiJobQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    public async Task<Guid> EnqueueAsync(
        Guid chatId,
        string jobType,
        CancellationToken ct = default)
    {
        var existing = await _db.AiJobs
            .Where(j => j.ChatId == chatId &&
                        j.JobType == jobType &&
                        (j.Status == AiJobStatus.Pending ||
                         j.Status == AiJobStatus.Running))
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing.Id;

        var job = new AiJob
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            JobType = jobType,
            Status = AiJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.AiJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(job.Id, ct);
        return job.Id;
    }
}
