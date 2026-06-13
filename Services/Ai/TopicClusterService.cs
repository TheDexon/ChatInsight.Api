using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatInsight.Api.Analysis.Clusters;
using ChatInsight.Api.Data;
using ChatInsight.Api.Services.Text;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Services.Ai;

/// <summary>
/// Кластеризует сообщения по смыслу (k-means над эмбеддингами) и просит модель
/// назвать каждый кластер. Если эмбеддингов нет — строит их.
/// </summary>
public class TopicClusterService
{
    private readonly ChatInsightDbContext _db;
    private readonly EmbeddingService _embedding;
    private readonly OllamaClient _ollama;

    public TopicClusterService(
        ChatInsightDbContext db,
        EmbeddingService embedding,
        OllamaClient ollama)
    {
        _db = db;
        _embedding = embedding;
        _ollama = ollama;
    }

    public async Task<TopicClusterResult> AnalyzeAsync(
        Guid chatId, CancellationToken ct = default)
    {
        // строит недостающие эмбеддинги и подчищает мусорные (фильтр внутри BuildAsync)
        await _embedding.BuildAsync(chatId, ct);

        var rows = await _db.MessageEmbeddings
            .Where(e => e.ChatId == chatId)
            .Join(_db.Messages, e => e.Id, m => m.Id,
                (e, m) => new { e.Embedding, m.Text })
            .ToListAsync(ct);

        // двойная защита: даже если в индексе остался мусор — в кластеризацию не пускаем
        rows = rows
            .Where(r => MeaningfulTextFilter.IsMeaningful(r.Text))
            .ToList();
        if (rows.Count < 4)
            return new TopicClusterResult { Summary = "Недостаточно данных для кластеризации." };

        var vectors = rows.Select(r => KMeans.Normalize(r.Embedding.ToArray())).ToList();
        var texts = rows.Select(r => r.Text).ToList();

        // число кластеров — эвристика по объёму
        int k = Math.Clamp(rows.Count / 60, 3, 8);

        var (assign, centroids) = KMeans.Cluster(vectors, k, 25, 42);

        var clusters = new List<TopicCluster>();
        var repsForLabeling = new List<(int Index, List<string> Reps)>();

        for (int c = 0; c < centroids.Length; c++)
        {
            var members = Enumerable.Range(0, vectors.Count)
                .Where(i => assign[i] == c)
                .OrderBy(i => KMeans.Dist(vectors[i], centroids[c]))
                .ToList();

            if (members.Count == 0) continue;

            var samples = members
                .Select(i => texts[i])
                .Where(t => t.Length is > 3 and < 200 && MeaningfulTextFilter.IsMeaningful(t))
                .Distinct()
                .Take(3)
                .ToList();

            var reps = members
                .Select(i => texts[i])
                .Where(t => t.Length is > 3 and < 200 && MeaningfulTextFilter.IsMeaningful(t))
                .Distinct()
                .Take(6)
                .ToList();

            clusters.Add(new TopicCluster
            {
                Size = members.Count,
                Share = (int)Math.Round(members.Count * 100.0 / vectors.Count),
                Samples = samples
            });

            repsForLabeling.Add((clusters.Count - 1, reps));
        }

        var summary = await LabelClustersAsync(clusters, repsForLabeling, ct);

        Deduplicate(ordered: clusters);
        var ordered = clusters.OrderByDescending(x => x.Size).ToList();

        return new TopicClusterResult
        {
            Clusters = ordered,
            Summary = summary,
            Model = _ollama.Model
        };
    }

    /// <summary>Разводит одинаковые названия кластеров (суффикс), чтобы не дублировались.</summary>
    private static void Deduplicate(List<TopicCluster> ordered)
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in ordered)
        {
            var label = string.IsNullOrWhiteSpace(c.Label) ? "Тема" : c.Label.Trim();
            if (seen.TryGetValue(label, out var n))
            {
                seen[label] = n + 1;
                c.Label = $"{label} ({n + 1})";
            }
            else
            {
                seen[label] = 1;
                c.Label = label;
            }
        }
    }

    private async Task<string> LabelClustersAsync(
        List<TopicCluster> clusters,
        List<(int Index, List<string> Reps)> reps,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Даны группы сообщений из переписки. Назови каждую группу " +
                      "короткой темой (2-4 слова).");
        foreach (var (index, messages) in reps)
        {
            sb.AppendLine($"Группа {index}:");
            foreach (var m in messages)
                sb.AppendLine($"  - {m}");
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["clusters"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["index"] = new JsonObject { ["type"] = "integer" },
                            ["label"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray { "index", "label" }
                    }
                },
                ["summary"] = new JsonObject { ["type"] = "string" }
            },
            ["required"] = new JsonArray { "clusters", "summary" }
        };

        var system =
            AiPrompts.IronyNote +
            "Ты — аналитик. Для каждой группы сообщений дай короткое осмысленное " +
            "название темы (2-4 слова) по-русски. Не давай одинаковых названий разным " +
            "группам. summary — 1-2 предложения об основных темах общения.";

        try
        {
            var raw = await _ollama.ChatAsync(system, sb.ToString(), schema, ct);
            return ApplyLabels(clusters, raw);
        }
        catch (OllamaUnavailableException)
        {
            // без модели — оставляем кластеры без названий, но не падаем
            for (int i = 0; i < clusters.Count; i++)
                if (string.IsNullOrEmpty(clusters[i].Label))
                    clusters[i].Label = $"Тема {i + 1}";
            return "";
        }
    }

    private static string ApplyLabels(List<TopicCluster> clusters, string raw)
    {
        var json = Extract(raw);
        try
        {
            var parsed = JsonSerializer.Deserialize<LabelsDto>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is not null)
            {
                foreach (var c in parsed.Clusters)
                    if (c.Index >= 0 && c.Index < clusters.Count)
                        clusters[c.Index].Label = c.Label;

                for (int i = 0; i < clusters.Count; i++)
                    if (string.IsNullOrWhiteSpace(clusters[i].Label))
                        clusters[i].Label = $"Тема {i + 1}";

                return parsed.Summary;
            }
        }
        catch (JsonException) { }

        for (int i = 0; i < clusters.Count; i++)
            clusters[i].Label = $"Тема {i + 1}";
        return "";
    }

    private static string Extract(string raw)
    {
        var s = raw.IndexOf('{');
        var e = raw.LastIndexOf('}');
        return (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : raw;
    }

    private class LabelsDto
    {
        public List<LabelItem> Clusters { get; set; } = [];
        public string Summary { get; set; } = "";
    }

    private class LabelItem
    {
        public int Index { get; set; }
        public string Label { get; set; } = "";
    }
}
