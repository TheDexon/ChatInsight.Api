using System.Text.Json.Serialization;

namespace ChatInsight.Api.Models.Telegram;

public class TelegramMessage
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("text")]
    public object? Text { get; set; }
}