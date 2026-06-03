using Microsoft.Xna.Framework;

namespace TrainUp.Skills;

/// <summary>Stamina: trained by spending energy. Professions favor capacity or efficiency.</summary>
public class StaminaSkill : TrainUpSkill
{
    public const string SkillId = "skint007.TrainUp.Stamina";

    // L5
    public readonly GenericProfession Energetic;
    public readonly GenericProfession Efficient;
    // L10 (under Energetic)
    public readonly GenericProfession Marathoner;
    public readonly GenericProfession Tireless;
    // L10 (under Efficient)
    public readonly GenericProfession Conservationist;
    public readonly GenericProfession Caffeinated;

    public override int PerLevelBonus => ModEntry.Instance.Config.StaminaEnergyPerLevel;

    public StaminaSkill()
        : base(SkillId, "stamina", "assets/stamina-16.png", "assets/stamina-10.png", new Color(245, 205, 80))
    {
        Energetic = Prof("Energetic");
        Efficient = Prof("Efficient");
        Marathoner = Prof("Marathoner");
        Tireless = Prof("Tireless");
        Conservationist = Prof("Conservationist");
        Caffeinated = Prof("Caffeinated");
        BuildTree(Energetic, Efficient, Marathoner, Tireless, Conservationist, Caffeinated);
    }
}
