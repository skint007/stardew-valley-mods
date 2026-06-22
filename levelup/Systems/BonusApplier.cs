using System;
using System.Linq;
using LevelUp.Config;
using LevelUp.State;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;

namespace LevelUp.Systems;

/// <summary>
/// Computes the cumulative bonus from all unlocked milestones and applies it.
///
/// Application strategy:
///   - Max HP / Max Energy: direct field mutation on Game1.player, using cached vanilla baselines
///   - Attack / Defense / CritChance / WeaponSpeed / Speed / MagneticRadius / Luck:
///     single persistent <see cref="Buff"/> with custom <see cref="BuffEffects"/>, reapplied on
///     DayStarted, SaveLoaded, level-up, and on config save
///   - XpMultiplier: consumed by Player.ExperienceReceived handler (see ModEntry)
///   - SellPriceBonus: consumed by Harmony patch on Object.sellToStorePrice (see Patches/ObjectPatches)
/// </summary>
public class BonusApplier
{
    public const string BuffId = "skint007.LevelUp.MilestoneBuff";

    private readonly ModConfig _config;
    private readonly SaveDataManager _saveData;
    private readonly ITranslationHelper _i18n;
    private readonly IMonitor _monitor;

    /// <summary>Current cumulative XP-gain bonus, exposed for the experience-received handler.</summary>
    public float CurrentXpMultiplier { get; private set; }

    /// <summary>Current cumulative sell-price bonus, exposed for the Object.sellToStorePrice patch.</summary>
    public float CurrentSellPriceBonus { get; private set; }

    /// <summary>Current cumulative HP regen rate (HP per real-time minute).</summary>
    public float CurrentHealthRegenPerMinute { get; private set; }

    /// <summary>Current cumulative energy regen rate (energy per real-time minute).</summary>
    public float CurrentEnergyRegenPerMinute { get; private set; }

    /// <summary>Current cumulative bonus-crop chance, consumed by the Crop.harvest patch.</summary>
    public float CurrentExtraCropChance { get; private set; }

    /// <summary>Current cumulative bonus-ore chance, consumed by the OnStoneDestroyed patch.</summary>
    public float CurrentExtraOreChance { get; private set; }

    /// <summary>Current cumulative machine-speed bonus, consumed by the PlaceInMachine patch.</summary>
    public float CurrentMachineSpeedBonus { get; private set; }

    // Fractional carry between ticks so a slow regen rate (e.g. 0.5/min) still applies
    // whole HP/energy points over time instead of always rounding to zero.
    private float _hpAccumulator;
    private float _energyAccumulator;

    public BonusApplier(ModConfig config, SaveDataManager saveData, ITranslationHelper i18n, IMonitor monitor)
    {
        _config = config;
        _saveData = saveData;
        _i18n = i18n;
        _monitor = monitor;
    }

    /// <summary>Sum of all milestone bonuses currently unlocked by the player's level.</summary>
    public MilestoneConfig ComputeCumulativeBonus()
    {
        var sum = new MilestoneConfig();
        int level = _saveData.Current.Level;

        foreach (var m in _config.Milestones.Where(m => m.Enabled && m.Level <= level))
        {
            sum.MaxHp                += m.MaxHp;
            sum.MaxEnergy            += m.MaxEnergy;
            sum.HealthRegenPerMinute += m.HealthRegenPerMinute;
            sum.EnergyRegenPerMinute += m.EnergyRegenPerMinute;
            sum.Attack               += m.Attack;
            sum.Defense              += m.Defense;
            sum.CritChance           += m.CritChance;
            sum.WeaponSpeed          += m.WeaponSpeed;
            sum.MovementSpeed        += m.MovementSpeed;
            sum.MagneticRadius       += m.MagneticRadius;
            sum.Luck                 += m.Luck;
            sum.XpMultiplier         += m.XpMultiplier;
            sum.SellPriceBonus       += m.SellPriceBonus;
            sum.ExtraCropChance      += m.ExtraCropChance;
            sum.ExtraOreChance       += m.ExtraOreChance;
            sum.MachineSpeedBonus    += m.MachineSpeedBonus;
        }
        return sum;
    }

