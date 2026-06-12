using System.Threading.Channels;

namespace ChatInsight.Api.Services.Ai;

/// <summary>In-memory очередь id задач между запросом и фоновым воркером.</summary>
public class AiJobQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>();

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(jobId, ct);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
