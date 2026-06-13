namespace ChatInsight.Api.Analysis.Search;

public class SearchHit
{
    public DateTime Date { get; set; }
    public string? Author { get; set; }
    public string Text { get; set; } = "";

    /// <summary>Семантическая близость к запросу (1 — точное совпадение по смыслу).</summary>
    public double Score { get; set; }
}
