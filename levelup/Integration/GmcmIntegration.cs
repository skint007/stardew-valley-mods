using System;
using LevelUp.Config;
using StardewModdingAPI;

namespace LevelUp.Integration;

/// <summary>
/// Wires the mod's config into Generic Mod Config Menu (GMCM).
/// Call <see cref="Register"/> from a GameLaunched handler.
///
/// All visible strings are pulled from the mod's i18n files via <see cref="T(string)"/>, so the
/// config UI is localized. GMCM re-reads the label/tooltip funcs each time the menu opens, so a
/// language change takes effect without a restart. Dropdown <em>values</em> (curve + milestone
/// presets) keep their stable English keys — only their displayed labels are translated, via
/// <c>formatAllowedValue</c>.
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

    // ── Translation helpers ───────────────────────────────────────────────────

    /// <summary>Look up a translation by key. Relies on the implicit Translation→string cast.</summary>
    private string T(string key) => _helper.Translation.Get(key);

    /// <summary>Look up a translation by key with token replacements (e.g. <c>{{number}}</c>).</summary>
    private string T(string key, object tokens) => _helper.Translation.Get(key, tokens);

    /// <summary>Translate a milestone-preset value for display; the stored value stays English.</summary>
    private string TranslatePresetName(string value) => value switch
    {
        MilestonePresets.KeepCurrent => T("preset.value.keep-current"),
        "Balanced"    => T("preset.value.balanced"),
        "Combat"      => T("preset.value.combat"),
        "Survivalist" => T("preset.value.survivalist"),
        "Explorer"    => T("preset.value.explorer"),
        "Minimalist"  => T("preset.value.minimalist"),
        "Empty"       => T("preset.value.empty"),
        _             => value,
    };

    /// <summary>Translate a curve-preset value for display; the stored enum value stays English.</summary>
    private string TranslateCurvePreset(string value) => value switch
    {
        "Casual"   => T("curve.preset.value.casual"),
        "Standard" => T("curve.preset.value.standard"),
        "Hardcore" => T("curve.preset.value.hardcore"),
        "Custom"   => T("curve.preset.value.custom"),
        _          => value,
    };

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
        api.AddSectionTitle(_manifest, () => T("config.section.general"));

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().Enabled,
            setValue: v => _getConfig().Enabled = v,
            name: () => T("config.enabled.name"),
            tooltip: () => T("config.enabled.tooltip"));

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().LevelCap,
            setValue: v => _getConfig().LevelCap = v,
            name: () => T("config.level-cap.name"),
            tooltip: () => T("config.level-cap.tooltip"),
            min: 10, max: 999, interval: 1);

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().ShowXpBar,
            setValue: v => _getConfig().ShowXpBar = v,
            name: () => T("config.show-xp-bar.name"),
            tooltip: () => T("config.show-xp-bar.tooltip"));

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().UseVerticalXpBar,
            setValue: v => _getConfig().UseVerticalXpBar = v,
            name: () => T("config.vertical-xp-bar.name"),
            tooltip: () => T("config.vertical-xp-bar.tooltip"));

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().FadeVerticalBarWhenIdle,
            setValue: v => _getConfig().FadeVerticalBarWhenIdle = v,
            name: () => T("config.fade-vertical-bar.name"),
            tooltip: () => T("config.fade-vertical-bar.tooltip"));

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpBarScale,
            setValue: v => _getConfig().XpBarScale = v,
            name: () => T("config.xp-bar-scale.name"),
            tooltip: () => T("config.xp-bar-scale.tooltip"),
            min: 0.5f, max: 1.5f, interval: 0.05f);

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().ShowLevelUpNotification,
            setValue: v => _getConfig().ShowLevelUpNotification = v,
            name: () => T("config.show-notification.name"));

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().PlayLevelUpSound,
            setValue: v => _getConfig().PlayLevelUpSound = v,
            name: () => T("config.play-sound.name"));

        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().DebugLogging,
            setValue: v => _getConfig().DebugLogging = v,
            name: () => T("config.debug-logging.name"),
            tooltip: () => T("config.debug-logging.tooltip"));

        api.AddKeybind(_manifest,
            getValue: () => _getConfig().OpenMenuHotkey,
            setValue: v => _getConfig().OpenMenuHotkey = v,
            name: () => T("config.open-menu-hotkey.name"),
            tooltip: () => T("config.open-menu-hotkey.tooltip"));

        api.AddPageLink(_manifest, "xp-sources", () => T("link.xp-sources"));
        api.AddPageLink(_manifest, "curve", () => T("link.curve"));
        api.AddPageLink(_manifest, "milestones", () => T("link.milestones"));

        // ── XP Sources page ─────────────────────────────────────────────────
        api.AddPage(_manifest, "xp-sources", () => T("page.xp-sources"));
        AddXpSourcesPage(api);

        // ── Curve page ──────────────────────────────────────────────────────
        api.AddPage(_manifest, "curve", () => T("page.curve"));
        AddCurvePage(api);

        // ── Milestones index page ───────────────────────────────────────────
        api.AddPage(_manifest, "milestones", () => T("page.milestones"));
        AddMilestonesIndexPage(api);

        // ── One sub-page per milestone slot ─────────────────────────────────
        for (int i = 0; i < ModConfig.MilestoneSlotCount; i++)
        {
            int slotIndex = i; // capture for closures
            api.AddPage(_manifest, $"milestone-{slotIndex}", () => T("page.milestone-slot", new { number = slotIndex + 1 }));
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
        api.AddSectionTitle(_manifest, () => T("xp.section.monster-kills"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.MonsterKillEnabled,
            setValue: v => _getConfig().XpSources.MonsterKillEnabled = v,
            name: () => T("common.enabled.name"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.MonsterXpPerMaxHp,
            setValue: v => _getConfig().XpSources.MonsterXpPerMaxHp = v,
            name: () => T("xp.monster-per-hp.name"),
            tooltip: () => T("xp.monster-per-hp.tooltip"),
            min: 0f, max: 10f, interval: 0.1f);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.BossKillMultiplier,
            setValue: v => _getConfig().XpSources.BossKillMultiplier = v,
            name: () => T("xp.boss-multiplier.name"),
            min: 1f, max: 50f, interval: 0.5f);

        api.AddSectionTitle(_manifest, () => T("xp.section.day-survived"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.DaySurvivedEnabled,
            setValue: v => _getConfig().XpSources.DaySurvivedEnabled = v,
            name: () => T("common.enabled.name"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.DaySurvivedXp,
            setValue: v => _getConfig().XpSources.DaySurvivedXp = v,
            name: () => T("xp.day-xp.name"),
            min: 0, max: 2000, interval: 5);
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.DaySurvivedSkipOnPassout,
            setValue: v => _getConfig().XpSources.DaySurvivedSkipOnPassout = v,
            name: () => T("xp.skip-passout.name"));

        api.AddSectionTitle(_manifest, () => T("xp.section.quests"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.QuestEnabled,
            setValue: v => _getConfig().XpSources.QuestEnabled = v,
            name: () => T("common.enabled.name"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.StoryQuestXp,
            setValue: v => _getConfig().XpSources.StoryQuestXp = v,
            name: () => T("xp.story-quest.name"),
            min: 0, max: 2000, interval: 10);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.HelpWantedQuestXp,
            setValue: v => _getConfig().XpSources.HelpWantedQuestXp = v,
            name: () => T("xp.help-wanted.name"),
            min: 0, max: 2000, interval: 5);

        api.AddSectionTitle(_manifest, () => T("xp.section.optional"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.FestivalEnabled,
            setValue: v => _getConfig().XpSources.FestivalEnabled = v,
            name: () => T("xp.festival.name"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.FestivalXp,
            setValue: v => _getConfig().XpSources.FestivalXp = v,
            name: () => T("xp.festival-xp.name"),
            min: 0, max: 2000, interval: 5);
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.NewAreaEnabled,
            setValue: v => _getConfig().XpSources.NewAreaEnabled = v,
            name: () => T("xp.new-area.name"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.NewAreaXp,
            setValue: v => _getConfig().XpSources.NewAreaXp = v,
            name: () => T("xp.new-area-xp.name"),
            min: 0, max: 2000, interval: 5);

        api.AddSectionTitle(_manifest, () => T("xp.section.skills"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillLevelUpEnabled,
            setValue: v => _getConfig().XpSources.SkillLevelUpEnabled = v,
            name: () => T("xp.skill-levelup.name"),
            tooltip: () => T("xp.skill-levelup.tooltip"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillLevelUpXp,
            setValue: v => _getConfig().XpSources.SkillLevelUpXp = v,
            name: () => T("xp.skill-levelup-xp.name"),
            min: 0, max: 2000, interval: 25);
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillXpEnabled,
            setValue: v => _getConfig().XpSources.SkillXpEnabled = v,
            name: () => T("xp.skill-xp.name"),
            tooltip: () => T("xp.skill-xp.tooltip"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().XpSources.SkillXpRate,
            setValue: v => _getConfig().XpSources.SkillXpRate = v,
            name: () => T("xp.skill-xp-rate.name"),
            tooltip: () => T("xp.skill-xp-rate.tooltip"),
            min: 0f, max: 2f, interval: 0.05f);
    }

    private void AddCurvePage(IGenericModConfigMenuApi api)
    {
        api.AddParagraph(_manifest, () => T("curve.paragraph"));

        api.AddTextOption(_manifest,
            getValue: () => _getConfig().Curve.Preset.ToString(),
            setValue: v => { if (Enum.TryParse<CurvePreset>(v, out var p)) _getConfig().Curve.Preset = p; },
            name: () => T("curve.preset.name"),
            allowedValues: new[] { "Casual", "Standard", "Hardcore", "Custom" },
            formatAllowedValue: TranslateCurvePreset);

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().Curve.BaseXp,
            setValue: v => _getConfig().Curve.BaseXp = v,
            name: () => T("curve.base.name"),
            tooltip: () => T("curve.base.tooltip"),
            min: 10, max: 1000, interval: 5);

        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().Curve.GrowthRate,
            setValue: v => _getConfig().Curve.GrowthRate = v,
            name: () => T("curve.growth.name"),
            tooltip: () => T("curve.growth.tooltip"),
            min: 1.00f, max: 1.50f, interval: 0.01f,
            formatValue: v => v.ToString("0.00"));
    }

    private void AddMilestonesIndexPage(IGenericModConfigMenuApi api)
    {
        api.AddParagraph(_manifest, () => T("milestones.paragraph"));

        api.AddSectionTitle(_manifest, () => T("milestones.section.preset"));
        api.AddTextOption(_manifest,
            getValue: () => _getConfig().ApplyMilestonePreset,
            setValue: v => _getConfig().ApplyMilestonePreset = v,
            name: () => T("milestones.apply-preset.name"),
            tooltip: () => T("milestones.apply-preset.tooltip"),
            allowedValues: MilestonePresets.Names,
            formatAllowedValue: TranslatePresetName);

        for (int i = 0; i < ModConfig.MilestoneSlotCount; i++)
        {
            int slotIndex = i;
            api.AddPageLink(_manifest, $"milestone-{slotIndex}",
                text: () =>
                {
                    var m = _getConfig().Milestones[slotIndex];
                    if (!m.Enabled) return T("milestones.link.disabled", new { number = slotIndex + 1 });
                    string name = string.IsNullOrEmpty(m.Name) ? T("milestones.unnamed") : m.Name;
                    return T("milestones.link.active", new { number = slotIndex + 1, level = m.Level, name });
                });
        }
    }

    private void AddMilestonePage(IGenericModConfigMenuApi api, int slotIndex)
    {
        // Local helpers to keep registrations tight
        MilestoneConfig M() => _getConfig().Milestones[slotIndex];

        // GMCM's sub-page nav doesn't have a true "back" stack: backing out of a milestone
        // slot lands on the mod root rather than the Milestones index. Surface an explicit
        // link at the top so users can jump back to pick another slot.
        api.AddPageLink(_manifest, "milestones", () => T("milestone.back-to-milestones"));

        api.AddSectionTitle(_manifest, () => T("milestone.section.slot"));
        api.AddBoolOption(_manifest,
            getValue: () => M().Enabled, setValue: v => M().Enabled = v,
            name: () => T("common.enabled.name"));
        api.AddNumberOption(_manifest,
            getValue: () => M().Level, setValue: v => M().Level = v,
            name: () => T("milestone.level.name"),
            min: 1, max: 999, interval: 1);
        api.AddTextOption(_manifest,
            getValue: () => M().Name, setValue: v => M().Name = v,
            name: () => T("milestone.name.name"));

        api.AddSectionTitle(_manifest, () => T("milestone.section.vitals"));
        api.AddNumberOption(_manifest,
            getValue: () => M().MaxHp, setValue: v => M().MaxHp = v,
            name: () => T("milestone.max-hp.name"), min: 0, max: 500, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().MaxEnergy, setValue: v => M().MaxEnergy = v,
            name: () => T("milestone.max-energy.name"), min: 0, max: 500, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().HealthRegenPerMinute, setValue: v => M().HealthRegenPerMinute = v,
            name: () => T("milestone.hp-regen.name"),
            tooltip: () => T("milestone.hp-regen.tooltip"),
            min: 0f, max: 100f, interval: 0.5f);
        api.AddNumberOption(_manifest,
            getValue: () => M().EnergyRegenPerMinute, setValue: v => M().EnergyRegenPerMinute = v,
            name: () => T("milestone.energy-regen.name"),
            tooltip: () => T("milestone.energy-regen.tooltip"),
            min: 0f, max: 100f, interval: 0.5f);

        api.AddSectionTitle(_manifest, () => T("milestone.section.combat"));
        api.AddNumberOption(_manifest,
            getValue: () => M().Attack, setValue: v => M().Attack = v,
            name: () => T("milestone.attack.name"), min: 0, max: 50, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().Defense, setValue: v => M().Defense = v,
            name: () => T("milestone.defense.name"), min: 0, max: 50, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => M().CritChance, setValue: v => M().CritChance = v,
            name: () => T("milestone.crit.name"),
            tooltip: () => T("milestone.crit.tooltip"),
            min: 0f, max: 2f, interval: 0.01f,
            formatValue: v => v.ToString("0.00"));
        api.AddNumberOption(_manifest,
            getValue: () => M().WeaponSpeed, setValue: v => M().WeaponSpeed = v,
            name: () => T("milestone.weapon-speed.name"),
            tooltip: () => T("milestone.weapon-speed.tooltip"),
            min: 0f, max: 2f, interval: 0.01f,
            formatValue: v => v.ToString("0.00"));

        api.AddSectionTitle(_manifest, () => T("milestone.section.utility"));
        api.AddNumberOption(_manifest,
            getValue: () => M().MovementSpeed, setValue: v => M().MovementSpeed = v,
            name: () => T("milestone.movement.name"),
            tooltip: () => T("milestone.movement.tooltip"),
            min: 0f, max: 10f, interval: 0.1f);
        api.AddNumberOption(_manifest,
            getValue: () => M().MagneticRadius, setValue: v => M().MagneticRadius = v,
            name: () => T("milestone.magnetic.name"),
            tooltip: () => T("milestone.magnetic.tooltip"),
            min: 0, max: 512, interval: 8);
        api.AddNumberOption(_manifest,
            getValue: () => M().Luck, setValue: v => M().Luck = v,
            name: () => T("milestone.luck.name"), min: 0, max: 20, interval: 1);

        api.AddSectionTitle(_manifest, () => T("milestone.section.resource"));
        api.AddNumberOption(_manifest,
            getValue: () => M().XpMultiplier, setValue: v => M().XpMultiplier = v,
            name: () => T("milestone.xp-gain.name"),
            tooltip: () => T("milestone.xp-gain.tooltip"),
            min: 0f, max: 5f, interval: 0.01f,
            formatValue: v => v.ToString("0.00"));
        api.AddNumberOption(_manifest,
            getValue: () => M().SellPriceBonus, setValue: v => M().SellPriceBonus = v,
            name: () => T("milestone.sell-price.name"),
            tooltip: () => T("milestone.sell-price.tooltip"),
            min: 0f, max: 5f, interval: 0.01f,
            formatValue: v => v.ToString("0.00"));

        api.AddSectionTitle(_manifest, () => T("milestone.section.gameplay"));
        api.AddNumberOption(_manifest,
            getValue: () => M().ExtraCropChance, setValue: v => M().ExtraCropChance = v,
            name: () => T("milestone.extra-crops.name"),
            tooltip: () => T("milestone.extra-crops.tooltip"),
            min: 0f, max: 2f, interval: 0.05f);
        api.AddNumberOption(_manifest,
            getValue: () => M().ExtraOreChance, setValue: v => M().ExtraOreChance = v,
            name: () => T("milestone.extra-ore.name"),
            tooltip: () => T("milestone.extra-ore.tooltip"),
            min: 0f, max: 2f, interval: 0.05f);
        api.AddNumberOption(_manifest,
            getValue: () => M().MachineSpeedBonus, setValue: v => M().MachineSpeedBonus = v,
            name: () => T("milestone.machine-speed.name"),
            tooltip: () => T("milestone.machine-speed.tooltip"),
            min: 0f, max: 2f, interval: 0.05f);
    }
}
