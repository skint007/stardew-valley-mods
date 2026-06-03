using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using TrainUp.Config;
using TrainUp.Skills;

namespace TrainUp.Systems;

/// <summary>
/// Applies the stat-based profession perks (max HP, max energy, defense) and ticks the regen
/// perks. Mutations are "save-safe": <see cref="Strip"/> removes them before the game serializes
/// the farmer and <see cref="Apply"/> re-adds them afterward, so disabling the mod cleanly
/// reverts the character (mirrors the approach used by the Level Up mod).
/// </summary>
public class BonusApplier
{
    public const string DefenseBuffId = "skint007.TrainUp/defense";

    private readonly ModConfig _config;
    private readonly SkillRegistry _skills;
    private readonly IMonitor _monitor;

    private int _appliedHp;
    private int _appliedStamina;
    private int _appliedDefense;

    private float _hpAccumulator;
    private float _staminaAccumulator;

    /// <summary>Snapshot of professions + skill levels last applied, to detect changes cheaply.</summary>
    private string _lastSignature = "";

    public BonusApplier(ModConfig config, SkillRegistry skills, IMonitor monitor)
    {
        _config = config;
        _skills = skills;
        _monitor = monitor;
    }

    private bool Has(GenericProfession p) => p.IsActiveFor(Game1.player);

    private static int Level(string skillId) =>
        Context.IsWorldReady ? SpaceCore.Skills.GetSkillLevel(Game1.player, skillId) : 0;

    /// <summary>A cheap signature of what affects the applied bonuses (profession picks + levels).</summary>
    private static string Signature() =>
        $"{Game1.player.professions.Count}:{Level(DefenseSkill.SkillId)}:{Level(VitalitySkill.SkillId)}:{Level(StaminaSkill.SkillId)}";

    /// <summary>Strip any prior bonuses, then (re)apply per-level growth plus profession perks.</summary>
    public void Apply()
    {
        if (!Context.IsWorldReady) return;
        Strip();

        if (!_config.Enabled) return;

        var player = Game1.player;
        bool perks = _config.EnableProfessionPerks;

        // Max HP: per-level growth, plus Hardy (+15) / Juggernaut (+25 more).
        int hp = Level(VitalitySkill.SkillId) * _config.VitalityHpPerLevel;
        if (perks)
        {
            if (Has(_skills.Vitality.Hardy)) hp += 15;
            if (Has(_skills.Vitality.Juggernaut)) hp += 25;
        }
        if (hp > 0) { player.maxHealth += hp; _appliedHp = hp; }

        // Max energy: per-level growth, plus Energetic (+25) / Marathoner (+50 more).
        int stam = Level(StaminaSkill.SkillId) * _config.StaminaEnergyPerLevel;
        if (perks)
        {
            if (Has(_skills.Stamina.Energetic)) stam += 25;
            if (Has(_skills.Stamina.Marathoner)) stam += 50;
        }
        if (stam > 0) { player.maxStamina.Value += stam; _appliedStamina = stam; }

        // Defense: per-level growth, plus Tough (+1) / Ironhide (+2 more). Applied as a buff.
        int def = Level(DefenseSkill.SkillId) * _config.DefensePerLevel;
        if (perks)
        {
            if (Has(_skills.Defense.Tough)) def += 1;
            if (Has(_skills.Defense.Ironhide)) def += 2;
        }
        if (def > 0)
        {
            player.buffs.Remove(DefenseBuffId);
            player.applyBuff(new Buff(
                id: DefenseBuffId,
                source: "Train Up",
                displaySource: ModEntry.Instance.Helper.Translation.Get("mod.name"),
                duration: Buff.ENDLESS,
                iconTexture: null,
                iconSheetIndex: -1,
                effects: new BuffEffects { Defense = { Value = def } },
                isDebuff: false,
                displayName: ModEntry.Instance.Helper.Translation.Get("mod.name")));
            _appliedDefense = def;
        }

        _lastSignature = Signature();
    }

    /// <summary>Remove all applied bonuses, restoring vanilla stat values.</summary>
    public void Strip()
    {
        if (!Context.IsWorldReady) return;
        var player = Game1.player;

        if (_appliedHp != 0)
        {
            player.maxHealth -= _appliedHp;
            _appliedHp = 0;
            if (player.health > player.maxHealth) player.health = player.maxHealth;
        }
        if (_appliedStamina != 0)
        {
            player.maxStamina.Value -= _appliedStamina;
            _appliedStamina = 0;
            if (player.stamina > player.MaxStamina) player.stamina = player.MaxStamina;
        }
        if (_appliedDefense != 0)
        {
            player.buffs.Remove(DefenseBuffId);
            _appliedDefense = 0;
        }
    }

    /// <summary>Re-apply if the player gained a level or picked a new profession since last apply.</summary>
    public void RefreshIfChanged()
    {
        if (!Context.IsWorldReady) return;
        if (Signature() != _lastSignature)
            Apply();
    }

    /// <summary>Tick HP/energy regen perks. Call from a one-second handler.</summary>
    public void TickRegen()
    {
        if (!_config.Enabled || !_config.EnableProfessionPerks) return;
        if (!Context.IsWorldReady || !Game1.shouldTimePass()) return;

        var player = Game1.player;

        // HP regen: Recovery (2/min), Second Wind raises it (5/min). Don't revive the dead.
        float hpPerMin = 0f;
        if (Has(_skills.Vitality.Recovery)) hpPerMin = 2f;
        if (Has(_skills.Vitality.SecondWind)) hpPerMin = 5f;
        if (hpPerMin > 0f && player.health > 0 && player.health < player.maxHealth)
        {
            _hpAccumulator += hpPerMin / 60f;
            int whole = (int)_hpAccumulator;
            if (whole > 0)
            {
                player.health = System.Math.Min(player.maxHealth, player.health + whole);
                _hpAccumulator -= whole;
            }
        }

        // Energy regen: Tireless (3/min).
        if (Has(_skills.Stamina.Tireless) && player.stamina < player.MaxStamina)
        {
            _staminaAccumulator += 3f / 60f;
            int whole = (int)_staminaAccumulator;
            if (whole > 0)
            {
                player.stamina = System.Math.Min(player.MaxStamina, player.stamina + whole);
                _staminaAccumulator -= whole;
            }
        }
    }
}
