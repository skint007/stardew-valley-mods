using Microsoft.Xna.Framework;

namespace TrainUp.Skills;

/// <summary>Vitality: trained by losing HP. Professions favor toughness or recovery.</summary>
public class VitalitySkill : TrainUpSkill
{
    public const string SkillId = "skint007.TrainUp.Vitality";

    // L5
    public readonly GenericProfession Hardy;
    public readonly GenericProfession Recovery;
    // L10 (under Hardy)
    public readonly GenericProfession Juggernaut;
    public readonly GenericProfession Bloodied;
    // L10 (under Recovery)
    public readonly GenericProfession Medic;
    public readonly GenericProfession SecondWind;

    public VitalitySkill()
        : base(SkillId, "vitality", "assets/vitality-16.png", "assets/vitality-10.png", new Color(235, 90, 90))
    {
        Hardy = Prof("Hardy");
        Recovery = Prof("Recovery");
        Juggernaut = Prof("Juggernaut");
        Bloodied = Prof("Bloodied");
        Medic = Prof("Medic");
        SecondWind = Prof("SecondWind");
        BuildTree(Hardy, Recovery, Juggernaut, Bloodied, Medic, SecondWind);
    }
}