    /// <summary>
    /// Recompute and apply bonuses to the current player. Call on:
    ///   - SaveLoaded
    ///   - DayStarted
    ///   - After level-up
    ///   - After config save (in case milestones changed)
    /// </summary>
    public void ApplyAll()
    {
        if (!Context.IsWorldReady) return;
        var player = Game1.player;
        if (player == null) return;

        // Absorb any vanilla-side max HP / energy increases (Stardrops, Combat Mastery cave)
        // that happened since the last apply, before we reset to baseline and wipe them.
        AbsorbVanillaIncreases(player);

        // Reset to baseline first to prevent compounding across reapplies.
        ResetToBaseline(player);

        if (!_config.Enabled)
        {
            CurrentXpMultiplier = 0f;
            CurrentSellPriceBonus = 0f;
            CurrentHealthRegenPerMinute = 0f;
            CurrentEnergyRegenPerMinute = 0f;
            CurrentExtraCropChance = 0f;
            CurrentExtraOreChance = 0f;
            CurrentMachineSpeedBonus = 0f;
            _hpAccumulator = 0f;
            _energyAccumulator = 0f;
            RemoveBuff(player);
            return;
        }

        var bonus = ComputeCumulativeBonus();

        // ── HP / Energy: direct mutation using cached vanilla baselines ────────
        // Guard: don't apply on top of a zero baseline (would clamp the player to the bonus value).
        if (bonus.MaxHp > 0 && _saveData.Current.BaselineMaxHp > 0)
            player.maxHealth = _saveData.Current.BaselineMaxHp + bonus.MaxHp;
        if (bonus.MaxEnergy > 0 && _saveData.Current.BaselineMaxEnergy > 0)
            player.maxStamina.Value = _saveData.Current.BaselineMaxEnergy + bonus.MaxEnergy;

        // Remember what we just applied, so the next AbsorbVanillaIncreases can tell our
        // own bonus apart from any subsequent vanilla bump.
        _saveData.Current.LastAppliedMaxHpBonus = bonus.MaxHp;
        _saveData.Current.LastAppliedMaxEnergyBonus = bonus.MaxEnergy;

        // ── Combat / utility: single persistent buff ───────────────────────────
        ApplyMilestoneBuff(player, bonus);

        // ── Consumed by other systems ──────────────────────────────────────────
        CurrentXpMultiplier = bonus.XpMultiplier;
        CurrentSellPriceBonus = bonus.SellPriceBonus;
        CurrentHealthRegenPerMinute = bonus.HealthRegenPerMinute;
        CurrentEnergyRegenPerMinute = bonus.EnergyRegenPerMinute;
        CurrentExtraCropChance = bonus.ExtraCropChance;
        CurrentExtraOreChance = bonus.ExtraOreChance;
        CurrentMachineSpeedBonus = bonus.MachineSpeedBonus;
        if (CurrentHealthRegenPerMinute <= 0f) _hpAccumulator = 0f;
        if (CurrentEnergyRegenPerMinute <= 0f) _energyAccumulator = 0f;

        if (_config.DebugLogging)
        {
            _monitor.Log(
                $"Applied bonuses (lv {_saveData.Current.Level}): +{bonus.MaxHp} HP, +{bonus.MaxEnergy} EN, " +
                $"+{bonus.HealthRegenPerMinute}/min hp-regen, +{bonus.EnergyRegenPerMinute}/min en-regen, " +
                $"+{bonus.Attack} atk, +{bonus.Defense} def, +{bonus.CritChance:P0} crit, " +
                $"+{bonus.WeaponSpeed:P0} wspd, +{bonus.MovementSpeed} mspd, +{bonus.MagneticRadius} mag, " +
                $"+{bonus.Luck} luck, +{bonus.XpMultiplier:P0} xp, +{bonus.SellPriceBonus:P0} sell, " +
                $"+{bonus.ExtraCropChance:P0} crop, +{bonus.ExtraOreChance:P0} ore, +{bonus.MachineSpeedBonus:P0} mach",
                LogLevel.Debug);
        }
    }

    /// <summary>
    /// Apply one second's worth of HP/energy regen. Call from SMAPI's OneSecondUpdateTicked.
    /// Skips when the world isn't ready or time isn't passing (menus/events/sleep), so menus
    /// don't tick regen and the player can't farm full HP by sitting in the pause screen.
    /// </summary>
    public void TickRegen()
    {
        if (!Context.IsWorldReady) return;
        if (!_config.Enabled) return;
        if (!Game1.shouldTimePass()) return;

        var player = Game1.player;
        if (player == null) return;

        // HP: don't resurrect a dead player; only regen between (0, maxHealth).
        if (CurrentHealthRegenPerMinute > 0f && player.health > 0 && player.health < player.maxHealth)
        {
            _hpAccumulator += CurrentHealthRegenPerMinute / 60f;
            int whole = (int)_hpAccumulator;
            if (whole > 0)
            {
                player.health = Math.Min(player.maxHealth, player.health + whole);
                _hpAccumulator -= whole;
            }
        }

        // Energy: allow regen even when negative (exhausted) so it can pull them back into
        // positive territory before bedtime; just cap at MaxStamina.
        if (CurrentEnergyRegenPerMinute > 0f && player.Stamina < player.MaxStamina)
        {
            _energyAccumulator += CurrentEnergyRegenPerMinute / 60f;
            int whole = (int)_energyAccumulator;
            if (whole > 0)
            {
                player.Stamina = Math.Min(player.MaxStamina, player.Stamina + whole);
                _energyAccumulator -= whole;
            }
        }
    }

