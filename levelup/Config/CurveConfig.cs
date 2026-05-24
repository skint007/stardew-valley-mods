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
    public int BaseXp { get; set; } = 100;

    /// <summary>Multiplier applied per level. Ignored unless <see cref="Preset"/> = Custom.</summary>
    public float GrowthRate { get; set; } = 1.15f;

    /// <summary>Resolve the actual (baseXp, growthRate) pair to use, applying presets.</summary>
    public (int baseXp, float growthRate) Resolve()
    {
        return Preset switch
        {
            CurvePreset.Casual   => (75,  1.12f),
            CurvePreset.Standard => (100, 1.15f),
            CurvePreset.Hardcore => (150, 1.20f),
            CurvePreset.Custom   => (BaseXp, GrowthRate),
            _ => (100, 1.15f),
        };
    }
}
