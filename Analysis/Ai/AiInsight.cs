namespace ChatInsight.Api.Analysis.Ai;

/// <summary>Человеческие выводы по переписке, сгенерированные моделью.</summary>
public class AiInsight
{
    /// <summary>Краткое резюме отношений/общения.</summary>
    public string Summary { get; set; } = "";

    /// <summary>Эмоциональный портрет диалога.</summary>
    public string EmotionalTone { get; set; } = "";

    /// <summary>Основные темы общения.</summary>
    public List<string> Topics { get; set; } = [];

    /// <summary>Как менялось общение во времени / ключевые наблюдения.</summary>
    public List<string> Dynamics { get; set; } = [];

    /// <summary>Модель, которой сгенерировано (для прозрачности).</summary>
    public string Model { get; set; } = "";
}
