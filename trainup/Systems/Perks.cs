using StardewModdingAPI;
using StardewValley;
using TrainUp.Config;
using TrainUp.Skills;

namespace TrainUp.Systems;

/// <summary>
/// Static facade the Harmony patches use to query the local player's active profession perks,
/// already gated by the mod's enable/perks toggles. Initialized once at startup.
/// </summary>
public static class Perks
{
    private static ModConfig _config = null!;
    private static SkillRegistry _skills = null!;

    public static void Init(ModConfig config, SkillRegistry skills)
    {
        _config = config;
        _skills = skills;
    }

    private static bool Enabled => _config.Enabled && _config.EnableProfessionPerks && Context.IsWorldReady;

    private static bool Has(GenericProfession p) => Enabled && p.IsActiveFor(Game1.player);

    // ── Defense ────────────────────────────────────────────────────────────────
    /// <summary>Chance (0–1) to dodge an incoming hit. Acrobat upgrades Evasive.</summary>
    public static float DodgeChance()
    {
        if (Has(_skills.Defense.Acrobat)) return 0.20f;
        if (Has(_skills.Defense.Evasive)) return 0.10f;
        return 0f;
    }

    /// <summary>Heal a little HP on a successful dodge.</summary>
    public static bool HasCounter => Has(_skills.Defense.Counter);

    /// <summary>Fraction of damage taken reflected back to the attacker.</summary>
    public static float RetaliateFraction() => Has(_skills.Defense.Retaliate) ? 0.25f : 0f;

    // ── Vitality ────────────────────────────────────────────────────────────────
    /// <summary>Outgoing weapon-damage multiplier, scaling with missing HP (Bloodied).</summary>
    public static float BloodiedMultiplier()
    {
        if (!Has(_skills.Vitality.Bloodied)) return 1f;
        var p = Game1.player;
        if (p.maxHealth <= 0) return 1f;
        float missing = 1f - ((float)p.health / p.maxHealth); // 0 at full, 1 near death
        return 1f + 0.5f * missing; // up to +50%
    }

    /// <summary>Healing-item HP multiplier (Medic).</summary>
    public static float FoodHealthMultiplier() => Has(_skills.Vitality.Medic) ? 1.25f : 1f;

    // ── Stamina ──────────────────────────────────────────────────────────────────
    /// <summary>Food/drink energy multiplier (Caffeinated).</summary>
    public static float FoodStaminaMultiplier() => Has(_skills.Stamina.Caffeinated) ? 1.25f : 1f;

    /// <summary>True if this stamina expenditure should be free (Conservationist, 20% chance).</summary>
    public static bool ConservationistFreeRoll() => Has(_skills.Stamina.Conservationist) && Game1.random.NextDouble() < 0.20;

    /// <summary>Multiplier applied to stamina spent (Efficient: 0.9).</summary>
    public static float EfficientCostMultiplier() => Has(_skills.Stamina.Efficient) ? 0.9f : 1f;
}
