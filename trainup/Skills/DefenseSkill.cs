using Microsoft.Xna.Framework;

namespace TrainUp.Skills;

/// <summary>Defense: trained by taking hits. Professions favor mitigation or evasion.</summary>
public class DefenseSkill : TrainUpSkill
{
    public const string SkillId = "skint007.TrainUp.Defense";

    // L5
    public readonly GenericProfession Tough;
    public readonly GenericProfession Evasive;
    // L10 (under Tough)
    public readonly GenericProfession Ironhide;
    public readonly GenericProfession Retaliate;
    // L10 (under Evasive)
    public readonly GenericProfession Acrobat;
    public readonly GenericProfession Counter;

    public DefenseSkill()
        : base(SkillId, "defense", "assets/defense-16.png", "assets/defense-10.png", new Color(120, 170, 255))
    {
        Tough = Prof("Tough");
        Evasive = Prof("Evasive");
        Ironhide = Prof("Ironhide");
        Retaliate = Prof("Retaliate");
        Acrobat = Prof("Acrobat");
        Counter = Prof("Counter");
        BuildTree(Tough, Evasive, Ironhide, Retaliate, Acrobat, Counter);
    }
}
