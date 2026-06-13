namespace ChatInsight.Api.Analysis.Clusters;

public class TopicCluster
{
    public string Label { get; set; } = "";
    public int Size { get; set; }
    public int Share { get; set; }
    public List<string> Samples { get; set; } = [];
}

public class TopicClusterResult
{
    public List<TopicCluster> Clusters { get; set; } = [];
    public string Summary { get; set; } = "";
    public string Model { get; set; } = "";
}
