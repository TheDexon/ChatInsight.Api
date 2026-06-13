using ChatInsight.Api.Data;
using ChatInsight.Api.Models.Domain;
using ChatInsight.Api.Services.Text;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Строит эмбеддинги для ОСМЫСЛЕННЫХ сообщений чата. Мусор (рандом, повторы,
/// эмодзи) отсеивается. При повторном запуске удаляет ранее построенные
/// эмбеддинги, ставшие мусором, и сбрасывает кэш кластеров.
/// </summary>
public class EmbeddingService
{
    private readonly ChatInsightDbContext _db;
    private readonly OllamaEmbeddingClient _embed;

    private const int MaxChars = 2000;

    public EmbeddingService(ChatInsightDbContext db, OllamaEmbeddingClient embed)
    {
        _db = db;
        _embed = embed;
    }

    public async Task<int> BuildAsync(Guid chatId, CancellationToken ct = default)
    {
        var existingIds = (await _db.MessageEmbeddings
            .Where(e => e.ChatId == chatId)
            .Select(e => e.Id)
            .ToListAsync(ct))
            .ToHashSet();

        var messages = await _db.Messages
            .Where(m => m.ChatId == chatId &&
                        m.Type == "message" &&
                        m.Text != null &&
                        m.Text != "")
            .Select(m => new { m.Id, m.Text })
            .ToListAsync(ct);

        var meaningfulIds = messages
            .Where(m => MeaningfulTextFilter.IsMeaningful(m.Text))
            .Select(m => m.Id)
            .ToHashSet();

        bool changed = false;

        // 1) подчистить ранее построенные эмбеддинги, которые теперь считаются мусором
        var junkIds = existingIds.Where(id => !meaningfulIds.Contains(id)).ToList();
        if (junkIds.Count > 0)
        {
            var toRemove = await _db.MessageEmbeddings
                .Where(e => e.ChatId == chatId && junkIds.Contains(e.Id))
                .ToListAsync(ct);
            _db.MessageEmbeddings.RemoveRange(toRemove);
            await _db.SaveChangesAsync(ct);
            changed = true;
        }

        // 2) добавить недостающие осмысленные
        int added = 0;
        foreach (var m in messages)
        {
            if (existingIds.Contains(m.Id)) continue;
            if (!MeaningfulTextFilter.IsMeaningful(m.Text)) continue;

            var vec = await _embed.EmbedAsync(Trim(m.Text), ct);
            if (vec.Length == 0) continue;

            _db.MessageEmbeddings.Add(new MessageEmbedding
            {
                Id = m.Id,
                ChatId = chatId,
                Embedding = new Vector(vec)
            });

            added++;
            changed = true;

            if (added % 100 == 0)
                await _db.SaveChangesAsync(ct);
        }

        if (added % 100 != 0)
            await _db.SaveChangesAsync(ct);

        // 3) если состав эмбеддингов поменялся — сбросить кэш кластеров (пересчитается)
        if (changed)
        {
            var clusters = await _db.TopicClusters
                .Where(c => c.ChatId == chatId)
                .ToListAsync(ct);
            if (clusters.Count > 0)
            {
                _db.TopicClusters.RemoveRange(clusters);
                await _db.SaveChangesAsync(ct);
            }
        }

        return added;
    }

    public static string Trim(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim();
        return text.Length <= MaxChars ? text : text[..MaxChars];
    }
}
