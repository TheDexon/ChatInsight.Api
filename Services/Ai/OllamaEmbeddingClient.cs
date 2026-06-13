using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatInsight.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ChatInsight.Api.Services.Ai;

/// <summary>Клиент к Ollama /api/embeddings — текст → вектор.</summary>
public class OllamaEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaEmbeddingClient(HttpClient http, IOptions<OllamaOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = _options.EmbeddingModel,
            prompt = text
        });

        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/api/embeddings", content, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new OllamaUnavailableException(
                "Не удалось подключиться к Ollama для эмбеддингов.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new OllamaUnavailableException(
                $"Ollama (embeddings) вернула {(int)response.StatusCode}: {err}");
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<EmbedResponse>(payload);

        return parsed?.Embedding ?? [];
    }

    private class EmbedResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}
