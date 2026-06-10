namespace ChatInsight.Api.Analysis.Emotion;

public class EmotionStatistics
{
    public int PositiveMessages { get; set; }

    public int NegativeMessages { get; set; }

    public int ProfanityMessages { get; set; }

    public double ToxicityScore { get; set; }
}