using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ChatInsight.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ChatInsight.Api.Services.Ai;

/// <summary>Тонкий клиент к Ollama /api/chat. Бросает OllamaUnavailableException,
/// если сервис не отвечает — контроллер превратит это в понятную 503.</summary>
public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;

    public OllamaClient(HttpClient http, IOptions<OllamaOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public string Model => _options.Model;

    /// <summary>
    /// Шлёт system+user. Если schema != null — передаёт её в format,
    /// и движок ГАРАНТИРУЕТ ответ ровно по этой JSON Schema (все поля, типы).
    /// </summary>
    public async Task<string> ChatAsync(
        string systemPrompt,
        string userPrompt,
        JsonNode? schema = null,
        CancellationToken ct = default)
    {
        var request = new JsonObject
        {
            ["model"] = _options.Model,
            ["stream"] = false,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                new JsonObject { ["role"] = "user", ["content"] = userPrompt }
            },
            ["options"] = new JsonObject
            {
                ["temperature"] = 0.3,
                ["num_ctx"] = 8192
            }
        };

        if (schema is not null)
            request["format"] = schema;

        var json = request.ToJsonString();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/api/chat", content, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new OllamaUnavailableException(
                "Не удалось подключиться к Ollama. Запущен ли сервис?", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new OllamaUnavailableException(
                "Ollama не ответила за отведённое время.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new OllamaUnavailableException(
                $"Ollama вернула {(int)response.StatusCode}: {body}");
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<OllamaChatResponse>(payload);

        return parsed?.Message?.Content ?? "";
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")] public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = "";
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }
}

/// <summary>Ollama недоступна / не ответила.</summary>
public class OllamaUnavailableException : Exception
{
    public OllamaUnavailableException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}
