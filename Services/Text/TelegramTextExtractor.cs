using System.Text;
using System.Text.Json;

namespace ChatInsight.Api.Services.Text;

public class TelegramTextExtractor
{
    public string Extract(object? text)
    {
        if (text == null)
            return "";

        if (text is string str)
            return str;

        if (text is JsonElement element)
        {
            return ExtractFromJsonElement(element);
        }

        return text.ToString() ?? "";
    }

    private string ExtractFromJsonElement(
        JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? "";

            case JsonValueKind.Array:

                var sb = new StringBuilder();

                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(item.GetString());
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        if (item.TryGetProperty(
                            "text",
                            out var textProperty))
                        {
                            sb.Append(
                                textProperty.GetString());
                        }
                    }
                }

                return sb.ToString();

            default:
                return "";
        }
    }
}