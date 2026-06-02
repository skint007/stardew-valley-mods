using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using TrainUp.Skills;

namespace TrainUp.Systems;

/// <summary>
/// Constructs and registers Train Up's custom skills with SpaceCore, and exposes the skill
/// instances (so professions can be queried) plus a small XP-award wrapper. Skills must be
/// registered after SpaceCore has loaded, i.e. from a GameLaunched handler.
/// </summary>
public class SkillRegistry
{
    private readonly IMonitor _monitor;

    public DefenseSkill Defense { get; private set; } = null!;
    public VitalitySkill Vitality { get; private set; } = null!;
    public StaminaSkill Stamina { get; private set; } = null!;

    public bool Registered { get; private set; }

    public SkillRegistry(IMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <summary>Build and register the three skills. Safe to call once, from GameLaunched.</summary>
    public void Register()
    {
        if (Registered) return;

        Defense = new DefenseSkill();
        Vitality = new VitalitySkill();
        Stamina = new StaminaSkill();

        SpaceCore.Skills.RegisterSkill(Defense);
        SpaceCore.Skills.RegisterSkill(Vitality);
        SpaceCore.Skills.RegisterSkill(Stamina);

        Registered = true;
        _monitor.Log("Registered Defense, Vitality, and Stamina skills with SpaceCore.", LogLevel.Info);
    }

    /// <summary>Award XP to a custom skill for the local player.</summary>
    public void AddXp(string skillId, int amount)
    {
        if (amount <= 0) return;
        if (!Context.IsWorldReady) return;
        SpaceCore.Skills.AddExperience(Game1.player, skillId, amount);
    }
}
