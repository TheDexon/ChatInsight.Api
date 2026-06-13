using Pgvector;

namespace ChatInsight.Api.Models.Domain;

/// <summary>Эмбеддинг сообщения (один на сообщение). Id == Message.Id.</summary>
public class MessageEmbedding
{
    public long Id { get; set; }
    public Guid ChatId { get; set; }

    /// <summary>Вектор сообщения (nomic-embed-text → 768).</summary>
    public Vector Embedding { get; set; } = null!;
}
