namespace ChatInsight.Api.Analysis.Relationship;

public class RelationshipReport
{
    /// <summary>Доля инициативы доминирующего участника, % (50 = поровну).</summary>
    public int InitiativeBalance { get; set; } = 50;

    /// <summary>Доля ответов доминирующего участника, % (50 = поровну).</summary>
    public int ResponseBalance { get; set; } = 50;

    /// <summary>Доля сообщений доминирующего участника, %.</summary>
    public int ActivityBalance { get; set; }

    /// <summary>Самый активный участник (база для процентов выше).</summary>
    public string DominantParticipant { get; set; } = "";

    /// <summary>Второй участник (для пары X / Y).</summary>
    public string SecondaryParticipant { get; set; } = "";

    public string Summary { get; set; } = "";
}
