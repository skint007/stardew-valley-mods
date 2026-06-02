using StardewModdingAPI;
using StardewValley;
using TrainUp.Config;
using TrainUp.Skills;

namespace TrainUp.Systems;

/// <summary>
/// Central place that turns in-world events (damage taken, HP lost, energy spent, dummy hits)
/// into skill XP, applying the configured rates and optional per-day caps.
///
/// Fractional XP is accumulated and only whole points are flushed to SpaceCore, so low rates
/// (e.g. 0.5 XP per energy) don't round away to nothing. Daily counters reset on day start.
/// </summary>
public class XpAwarder
{
    private readonly ModConfig _config;
    private readonly SkillRegistry _skills;
    private readonly IMonitor _monitor;

    // Fractional XP carried between flushes.
    private float _defenseAccum;
    private float _vitalityAccum;
    private float _staminaAccum;

    // XP granted so far today (for the daily caps).
    private int _defenseToday;
    private int _vitalityToday;
    private int _staminaToday;
    private int _dummyToday;

    public XpAwarder(ModConfig config, SkillRegistry skills, IMonitor monitor)
    {
        _config = config;
        _skills = skills;
        _monitor = monitor;
    }

    /// <summary>Reset the per-day XP counters. Call on day start.</summary>
    public void ResetDaily()
    {
        _defenseToday = _vitalityToday = _staminaToday = _dummyToday = 0;
    }

    /// <summary>Defense XP for HP actually lost to an enemy hit.</summary>
    public void AwardDefenseFromDamage(int hpLost)
    {
        if (!_config.Enabled || _config.DefenseXpPerDamage <= 0f || hpLost <= 0) return;
        _defenseAccum += hpLost * _config.DefenseXpPerDamage;
        Flush(DefenseSkill.SkillId, ref _defenseAccum, ref _defenseToday, _config.DefenseDailyXpCap, "defense");
    }

    /// <summary>Vitality XP for HP lost from any source.</summary>
    public void AwardVitalityFromHpLost(int hpLost)
    {
        if (!_config.Enabled || _config.VitalityXpPerHpLost <= 0f || hpLost <= 0) return;
        _vitalityAccum += hpLost * _config.VitalityXpPerHpLost;
        Flush(VitalitySkill.SkillId, ref _vitalityAccum, ref _vitalityToday, _config.VitalityDailyXpCap, "vitality");
    }

    /// <summary>Stamina XP for energy spent.</summary>
    public void AwardStaminaFromEnergy(float energySpent)
    {
        if (!_config.Enabled || _config.StaminaXpPerEnergy <= 0f || energySpent <= 0f) return;
        _staminaAccum += energySpent * _config.StaminaXpPerEnergy;
        Flush(StaminaSkill.SkillId, ref _staminaAccum, ref _staminaToday, _config.StaminaDailyXpCap, "stamina");
    }

    /// <summary>
    /// Vanilla Combat XP for a training-dummy hit, honoring the dummy's daily cap.
    /// Returns the XP actually granted (0 if disabled or capped out).
    /// </summary>
    public int AwardDummyCombatXp(Farmer who)
    {
        if (!_config.Enabled || !_config.DummyEnabled || _config.DummyCombatXpPerHit <= 0) return 0;

        int grant = _config.DummyCombatXpPerHit;
        if (_config.DummyDailyXpCap > 0)
        {
            int remaining = _config.DummyDailyXpCap - _dummyToday;
            if (remaining <= 0) return 0;
            if (grant > remaining) grant = remaining;
        }

        who.gainExperience(Farmer.combatSkill, grant); // combatSkill == 4
        _dummyToday += grant;
        return grant;
    }

    /// <summary>Move whole accumulated points into the skill, respecting the daily cap.</summary>
    private void Flush(string skillId, ref float accum, ref int today, int dailyCap, string label)
    {
        int whole = (int)accum;
        if (whole <= 0) return;
        accum -= whole;

        int grant = whole;
        if (dailyCap > 0)
        {
            int remaining = dailyCap - today;
            if (remaining <= 0) return;
            if (grant > remaining) grant = remaining;
        }

        if (grant <= 0) return;
        _skills.AddXp(skillId, grant);
        today += grant;

        if (_config.DebugLogging)
            _monitor.Log($"+{grant} {label} XP (today: {today}).", LogLevel.Trace);
    }
}
