using System.Collections.Generic;

namespace LevelUp.State;

/// <summary>
/// Per-player persisted state. Serialized to JSON and stored on the local player's
/// <see cref="StardewValley.Farmer.modData"/> by <see cref="SaveDataManager"/> (works in
/// multiplayer; each character keeps its own copy).
/// </summary>
public class PlayerLevelData
{

    /// <summary>Total XP earned (lifetime, never decreases except via reset).</summary>
    public long TotalXp { get; set; } = 0;

    /// <summary>Current player level (derived from TotalXp, cached for convenience).</summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Vanilla baseline max HP captured the first time we see this save.
    /// Used to compute additive bonuses without compounding.
    /// </summary>
    public int BaselineMaxHp { get; set; } = 0;

    /// <summary>
    /// Vanilla baseline max energy captured the first time we see this save.
    /// </summary>
    public int BaselineMaxEnergy { get; set; } = 0;

    /// <summary>
    /// Last max-HP bonus we applied on top of <see cref="BaselineMaxHp"/>. Used to detect
    /// vanilla-side increases (Stardrops, Combat Mastery cave reward, etc.) that happen
    /// after the baseline was first captured, so we can ratchet the baseline upward and
    /// avoid wiping them when we reapply.
    /// </summary>
    public int LastAppliedMaxHpBonus { get; set; } = 0;

    /// <summary>
    /// Last max-energy bonus we applied on top of <see cref="BaselineMaxEnergy"/>. See
    /// <see cref="LastAppliedMaxHpBonus"/>.
    /// </summary>
    public int LastAppliedMaxEnergyBonus { get; set; } = 0;

    /// <summary>
    /// Set of location names already visited (for the "new area" XP source).
    /// We mirror Stardew 1.6's locationsVisited but maintain our own set to
    /// avoid awarding XP for areas visited before the mod was installed.
    /// </summary>
    public HashSet<string> AreasAwardedXpFor { get; set; } = new();

    /// <summary>Save format version, for future migrations.</summary>
    public int Version { get; set; } = 1;
}
