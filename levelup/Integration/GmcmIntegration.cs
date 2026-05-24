using System;
using LevelUp.Config;
using StardewModdingAPI;

namespace LevelUp.Integration;

/// <summary>
/// Wires the mod's config into Generic Mod Config Menu (GMCM).
/// Call <see cref="Register"/> from a GameLaunched handler.
/// </summary>
public class GmcmIntegration
{
    private readonly IModHelper _helper;
    private readonly IManifest _manifest;
    private readonly IMonitor _monitor;
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<ModConfig> _setConfig;
    private readonly Action _onSave;

    private IGenericModConfigMenuApi? _api;

    public GmcmIntegration(
        IModHelper helper,
        IManifest manifest,
        IMonitor monitor,
        Func<ModConfig> getConfig,
        Action<ModConfig> setConfig,
        Action onSave)
    {
        _helper = helper;
        _manifest = manifest;
        _monitor = monitor;
        _getConfig = getConfig;
        _setConfig = setConfig;
        _onSave = onSave;
    }

    public void Register()
    {
        var api = _helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api == null)
        {
            _monitor.Log("GMCM not found — in-game config menu unavailable.", LogLevel.Debug);
            return;
        }
        _api = api;

        api.Register(
            mod: _manifest,
            reset: () => _setConfig(new ModConfig()),
            save: _onSave);

