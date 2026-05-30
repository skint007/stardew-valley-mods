namespace LevelUp.Config;

/// <summary>
/// A single milestone entry. The mod always serializes 20 of these (most disabled).
/// At runtime, every <see cref="Enabled"/> milestone whose <see cref="Level"/> is &lt;= the player's
/// current level contributes its bonuses (summed cumulatively).
/// </summary>
public class MilestoneConfig
{
    /// <summary>Whether this slot is active. Disabled slots are skipped entirely.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Player level at which this milestone activates.</summary>
    public int Level { get; set; } = 0;

    /// <summary>Display name shown in level-up toast / tooltip (e.g. "Veteran").</summary>
    public string Name { get; set; } = "";

    // ── Stat bonuses ────────────────────────────────────────────────────────

    /// <summary>Flat max HP bonus.</summary>
    public int MaxHp { get; set; } = 0;

    /// <summary>Flat max energy (stamina) bonus.</summary>
    public int MaxEnergy { get; set; } = 0;

    /// <summary>HP restored per real-time minute while time is passing. 0 disables.</summary>
    public float HealthRegenPerMinute { get; set; } = 0f;

    /// <summary>Energy restored per real-time minute while time is passing. 0 disables.</summary>
    public float EnergyRegenPerMinute { get; set; } = 0f;

    /// <summary>Flat attack bonus (applied via persistent buff).</summary>
    public int Attack { get; set; } = 0;

    /// <summary>Flat defense bonus (applied via persistent buff).</summary>
    public int Defense { get; set; } = 0;

    /// <summary>Additive crit-chance multiplier, e.g. 0.05 = +5%.</summary>
    public float CritChance { get; set; } = 0f;

    /// <summary>Additive weapon-speed multiplier, e.g. 0.05 = +5%.</summary>
    public float WeaponSpeed { get; set; } = 0f;

    /// <summary>Additive movement-speed bonus (raw game units, +1 ≈ noticeable).</summary>
    public float MovementSpeed { get; set; } = 0f;

    /// <summary>Magnetic pickup radius bonus (game units, vanilla base is 128).</summary>
    public int MagneticRadius { get; set; } = 0;

    /// <summary>Flat luck level bonus.</summary>
    public int Luck { get; set; } = 0;

    /// <summary>Additive bonus to all skill XP gain, e.g. 0.10 = +10% XP.</summary>
    public float XpMultiplier { get; set; } = 0f;

    /// <summary>Additive bonus to shop sell prices, e.g. 0.05 = +5% sell price.</summary>
    public float SellPriceBonus { get; set; } = 0f;

    // ── Gameplay bonuses (consumed by Harmony patches) ─────────────────────

    /// <summary>
    /// Additive chance of rolling a bonus crop on harvest, e.g. 0.10 = +10% chance.
    /// Values &gt; 1.0 guarantee that many extras plus a roll for one more (e.g. 1.20 =
    /// always +1, plus 20% chance of +2).
    /// </summary>
    public float ExtraCropChance { get; set; } = 0f;

    /// <summary>
    /// Additive chance of duplicating ore / stone-node drops, e.g. 0.10 = +10% chance per
    /// drop. &gt;1.0 grants guaranteed extras with a roll for one more.
    /// </summary>
    public float ExtraOreChance { get; set; } = 0f;

    /// <summary>
    /// Additive speed-up applied to machine processing times, e.g. 0.10 = 10% faster
    /// (minutes scaled by 1 / (1 + bonus)). Only affects machines placed after the bonus
    /// is in effect, not already-running ones.
    /// </summary>
    public float MachineSpeedBonus { get; set; } = 0f;
}
