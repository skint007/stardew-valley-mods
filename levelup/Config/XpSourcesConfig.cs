namespace LevelUp.Config;

/// <summary>
/// Controls which in-world actions award player XP and how much.
/// </summary>
public class XpSourcesConfig
{
    // ── Monster kills ───────────────────────────────────────────────────────

    /// <summary>Award XP for killing monsters.</summary>
    public bool MonsterKillEnabled { get; set; } = true;

    /// <summary>XP per point of monster max-HP (0.25 = a 50-HP monster gives ~12 XP).</summary>
    public float MonsterXpPerMaxHp { get; set; } = 0.25f;

    /// <summary>Multiplier applied on top when the monster is a boss / mine-floor boss.</summary>
    public float BossKillMultiplier { get; set; } = 2f;

    // ── Days survived ───────────────────────────────────────────────────────

    /// <summary>Award XP every day at end-of-day.</summary>
    public bool DaySurvivedEnabled { get; set; } = true;

    /// <summary>XP awarded per day survived.</summary>
    public int DaySurvivedXp { get; set; } = 50;

    /// <summary>Don't award the day-survived XP if the player passed out.</summary>
    public bool DaySurvivedSkipOnPassout { get; set; } = true;

    // ── Quests ──────────────────────────────────────────────────────────────

    /// <summary>Award XP for completing quests.</summary>
    public bool QuestEnabled { get; set; } = true;

    /// <summary>XP for completing a story (non-billboard) quest.</summary>
    public int StoryQuestXp { get; set; } = 100;

    /// <summary>XP for completing a "Help Wanted" billboard quest.</summary>
    public int HelpWantedQuestXp { get; set; } = 25;

    // ── Optional sources (off by default) ───────────────────────────────────

    /// <summary>Award XP for attending a festival.</summary>
    public bool FestivalEnabled { get; set; } = false;

    /// <summary>XP for attending a festival.</summary>
    public int FestivalXp { get; set; } = 75;

    /// <summary>Award XP the first time the player enters a new location.</summary>
    public bool NewAreaEnabled { get; set; } = false;

    /// <summary>XP for discovering a new location.</summary>
    public int NewAreaXp { get; set; } = 25;

    // ── Skills ──────────────────────────────────────────────────────────────

    /// <summary>Award XP when a vanilla skill (Farming/Fishing/Foraging/Mining/Combat) levels up.</summary>
    public bool SkillLevelUpEnabled { get; set; } = true;

    /// <summary>XP per vanilla skill level gained.</summary>
    public int SkillLevelUpXp { get; set; } = 150;

    /// <summary>
    /// Award meta XP as a fraction of the vanilla skill XP earned (so every productive
    /// task — harvesting, fishing, chopping, mining, foraging, combat — feeds it).
    /// </summary>
    public bool SkillXpEnabled { get; set; } = true;

    /// <summary>Fraction of earned skill XP converted to meta XP (0.1 = 10%).</summary>
    public float SkillXpRate { get; set; } = 0.1f;
}
