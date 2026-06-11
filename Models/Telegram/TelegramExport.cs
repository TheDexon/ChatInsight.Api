using System.Text.Json.Serialization;

namespace ChatInsight.Api.Models.Telegram;

public class TelegramExport
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>Стабильный id чата из Telegram — по нему узнаём «тот же» чат.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("messages")]
    public List<TelegramMessage> Messages { get; set; } = [];
}
