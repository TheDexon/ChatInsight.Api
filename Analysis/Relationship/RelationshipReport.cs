namespace ChatInsight.Api.Analysis.Relationship;

public class RelationshipReport
{
    public int InitiativeBalance { get; set; }

    public int ResponseBalance { get; set; }

    public int ActivityBalance { get; set; }

    public string DominantParticipant { get; set; } = "";

    public string Summary { get; set; } = "";
}