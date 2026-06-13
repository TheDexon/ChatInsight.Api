namespace ChatInsight.Api.Configuration;

public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1:8b";

    /// <summary>Модель эмбеддингов (для семантического поиска).</summary>
    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    public int TimeoutSeconds { get; set; } = 180;
    public int SampleMessages { get; set; } = 200;
}
