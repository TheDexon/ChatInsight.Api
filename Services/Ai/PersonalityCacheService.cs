using ChatInsight.Api.Analysis.Personality;
using ChatInsight.Api.Data;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Кэш AI-портретов. Считаем один раз на чат (по всем участникам),
/// сохраняем в БД, дальше отдаём мгновенно. refresh=true — пересчёт.
/// </summary>
public class PersonalityCacheService
{
    private readonly ChatInsightDbContext _db;
    private readonly PersonalityService _svc;

    public PersonalityCacheService(
        ChatInsightDbContext db,
        PersonalityService svc)
    {
        _db = db;
        _svc = svc;
    }

    public async Task<(List<PersonalityProfile> Profiles, bool FromCache)> GetOrCreateAsync(
        ChatAnalysisContext context,
        Guid chatId,
        bool refresh,
        CancellationToken ct = default)
    {
        var existing = await _db.Personalities
            .AsNoTracking()
            .Where(x => x.ChatId == chatId)
            .ToListAsync(ct);

        var participants = context.Participants;

        var covers = participants.Count > 0 &&
            participants.All(p => existing.Any(e => e.Participant == p));

        if (existing.Count > 0 && covers && !refresh)
            return (existing.Select(ToDto).ToList(), true);

        // считаем заново (по всем участникам) и заменяем
        var generated = await _svc.AnalyzeAsync(context, ct);

        var old = await _db.Personalities
            .Where(x => x.ChatId == chatId)
            .ToListAsync(ct);

        if (old.Count > 0)
            _db.Personalities.RemoveRange(old);

        foreach (var p in generated)
        {
            _db.Personalities.Add(new PersonalityRecord
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                Participant = p.Participant,
                Summary = p.Summary,
                CommunicationStyle = p.CommunicationStyle,
                Traits = p.Traits,
                Model = p.Model,
                GeneratedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        return (generated, false);
    }

    private static PersonalityProfile ToDto(PersonalityRecord r) => new()
    {
        Participant = r.Participant,
        Summary = r.Summary,
        CommunicationStyle = r.CommunicationStyle,
        Traits = r.Traits,
        Model = r.Model
    };
}
