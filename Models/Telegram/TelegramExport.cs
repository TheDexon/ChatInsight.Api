using System.Text.Json.Serialization;

namespace ChatInsight.Api.Models.Telegram;

public class TelegramExport
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("messages")]
    public List<TelegramMessage> Messages { get; set; } = [];
}