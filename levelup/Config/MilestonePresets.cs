using System.Collections.Generic;

namespace LevelUp.Config;

/// <summary>
/// Ready-made milestone progressions for different play styles. Unlike the curve preset
/// (resolved live from an enum), a milestone preset is <em>applied</em>: selecting one in
/// GMCM and saving overwrites the 20 slots with that preset's values, after which every
/// slot remains individually editable. The selector then reverts to <see cref="KeepCurrent"/>.
/// </summary>
public static class MilestonePresets
{
    public const string KeepCurrent = "(keep current)";

    /// <summary>Allowed values for the GMCM dropdown, in display order.</summary>
    public static readonly string[] Names =
    {
        KeepCurrent,
        "Balanced",
        "Combat",
        "Survivalist",
        "Explorer",
        "Minimalist",
        "Empty",
    };

    /// <summary>
    /// Build the 20-slot list for a preset name, or <c>null</c> if the selection is
    /// <see cref="KeepCurrent"/> or unrecognized (meaning: don't change anything).
    /// </summary>
    public static List<MilestoneConfig>? Build(string? name)
    {
        return name switch
        {
            "Balanced"    => Pad(ModConfig.DefaultMilestones()),
            "Combat"      => Pad(Combat()),
            "Survivalist" => Pad(Survivalist()),
            "Explorer"    => Pad(Explorer()),
            "Minimalist"  => Pad(Minimalist()),
            "Empty"       => Pad(new List<MilestoneConfig>()),
            _             => null,
        };
    }

    /// <summary>Pad/trim to exactly <see cref="ModConfig.MilestoneSlotCount"/> entries.</summary>
    private static List<MilestoneConfig> Pad(List<MilestoneConfig> list)
    {
        while (list.Count < ModConfig.MilestoneSlotCount)
            list.Add(new MilestoneConfig());
        if (list.Count > ModConfig.MilestoneSlotCount)
            list.RemoveRange(ModConfig.MilestoneSlotCount, list.Count - ModConfig.MilestoneSlotCount);
        return list;
    }

    // Attack/defense/crit/weapon-speed focused, light HP. HP regen at higher tiers fits the
    // "battle-hardened" fantasy; no energy regen — Combat doesn't lean on stamina. Endgame
    // cumulative ≈ 35 HP/min, full-heals the 165-HP pool in ~5 minutes.
    private static List<MilestoneConfig> Combat() => new()
    {
        new() { Enabled = true, Level = 5,   Name = "Recruit",    Attack = 1, Defense = 1 },
        new() { Enabled = true, Level = 10,  Name = "Fighter",    CritChance = 0.03f, WeaponSpeed = 0.05f },
        new() { Enabled = true, Level = 15,  Name = "Soldier",    Attack = 2, Defense = 2 },
        new() { Enabled = true, Level = 20,  Name = "Warrior",    MaxHp = 15, CritChance = 0.03f },
        new() { Enabled = true, Level = 25,  Name = "Vanguard",   Attack = 3, Defense = 3, WeaponSpeed = 0.05f },
        new() { Enabled = true, Level = 35,  Name = "Slayer",     CritChance = 0.05f, MaxHp = 20 },
        new() { Enabled = true, Level = 50,  Name = "Warlord",    Attack = 5, Defense = 5, HealthRegenPerMinute = 5f },
        new() { Enabled = true, Level = 75,  Name = "Berserker",  CritChance = 0.07f, WeaponSpeed = 0.10f, MovementSpeed = 0.5f, HealthRegenPerMinute = 10f },
        new() { Enabled = true, Level = 100, Name = "Godslayer",  Attack = 10, Defense = 10, CritChance = 0.10f, MaxHp = 30, HealthRegenPerMinute = 20f },
    };

