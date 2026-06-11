namespace ChatInsight.Api.Configuration;

/// <summary>Настройки подключения к Ollama (секция "Ollama" в appsettings).</summary>
public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "qwen2.5:14b";

    /// <summary>Сколько секунд ждём ответ модели (LLM думает дольше).</summary>
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>Сколько сообщений-сэмплов класть в промпт (контекст ограничен).</summary>
    public int SampleMessages { get; set; } = 200;
}
