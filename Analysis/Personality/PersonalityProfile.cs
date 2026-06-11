namespace ChatInsight.Api.Analysis.Personality;

/// <summary>AI-портрет одного участника переписки.</summary>
public class PersonalityProfile
{
    public string Participant { get; set; } = "";
    public string Summary { get; set; } = "";
    public string CommunicationStyle { get; set; } = "";
    public List<string> Traits { get; set; } = [];
    public string Model { get; set; } = "";
}
