namespace TrainUp.Config;

/// <summary>
/// User configuration for Train Up. Loaded via <c>Helper.ReadConfig</c> and exposed in-game
/// through GMCM. All XP rates and caps are tunable so players can balance training to taste;
/// any rate set to 0 disables that training source.
/// </summary>
public class ModConfig
{
    // ── Master ────────────────────────────────────────────────────────────────
    /// <summary>Master switch. When false, no XP is awarded and no profession perks apply.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Extra trace logging for troubleshooting XP awards.</summary>
    public bool DebugLogging { get; set; } = false;

    // ── XP rates ──────────────────────────────────────────────────────────────
    /// <summary>Defense XP earned per point of HP actually lost to an enemy hit.</summary>
    public float DefenseXpPerDamage { get; set; } = 3f;

    /// <summary>Vitality XP earned per point of HP lost from any source.</summary>
    public float VitalityXpPerHpLost { get; set; } = 2f;

    /// <summary>Stamina XP earned per point of energy spent.</summary>
    public float StaminaXpPerEnergy { get; set; } = 0.5f;

    // ── Daily XP caps (0 = unlimited) ───────────────────────────────────────────
    /// <summary>Max Defense XP per day. 0 disables the cap.</summary>
    public int DefenseDailyXpCap { get; set; } = 0;

    /// <summary>Max Vitality XP per day. 0 disables the cap.</summary>
    public int VitalityDailyXpCap { get; set; } = 0;

    /// <summary>Max Stamina XP per day. 0 disables the cap.</summary>
    public int StaminaDailyXpCap { get; set; } = 0;

    // ── Training dummy ──────────────────────────────────────────────────────────
    /// <summary>Whether the training dummy grants vanilla Combat XP when hit.</summary>
    public bool DummyEnabled { get; set; } = true;

    /// <summary>Vanilla Combat XP granted per hit on the training dummy.</summary>
    public int DummyCombatXpPerHit { get; set; } = 2;

    /// <summary>Max Combat XP per day from the dummy (anti-grind). 0 disables the cap.</summary>
    public int DummyDailyXpCap { get; set; } = 500;

    // ── Profession perks ─────────────────────────────────────────────────────────
    /// <summary>Apply the stat/gameplay perks granted by chosen professions.</summary>
    public bool EnableProfessionPerks { get; set; } = true;

    /// <summary>
    /// Copy all settings from <paramref name="other"/> into this instance. Used so GMCM's
    /// "reset to default" updates the single shared config object every system holds a reference
    /// to, instead of swapping in a new instance they wouldn't see.
    /// </summary>
    public void CopyFrom(ModConfig other)
    {
        Enabled = other.Enabled;
        DebugLogging = other.DebugLogging;
        DefenseXpPerDamage = other.DefenseXpPerDamage;
        VitalityXpPerHpLost = other.VitalityXpPerHpLost;
        StaminaXpPerEnergy = other.StaminaXpPerEnergy;
        DefenseDailyXpCap = other.DefenseDailyXpCap;
        VitalityDailyXpCap = other.VitalityDailyXpCap;
        StaminaDailyXpCap = other.StaminaDailyXpCap;
        DummyEnabled = other.DummyEnabled;
        DummyCombatXpPerHit = other.DummyCombatXpPerHit;
        DummyDailyXpCap = other.DummyDailyXpCap;
        EnableProfessionPerks = other.EnableProfessionPerks;
    }
}
