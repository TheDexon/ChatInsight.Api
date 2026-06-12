using ChatInsight.Api.Analysis.Personality;

namespace ChatInsight.Api.Analysis.Evolution;

/// <summary>Эволюция одного участника: портрет «раньше» → «позже» + что изменилось.</summary>
public class EvolutionEntry
{
    public string Participant { get; set; } = "";
    public PersonalityProfile Before { get; set; } = new();
    public PersonalityProfile After { get; set; } = new();
    public string Change { get; set; } = "";
}

/// <summary>Результат анализа эволюции личности по периодам.</summary>
public class PersonalityEvolutionResult
{
    public List<EvolutionEntry> Entries { get; set; } = [];
    public string Summary { get; set; } = "";
    public string Model { get; set; } = "";
}
