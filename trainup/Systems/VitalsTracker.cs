using StardewModdingAPI;
using StardewValley;
using TrainUp.Config;

namespace TrainUp.Systems;

/// <summary>
/// Polls the local player's HP and energy each second and awards Vitality XP for HP lost and
/// Stamina XP for energy spent. Increases (healing, eating, the overnight refill) are ignored.
///
/// HP loss from combat is intentionally also counted here, so a monster hit trains both Defense
/// (via the takeDamage patch) and Vitality — both rates are independently configurable.
/// </summary>
public class VitalsTracker
{
    private readonly ModConfig _config;
    private readonly XpAwarder _xp;

    private int _prevHealth;
    private float _prevStamina;
    private bool _primed;

    public VitalsTracker(ModConfig config, XpAwarder xp)
    {
        _config = config;
        _xp = xp;
    }

    /// <summary>Snap the baselines to the current values without awarding (call on load/day start).</summary>
    public void Prime()
    {
        if (!Context.IsWorldReady) { _primed = false; return; }
        _prevHealth = Game1.player.health;
        _prevStamina = Game1.player.stamina;
        _primed = true;
    }

    /// <summary>Compare current vitals to the last snapshot and award XP for any decrease.</summary>
    public void Tick()
    {
        if (!_config.Enabled) return;
        if (!Context.IsWorldReady) return;
        if (!Game1.shouldTimePass()) return; // pause in menus, events, sleep

        if (!_primed) { Prime(); return; }

        var player = Game1.player;

        // Vitality: HP lost. Don't reward the killing blow / passing out.
        int hpLost = _prevHealth - player.health;
        if (hpLost > 0 && player.health > 0)
            _xp.AwardVitalityFromHpLost(hpLost);

        // Stamina: energy spent.
        float energySpent = _prevStamina - player.stamina;
        if (energySpent > 0f)
            _xp.AwardStaminaFromEnergy(energySpent);

        _prevHealth = player.health;
        _prevStamina = player.stamina;
    }
}
