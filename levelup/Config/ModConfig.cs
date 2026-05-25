using System.Collections.Generic;
using StardewModdingAPI;

namespace LevelUp.Config;

/// <summary>
/// Root mod configuration. Serialized to config.json by SMAPI.
/// </summary>
public class ModConfig
{
    // ── Master toggles ──────────────────────────────────────────────────────

    /// <summary>Master switch. When false, the mod awards no XP and applies no bonuses.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Hard cap on player level. XP past this level is discarded.</summary>
    public int LevelCap { get; set; } = 100;

    // ── HUD / feedback ──────────────────────────────────────────────────────

    /// <summary>Show the XP bar HUD element.</summary>
    public bool ShowXpBar { get; set; } = true;

    /// <summary>
    /// When true, draw the original vertical XP bar to the left of the vanilla HP/Energy bars
    /// instead of the horizontal bar above the toolbar.
    /// </summary>
    public bool UseVerticalXpBar { get; set; } = false;

    /// <summary>Show a HUD message when the player levels up.</summary>
    public bool ShowLevelUpNotification { get; set; } = true;

    /// <summary>Play a sound cue when the player levels up.</summary>
    public bool PlayLevelUpSound { get; set; } = true;

    /// <summary>Enable verbose console logging.</summary>
    public bool DebugLogging { get; set; } = false;

    /// <summary>
    /// Optional hotkey that opens this mod's GMCM page directly. <see cref="SButton.None"/>
    /// disables the shortcut (default).
    /// </summary>
    public SButton OpenMenuHotkey { get; set; } = SButton.None;

    // ── Sub-configs ─────────────────────────────────────────────────────────

    public XpSourcesConfig XpSources { get; set; } = new();
    public CurveConfig Curve { get; set; } = new();

    /// <summary>
    /// Always exactly 20 entries. Slots default to disabled; the first 9 are
    /// pre-populated with the default milestone progression.
    /// </summary>
    public List<MilestoneConfig> Milestones { get; set; } = DefaultMilestones();

    /// <summary>
    /// Transient GMCM selector. When set to anything other than
    /// <see cref="MilestonePresets.KeepCurrent"/> and the config is saved, the chosen
    /// preset overwrites <see cref="Milestones"/> and this reverts to keep-current.
    /// </summary>
    public string ApplyMilestonePreset { get; set; } = MilestonePresets.KeepCurrent;

    public const int MilestoneSlotCount = 20;

    /// <summary>
    /// Build the default milestone progression. Called both at first-run config
    /// generation and whenever the user hits "Reset" in GMCM.
    /// </summary>
    public static List<MilestoneConfig> DefaultMilestones()
    {
        var list = new List<MilestoneConfig>(MilestoneSlotCount);

        // The 9 defaults
        list.Add(new MilestoneConfig { Enabled = true, Level = 5,   Name = "Initiate",   MaxHp = 10, MaxEnergy = 10 });
        list.Add(new MilestoneConfig { Enabled = true, Level = 10,  Name = "Apprentice", Attack = 1, Defense = 1, XpMultiplier = 0.05f });
        list.Add(new MilestoneConfig { Enabled = true, Level = 15,  Name = "Journeyman", MaxHp = 10, MaxEnergy = 10, Luck = 1 });
        list.Add(new MilestoneConfig { Enabled = true, Level = 20,  Name = "Adept",      CritChance = 0.05f, WeaponSpeed = 0.05f });
        list.Add(new MilestoneConfig { Enabled = true, Level = 25,  Name = "Veteran",    Attack = 2, Defense = 2, MagneticRadius = 64 });
        list.Add(new MilestoneConfig { Enabled = true, Level = 35,  Name = "Expert",     MaxHp = 20, MaxEnergy = 20, SellPriceBonus = 0.05f, HealthRegenPerMinute = 5f, EnergyRegenPerMinute = 10f });
        list.Add(new MilestoneConfig { Enabled = true, Level = 50,  Name = "Master",     MovementSpeed = 1f, XpMultiplier = 0.10f });
        list.Add(new MilestoneConfig { Enabled = true, Level = 75,  Name = "Champion",   Attack = 3, Defense = 3, CritChance = 0.05f, HealthRegenPerMinute = 10f, EnergyRegenPerMinute = 20f });
        list.Add(new MilestoneConfig { Enabled = true, Level = 100, Name = "Legend",     MaxHp = 50, MaxEnergy = 50, SellPriceBonus = 0.10f, Luck = 1, HealthRegenPerMinute = 25f, EnergyRegenPerMinute = 40f });

        // Pad to MilestoneSlotCount with disabled slots
        while (list.Count < MilestoneSlotCount)
            list.Add(new MilestoneConfig());

        return list;
    }

    /// <summary>
    /// Ensure <see cref="Milestones"/> always has exactly <see cref="MilestoneSlotCount"/>
    /// entries. Called on config load to repair old/short configs.
    /// </summary>
    public void NormalizeMilestoneSlots()
    {
        if (Milestones == null)
            Milestones = new List<MilestoneConfig>();

        while (Milestones.Count < MilestoneSlotCount)
            Milestones.Add(new MilestoneConfig());

        if (Milestones.Count > MilestoneSlotCount)
            Milestones.RemoveRange(MilestoneSlotCount, Milestones.Count - MilestoneSlotCount);
    }
}
