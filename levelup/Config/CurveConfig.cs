namespace LevelUp.Config;

public enum CurvePreset
{
    Casual,
    Standard,
    Hardcore,
    Custom
}

/// <summary>
/// Controls how much XP each level requires.
/// Formula: xpForLevel(N) = floor(BaseXp * GrowthRate^(N-1))
/// </summary>
public class CurveConfig
{
    public CurvePreset Preset { get; set; } = CurvePreset.Standard;

    /// <summary>Base XP for level 1→2. Ignored unless <see cref="Preset"/> = Custom.</summary>
    public int BaseXp { get; set; } = 250;

    /// <summary>Multiplier applied per level. Ignored unless <see cref="Preset"/> = Custom.</summary>
    public float GrowthRate { get; set; } = 1.10f;

    /// <summary>
    /// Resolve the actual (baseXp, growthRate) pair to use, applying presets.
    ///
    /// Curves use a higher base and a gentler growth than a naive exponential: a steep growth
    /// makes early levels trivially cheap (then a 4k monster kill rockets you up) and late
    /// levels astronomically expensive (an unreachable wall). Flatter growth + a higher floor
    /// spreads progression more evenly across levels 1–100.
    /// </summary>
    public (int baseXp, float growthRate) Resolve()
    {
        return Preset switch
        {
            CurvePreset.Casual   => (200, 1.06f),
            CurvePreset.Standard => (250, 1.08f),
            CurvePreset.Hardcore => (300, 1.10f),
            CurvePreset.Custom   => (BaseXp, GrowthRate),
            _ => (250, 1.08f),
        };
    }
}