    // Big max HP/energy + defense + a little luck (tanky, long runs). Regen is the signature
    // mechanic here — present from mid-tier on; cumulative endgame rate is 70 HP/min +
    // 110 EN/min, full-heals the 340-HP / 550-EN pool in ~5 minutes.
    private static List<MilestoneConfig> Survivalist() => new()
    {
        new() { Enabled = true, Level = 5,   Name = "Hardy",        MaxHp = 15, MaxEnergy = 15 },
        new() { Enabled = true, Level = 10,  Name = "Tough",        Defense = 2, MaxEnergy = 15 },
        new() { Enabled = true, Level = 15,  Name = "Resilient",    MaxHp = 20, MaxEnergy = 20, HealthRegenPerMinute = 5f, EnergyRegenPerMinute = 10f },
        new() { Enabled = true, Level = 20,  Name = "Stalwart",     Defense = 3, Luck = 1 },
        new() { Enabled = true, Level = 25,  Name = "Enduring",     MaxHp = 30, MaxEnergy = 30, HealthRegenPerMinute = 10f, EnergyRegenPerMinute = 15f },
        new() { Enabled = true, Level = 35,  Name = "Ironhide",     Defense = 5, MaxHp = 25 },
        new() { Enabled = true, Level = 50,  Name = "Unbreakable",  MaxHp = 50, MaxEnergy = 50, HealthRegenPerMinute = 20f, EnergyRegenPerMinute = 30f },
        new() { Enabled = true, Level = 75,  Name = "Juggernaut",   Defense = 8, MaxEnergy = 50, Luck = 1 },
        new() { Enabled = true, Level = 100, Name = "Immortal",     MaxHp = 100, MaxEnergy = 100, Defense = 10, HealthRegenPerMinute = 35f, EnergyRegenPerMinute = 55f },
    };

    // Movement, magnetic radius, luck, XP gain, sell price (QoL / economy). Energy regen on
    // the long-haul tiers — exploring drains stamina, so a meaningful trickle keeps the
    // player moving; no HP regen (Explorer isn't a combat preset). Endgame cumulative ≈
    // 55 EN/min, refills the 270-EN pool in ~5 minutes.
    private static List<MilestoneConfig> Explorer() => new()
    {
        new() { Enabled = true, Level = 5,   Name = "Wanderer",    MagneticRadius = 32, MovementSpeed = 0.25f },
        new() { Enabled = true, Level = 10,  Name = "Scout",       XpMultiplier = 0.05f, MagneticRadius = 32 },
        new() { Enabled = true, Level = 15,  Name = "Pathfinder",  MovementSpeed = 0.5f, Luck = 1 },
        new() { Enabled = true, Level = 20,  Name = "Trader",      SellPriceBonus = 0.05f },
        new() { Enabled = true, Level = 25,  Name = "Pioneer",     MagneticRadius = 64, MovementSpeed = 0.5f, EnergyRegenPerMinute = 10f },
        new() { Enabled = true, Level = 35,  Name = "Prospector",  Luck = 1, XpMultiplier = 0.10f },
        new() { Enabled = true, Level = 50,  Name = "Trailblazer", MovementSpeed = 1f, SellPriceBonus = 0.05f, EnergyRegenPerMinute = 15f },
        new() { Enabled = true, Level = 75,  Name = "Voyager",     MagneticRadius = 128, Luck = 2 },
        new() { Enabled = true, Level = 100, Name = "Pathlord",    MovementSpeed = 1f, SellPriceBonus = 0.15f, XpMultiplier = 0.15f, Luck = 2, EnergyRegenPerMinute = 30f },
    };

    // Small bonuses, fewer slots — levels feel rewarding without trivializing the game.
    // Regen is the one big capstone payoff at Grandmaster, sized to refill the modest
    // 130-HP / 300-EN pool in ~5 minutes.
    private static List<MilestoneConfig> Minimalist() => new()
    {
        new() { Enabled = true, Level = 10,  Name = "Apprentice",  MaxHp = 5,  MaxEnergy = 5 },
        new() { Enabled = true, Level = 25,  Name = "Adept",       Attack = 1, Defense = 1 },
        new() { Enabled = true, Level = 50,  Name = "Expert",      MaxHp = 10, MaxEnergy = 10, XpMultiplier = 0.05f },
        new() { Enabled = true, Level = 75,  Name = "Master",      Attack = 2, Defense = 2 },
        new() { Enabled = true, Level = 100, Name = "Grandmaster", MaxHp = 15, MaxEnergy = 15, Luck = 1, HealthRegenPerMinute = 25f, EnergyRegenPerMinute = 60f },
    };
}
