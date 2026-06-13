using ChatInsight.Api.Analysis.Search;
using ChatInsight.Api.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>Семантический поиск по сообщениям чата через pgvector.</summary>
public class SemanticSearchService
{
    private readonly ChatInsightDbContext _db;
    private readonly OllamaEmbeddingClient _embed;

    public SemanticSearchService(
        ChatInsightDbContext db,
        OllamaEmbeddingClient embed)
    {
        _db = db;
        _embed = embed;
    }

    public async Task<List<SearchHit>> SearchAsync(
        Guid chatId, string query, int limit, CancellationToken ct = default)
    {
        // запрос тоже обрезаем под лимит embed-модели
        var qv = new Vector(await _embed.EmbedAsync(EmbeddingService.Trim(query), ct));

        var hits = await _db.MessageEmbeddings
            .Where(e => e.ChatId == chatId)
            .OrderBy(e => e.Embedding.CosineDistance(qv))
            .Take(limit)
            .Join(_db.Messages,
                e => e.Id,
                m => m.Id,
                (e, m) => new
                {
                    m.Date,
                    m.Author,
                    m.Text,
                    Distance = e.Embedding.CosineDistance(qv)
                })
            .ToListAsync(ct);

        return hits.Select(h => new SearchHit
        {
            Date = h.Date,
            Author = h.Author,
            Text = h.Text,
            Score = Math.Round(1 - h.Distance, 3)
        }).ToList();
    }

    public async Task<int> CountAsync(Guid chatId, CancellationToken ct = default) =>
        await _db.MessageEmbeddings.CountAsync(e => e.ChatId == chatId, ct);
}