    private void ApplyMilestoneBuff(Farmer player, MilestoneConfig bonus)
    {
        bool hasAny =
            bonus.Attack != 0 ||
            bonus.Defense != 0 ||
            bonus.CritChance != 0f ||
            bonus.WeaponSpeed != 0f ||
            bonus.MovementSpeed != 0f ||
            bonus.MagneticRadius != 0 ||
            bonus.Luck != 0;

        if (!hasAny)
        {
            RemoveBuff(player);
            return;
        }

        var effects = new BuffEffects
        {
            Attack                   = { Value = bonus.Attack },
            Defense                  = { Value = bonus.Defense },
            CriticalChanceMultiplier = { Value = bonus.CritChance },
            WeaponSpeedMultiplier    = { Value = bonus.WeaponSpeed },
            Speed                    = { Value = bonus.MovementSpeed },
            MagneticRadius           = { Value = bonus.MagneticRadius },
            LuckLevel                = { Value = bonus.Luck },
        };

        var buff = new Buff(
            id: BuffId,
            source: "Level Up", // stable internal identifier; not shown to the player
            displaySource: _i18n.Get("buff.source"),
            duration: Buff.ENDLESS,
            iconTexture: null,
            iconSheetIndex: -1,
            effects: effects,
            isDebuff: false,
            displayName: _i18n.Get("buff.name"));

        // Replace any existing buff of the same id.
        player.buffs.Remove(BuffId);
        player.applyBuff(buff);
    }

    private static void RemoveBuff(Farmer player)
    {
        player.buffs.Remove(BuffId);
    }

    /// <summary>
    /// Strip the mod's bonuses off the player: reset maxHealth / maxStamina to baseline and
    /// remove the persistent buff. Call before vanilla save serializes the player so the
    /// inflated values don't get baked into the save file.
    /// </summary>
    public void Strip()
    {
        if (!Context.IsWorldReady) return;
        var player = Game1.player;
        if (player == null) return;
        // Absorb pending vanilla bumps before zeroing them out via ResetToBaseline. Critical
        // for the day-end save path: vanilla serializes maxStamina after we Strip, so a
        // Stardrop eaten today is otherwise lost forever.
        AbsorbVanillaIncreases(player);
        ResetToBaseline(player);
        RemoveBuff(player);
    }

    private void ResetToBaseline(Farmer player)
    {
        if (_saveData.Current.BaselineMaxHp > 0)
            player.maxHealth = _saveData.Current.BaselineMaxHp;
        if (_saveData.Current.BaselineMaxEnergy > 0)
            player.maxStamina.Value = _saveData.Current.BaselineMaxEnergy;
    }

    /// <summary>
    /// Detect post-baseline vanilla increases to <see cref="Farmer.maxHealth"/> /
    /// <see cref="Farmer.MaxStamina"/> (Stardrops add +34 EN, the Combat Mastery cave
    /// reward adds +25 HP, other mods may also bump these) and ratchet the stored
    /// baseline upward so the next ResetToBaseline / ApplyAll doesn't wipe them.
    ///
    /// Only ratchets up, never down: a vanilla "lose max HP" effect should not bake a
    /// permanent decrease into our baseline.
    /// </summary>
    private void AbsorbVanillaIncreases(Farmer player)
    {
        if (_saveData.Current.BaselineMaxHp > 0)
        {
            int observed = player.maxHealth;
            int expected = _saveData.Current.BaselineMaxHp + _saveData.Current.LastAppliedMaxHpBonus;
            int delta = observed - expected;
            if (delta > 0)
            {
                _saveData.Current.BaselineMaxHp += delta;
                _monitor.Log(
                    $"Absorbed +{delta} max HP into baseline (now {_saveData.Current.BaselineMaxHp}).",
                    LogLevel.Info);
            }
        }

        if (_saveData.Current.BaselineMaxEnergy > 0)
        {
            int observed = (int)player.MaxStamina;
            int expected = _saveData.Current.BaselineMaxEnergy + _saveData.Current.LastAppliedMaxEnergyBonus;
            int delta = observed - expected;
            if (delta > 0)
            {
                _saveData.Current.BaselineMaxEnergy += delta;
                _monitor.Log(
                    $"Absorbed +{delta} max energy into baseline (now {_saveData.Current.BaselineMaxEnergy}).",
                    LogLevel.Info);
            }
        }
    }
}
