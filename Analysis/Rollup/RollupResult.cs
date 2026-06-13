namespace ChatInsight.Api.Analysis.Rollup;

/// <summary>Событие итоговой хронологии, собранной из всех выжимок.</summary>
public class RollupEvent
{
    public string Period { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>Итог посуточного (по-кусочного) анализа всего чата.</summary>
public class RollupResult
{
    public string Summary { get; set; } = "";
    public List<RollupEvent> Timeline { get; set; } = [];

    /// <summary>Сколько кусков (периодов) обработано.</summary>
    public int DigestCount { get; set; }
    public string Model { get; set; } = "";
}

/// <summary>Внутренняя выжимка одного куска (периода). В БД не хранится (v1).</summary>
public class PeriodDigest
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int MessageCount { get; set; }
    public string Summary { get; set; } = "";
    public string Mood { get; set; } = "";
    public List<string> Events { get; set; } = [];
}