        // ── Main page ───────────────────────────────────────────────────────
        api.AddSectionTitle(_manifest, () => "General");

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().Enabled,
            setValue: v => _getConfig().Enabled = v,
            name: () => "Enable mod",
            tooltip: () => "Master switch. When off, no XP is awarded and no bonuses are applied.");

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().LevelCap,
            setValue: v => _getConfig().LevelCap = v,
            name: () => "Level cap",
            tooltip: () => "Maximum level the player can reach.",
            min: 10, max: 999, interval: 1);

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().ShowXpBar,
            setValue: v => _getConfig().ShowXpBar = v,
            name: () => "Show XP bar",
            tooltip: () => "Display an XP bar next to the HP and Energy bars.");

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().ShowLevelUpNotification,
            setValue: v => _getConfig().ShowLevelUpNotification = v,
            name: () => "Show level-up notification");

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().PlayLevelUpSound,
            setValue: v => _getConfig().PlayLevelUpSound = v,
            name: () => "Play level-up sound");

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().DebugLogging,
            setValue: v => _getConfig().DebugLogging = v,
            name: () => "Debug logging",
            tooltip: () => "Verbose console output. Useful for troubleshooting.");

        api.AddKeybind(_manifest,
            getValue: () => _getConfig().OpenMenuHotkey,
            setValue: v => _getConfig().OpenMenuHotkey = v,
            name: () => "Open menu hotkey",
            tooltip: () => "Press this key in-game to jump straight to this options menu. Leave unset to disable.");

        api.AddPageLink(_manifest, "xp-sources", () => "→ XP Sources");
        api.AddPageLink(_manifest, "curve", () => "→ XP Curve");
        api.AddPageLink(_manifest, "milestones", () => "→ Milestones");

        // ── XP Sources page ─────────────────────────────────────────────────
        api.AddPage(_manifest, "xp-sources", () => "XP Sources");
        AddXpSourcesPage(api);

        // ── Curve page ──────────────────────────────────────────────────────
        api.AddPage(_manifest, "curve", () => "XP Curve");
        AddCurvePage(api);

        // ── Milestones index page ───────────────────────────────────────────
        api.AddPage(_manifest, "milestones", () => "Milestones");
        AddMilestonesIndexPage(api);

        // ── One sub-page per milestone slot ─────────────────────────────────
        for (int i = 0; i < ModConfig.MilestoneSlotCount; i++)
        {
            int slotIndex = i; // capture for closures
            api.AddPage(_manifest, $"milestone-{slotIndex}", () => $"Milestone Slot {slotIndex + 1}");
            AddMilestonePage(api, slotIndex);
        }
    }

    /// <summary>
    /// Open this mod's GMCM page programmatically. No-op if GMCM isn't installed.
    /// </summary>
    public bool OpenMenu()
    {
        if (_api == null) return false;
        _api.OpenModMenu(_manifest);
        return true;
    }

    // ── Page builders ───────────────────────────────────────────────────────

    private void AddXpSourcesPage(IGenericModConfigMenuApi api)
    {
        api.AddSectionTitle(_manifest, () => "Monster kills");
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.MonsterKillEnabled,
            setValue: v => _getConfig().XpSources.MonsterKillEnabled = v,
            name: () => "Enabled");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.MonsterXpPerMaxHp,
            setValue: v => _getConfig().XpSources.MonsterXpPerMaxHp = v,
            name: () => "XP per max HP",
            tooltip: () => "A 100-HP monster gives this × 100 XP.",
            min: 0f, max: 10f, interval: 0.1f);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.BossKillMultiplier,
            setValue: v => _getConfig().XpSources.BossKillMultiplier = v,
            name: () => "Boss multiplier",
            min: 1f, max: 50f, interval: 0.5f);

        api.AddSectionTitle(_manifest, () => "Day survived");
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.DaySurvivedEnabled,
            setValue: v => _getConfig().XpSources.DaySurvivedEnabled = v,
            name: () => "Enabled");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.DaySurvivedXp,
            setValue: v => _getConfig().XpSources.DaySurvivedXp = v,
            name: () => "XP per day",
            min: 0, max: 10000, interval: 5);
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.DaySurvivedSkipOnPassout,
            setValue: v => _getConfig().XpSources.DaySurvivedSkipOnPassout = v,
            name: () => "Skip on passout");

        api.AddSectionTitle(_manifest, () => "Quests");
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.QuestEnabled,
            setValue: v => _getConfig().XpSources.QuestEnabled = v,
            name: () => "Enabled");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.StoryQuestXp,
            setValue: v => _getConfig().XpSources.StoryQuestXp = v,
            name: () => "Story quest XP",
            min: 0, max: 10000, interval: 10);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.HelpWantedQuestXp,
            setValue: v => _getConfig().XpSources.HelpWantedQuestXp = v,
            name: () => "Help Wanted XP",
            min: 0, max: 10000, interval: 5);

        api.AddSectionTitle(_manifest, () => "Optional");
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.FestivalEnabled,
            setValue: v => _getConfig().XpSources.FestivalEnabled = v,
            name: () => "Festival attendance");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.FestivalXp,
            setValue: v => _getConfig().XpSources.FestivalXp = v,
            name: () => "Festival XP",
            min: 0, max: 10000, interval: 5);
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.NewAreaEnabled,
            setValue: v => _getConfig().XpSources.NewAreaEnabled = v,
            name: () => "New area discovery");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.NewAreaXp,
            setValue: v => _getConfig().XpSources.NewAreaXp = v,
            name: () => "New area XP",
            min: 0, max: 10000, interval: 5);

        api.AddSectionTitle(_manifest, () => "Skills");
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillLevelUpEnabled,
            setValue: v => _getConfig().XpSources.SkillLevelUpEnabled = v,
            name: () => "Skill level-up",
            tooltip: () => "Award XP when a vanilla skill (Farming, Fishing, Foraging, Mining, Combat) levels up.");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillLevelUpXp,
            setValue: v => _getConfig().XpSources.SkillLevelUpXp = v,
            name: () => "XP per skill level",
            min: 0, max: 10000, interval: 25);
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillXpEnabled,
            setValue: v => _getConfig().XpSources.SkillXpEnabled = v,
            name: () => "Scale with skill XP",
            tooltip: () => "Earn meta XP as a fraction of all vanilla skill XP (farming, fishing, chopping, mining, foraging, combat).");
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillXpRate,
            setValue: v => _getConfig().XpSources.SkillXpRate = v,
            name: () => "Skill XP rate",
            tooltip: () => "Fraction of earned skill XP converted to meta XP. 0.10 = 10%.",
            min: 0f, max: 2f, interval: 0.05f);
    }

    private void AddCurvePage(IGenericModConfigMenuApi api)
    {
        api.AddParagraph(_manifest, () => "XP needed per level grows as: base × growth^(level-1)");

        api.AddTextOption(_manifest,
            getValue: () => _getConfig().Curve.Preset.ToString(),
            setValue: v => { if (Enum.TryParse<CurvePreset>(v, out var p)) _getConfig().Curve.Preset = p; },
            name: () => "Preset",
            allowedValues: new[] { "Casual", "Standard", "Hardcore", "Custom" });

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().Curve.BaseXp,
            setValue: v => _getConfig().Curve.BaseXp = v,
            name: () => "Base XP (Custom only)",
            tooltip: () => "XP for level 1 → 2. Only used when preset is Custom.",
            min: 10, max: 10000, interval: 5);

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().Curve.GrowthRate,
            setValue: v => _getConfig().Curve.GrowthRate = v,
            name: () => "Growth rate (Custom only)",
            tooltip: () => "Per-level multiplier. 1.0 = linear, 1.15 = +15% each level.",
            min: 1.00f, max: 2.00f, interval: 0.01f);
    }

    private void AddMilestonesIndexPage(IGenericModConfigMenuApi api)
    {
        api.AddParagraph(_manifest, () =>
            "20 milestone slots. Enable a slot, set its level + name, then assign bonuses. " +
            "All enabled milestones whose level you've reached are summed and applied.");

        api.AddSectionTitle(_manifest, () => "Preset");
        api.AddTextOption(_manifest,
            getValue: () => _getConfig().ApplyMilestonePreset,
            setValue: v => _getConfig().ApplyMilestonePreset = v,
            name: () => "Apply preset",
            tooltip: () =>
                "Pick a play-style preset, then Save to overwrite all 20 slots with it. " +
                "Every slot stays editable afterward; this resets to \"(keep current)\" once applied.",
            allowedValues: MilestonePresets.Names);

        for (int i = 0; i < ModConfig.MilestoneSlotCount; i++)
        {
            int slotIndex = i;
            api.AddPageLink(_manifest, $"milestone-{slotIndex}",
                text: () =>
                {
                    var m = _getConfig().Milestones[slotIndex];
                    if (!m.Enabled) return $"Slot {slotIndex + 1}: (disabled)";
                    return $"Slot {slotIndex + 1}: Lv {m.Level} — {(string.IsNullOrEmpty(m.Name) ? "(unnamed)" : m.Name)}";
                });
        }
    }

    private void AddMilestonePage(IGenericModConfigMenuApi api, int slotIndex)
    {
        // Local helpers to keep registrations tight
        MilestoneConfig M() => _getConfig().Milestones[slotIndex];

        api.AddSectionTitle(_manifest, () => "Slot");
        api.AddBoolOption(_manifest,
            getValue: () => M().Enabled, setValue: v => M().Enabled = v,
            name: () => "Enabled");
        api.AddNumberOption(_manifest,
            getValue: () => M().Level, setValue: v => M().Level = v,
            name: () => "Required level",
            min: 1, max: 999, interval: 1);
        api.AddTextOption(_manifest,
            getValue: () => M().Name, setValue: v => M().Name = v,
            name: () => "Name");

        api.AddSectionTitle(_manifest, () => "Vitals");
        api.AddNumberOption(_manifest,
            getValue: () => M().MaxHp, setValue: v => M().MaxHp = v,
            name: () => "+Max HP", min: 0, max: 9999, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().MaxEnergy, setValue: v => M().MaxEnergy = v,
            name: () => "+Max Energy", min: 0, max: 9999, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().HealthRegenPerMinute, setValue: v => M().HealthRegenPerMinute = v,
            name: () => "+HP regen / min",
            tooltip: () => "Health restored per real-time minute while time is passing. 0 disables.",
            min: 0f, max: 100f, interval: 0.5f);
        api.AddNumberOption(_manifest,
            getValue: () => M().EnergyRegenPerMinute, setValue: v => M().EnergyRegenPerMinute = v,
            name: () => "+Energy regen / min",
            tooltip: () => "Energy restored per real-time minute while time is passing. 0 disables.",
            min: 0f, max: 100f, interval: 0.5f);

        api.AddSectionTitle(_manifest, () => "Combat");
        api.AddNumberOption(_manifest,
            getValue: () => M().Attack, setValue: v => M().Attack = v,
            name: () => "+Attack", min: 0, max: 999, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().Defense, setValue: v => M().Defense = v,
            name: () => "+Defense", min: 0, max: 999, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().CritChance, setValue: v => M().CritChance = v,
            name: () => "+Crit chance",
            tooltip: () => "Additive multiplier. 0.05 = +5% crit.",
            min: 0f, max: 2f, interval: 0.01f);
        api.AddNumberOption(_manifest,
            getValue: () => M().WeaponSpeed, setValue: v => M().WeaponSpeed = v,
            name: () => "+Weapon speed",
            tooltip: () => "Additive multiplier. 0.05 = +5% swing speed.",
            min: 0f, max: 2f, interval: 0.01f);

        api.AddSectionTitle(_manifest, () => "Utility");
        api.AddNumberOption(_manifest,
            getValue: () => M().MovementSpeed, setValue: v => M().MovementSpeed = v,
            name: () => "+Movement speed",
            tooltip: () => "Raw game units. +1 is roughly +20% walk speed.",
            min: 0f, max: 10f, interval: 0.1f);
        api.AddNumberOption(_manifest,
            getValue: () => M().MagneticRadius, setValue: v => M().MagneticRadius = v,
            name: () => "+Magnetic radius",
            tooltip: () => "Game units. Vanilla base is 128.",
            min: 0, max: 999, interval: 8);
        api.AddNumberOption(_manifest,
            getValue: () => M().Luck, setValue: v => M().Luck = v,
            name: () => "+Luck", min: 0, max: 99, interval: 1);

        api.AddSectionTitle(_manifest, () => "Resource");
        api.AddNumberOption(_manifest,
            getValue: () => M().XpMultiplier, setValue: v => M().XpMultiplier = v,
            name: () => "+XP gain",
            tooltip: () => "Additive bonus to all skill XP. 0.10 = +10% XP.",
            min: 0f, max: 5f, interval: 0.01f);
        api.AddNumberOption(_manifest,
            getValue: () => M().SellPriceBonus, setValue: v => M().SellPriceBonus = v,
            name: () => "+Sell price",
            tooltip: () => "Additive bonus to shop sell prices. 0.05 = +5%.",
            min: 0f, max: 5f, interval: 0.01f);
    }
}
