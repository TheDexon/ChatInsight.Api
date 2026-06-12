using System.Text.Json;
using ChatInsight.Api.Analysis.Evolution;
using ChatInsight.Api.Data;
using ChatInsight.Api.Domain;
using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>Кэш анализа эволюции личности: считаем раз на чат, дальше из БД.</summary>
public class PersonalityEvolutionCacheService
{
    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ChatInsightDbContext _db;
    private readonly PersonalityEvolutionService _svc;

    public PersonalityEvolutionCacheService(
        ChatInsightDbContext db,
        PersonalityEvolutionService svc)
    {
        _db = db;
        _svc = svc;
    }

    public async Task<(PersonalityEvolutionResult Result, bool FromCache)> GetOrCreateAsync(
        ChatAnalysisContext context,
        Guid chatId,
        bool refresh,
        CancellationToken ct = default)
    {
        var existing = await _db.PersonalityEvolutions
            .FirstOrDefaultAsync(x => x.ChatId == chatId, ct);

        if (existing is not null && !refresh)
            return (ToDto(existing), true);

        var generated = await _svc.AnalyzeAsync(context, null, ct);
        var resultJson = JsonSerializer.Serialize(generated);

        if (existing is null)
        {
            _db.PersonalityEvolutions.Add(new PersonalityEvolutionRecord
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

    private static PersonalityEvolutionResult ToDto(PersonalityEvolutionRecord r)
    {
        try
        {
            return JsonSerializer.Deserialize<PersonalityEvolutionResult>(r.ResultJson, Opts)
                ?? new PersonalityEvolutionResult();
        }
        catch (JsonException)
        {
            return new PersonalityEvolutionResult();
        }
    }
}
