using System;
using StardewModdingAPI;
using TrainUp.Config;

namespace TrainUp.Integration;

/// <summary>
/// Wires Train Up's config into Generic Mod Config Menu (GMCM). Optional soft dependency: if
/// GMCM isn't installed this is a no-op and the mod still works via config.json. All labels are
/// translated, so the menu is localizable.
/// </summary>
public class GmcmIntegration
{
    private readonly IModHelper _helper;
    private readonly IManifest _manifest;
    private readonly IMonitor _monitor;
    private readonly Func<ModConfig> _getConfig;
    private readonly Action<ModConfig> _setConfig;
    private readonly Action _onSave;

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

    private string T(string key) => _helper.Translation.Get(key);

    public void Register()
    {
        var api = _helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api == null)
        {
            _monitor.Log("GMCM not found — in-game config menu unavailable.", LogLevel.Debug);
            return;
        }

        api.Register(
            mod: _manifest,
            reset: () => _setConfig(new ModConfig()),
            save: _onSave);

        // ── General ─────────────────────────────────────────────────────────
        api.AddSectionTitle(_manifest, () => T("config.section.general"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().Enabled,
            setValue: v => _getConfig().Enabled = v,
            name: () => T("config.enabled.name"),
            tooltip: () => T("config.enabled.tooltip"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().EnableProfessionPerks,
            setValue: v => _getConfig().EnableProfessionPerks = v,
            name: () => T("config.perks.name"),
            tooltip: () => T("config.perks.tooltip"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().DebugLogging,
            setValue: v => _getConfig().DebugLogging = v,
            name: () => T("config.debug.name"),
            tooltip: () => T("config.debug.tooltip"));

        // ── XP rates ────────────────────────────────────────────────────────
        api.AddSectionTitle(_manifest, () => T("config.section.rates"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().DefenseXpPerDamage,
            setValue: v => _getConfig().DefenseXpPerDamage = v,
            name: () => T("config.defense-rate.name"),
            tooltip: () => T("config.defense-rate.tooltip"),
            min: 0f, max: 25f, interval: 0.5f, formatValue: v => v.ToString("0.0"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().VitalityXpPerHpLost,
            setValue: v => _getConfig().VitalityXpPerHpLost = v,
            name: () => T("config.vitality-rate.name"),
            tooltip: () => T("config.vitality-rate.tooltip"),
            min: 0f, max: 25f, interval: 0.5f, formatValue: v => v.ToString("0.0"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().StaminaXpPerEnergy,
            setValue: v => _getConfig().StaminaXpPerEnergy = v,
            name: () => T("config.stamina-rate.name"),
            tooltip: () => T("config.stamina-rate.tooltip"),
            min: 0f, max: 10f, interval: 0.1f, formatValue: v => v.ToString("0.0"));

        // ── Daily caps ──────────────────────────────────────────────────────
        api.AddSectionTitle(_manifest, () => T("config.section.caps"));
        api.AddParagraph(_manifest, () => T("config.caps.paragraph"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().DefenseDailyXpCap,
            setValue: v => _getConfig().DefenseDailyXpCap = v,
            name: () => T("config.defense-cap.name"),
            min: 0, max: 10000, interval: 50);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().VitalityDailyXpCap,
            setValue: v => _getConfig().VitalityDailyXpCap = v,
            name: () => T("config.vitality-cap.name"),
            min: 0, max: 10000, interval: 50);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().StaminaDailyXpCap,
            setValue: v => _getConfig().StaminaDailyXpCap = v,
            name: () => T("config.stamina-cap.name"),
            min: 0, max: 10000, interval: 50);

        // ── Per-level bonuses ───────────────────────────────────────────────
        api.AddSectionTitle(_manifest, () => T("config.section.perlevel"));
        api.AddParagraph(_manifest, () => T("config.perlevel.paragraph"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().VitalityHpPerLevel,
            setValue: v => _getConfig().VitalityHpPerLevel = v,
            name: () => T("config.vitality-perlevel.name"),
            min: 0, max: 25, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().StaminaEnergyPerLevel,
            setValue: v => _getConfig().StaminaEnergyPerLevel = v,
            name: () => T("config.stamina-perlevel.name"),
            min: 0, max: 50, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().DefensePerLevel,
            setValue: v => _getConfig().DefensePerLevel = v,
            name: () => T("config.defense-perlevel.name"),
            min: 0, max: 10, interval: 1);

        // ── Training dummy ──────────────────────────────────────────────────
        api.AddSectionTitle(_manifest, () => T("config.section.dummy"));
        api.AddBoolOption(_manifest,
            getValue: () => _getConfig().DummyEnabled,
            setValue: v => _getConfig().DummyEnabled = v,
            name: () => T("config.dummy-enabled.name"),
            tooltip: () => T("config.dummy-enabled.tooltip"));
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().DummyCombatXpPerHit,
            setValue: v => _getConfig().DummyCombatXpPerHit = v,
            name: () => T("config.dummy-xp.name"),
            tooltip: () => T("config.dummy-xp.tooltip"),
            min: 0, max: 50, interval: 1);
        api.AddNumberOption(_manifest,
            getValue: () => _getConfig().DummyDailyXpCap,
            setValue: v => _getConfig().DummyDailyXpCap = v,
            name: () => T("config.dummy-cap.name"),
            tooltip: () => T("config.dummy-cap.tooltip"),
            min: 0, max: 10000, interval: 50);
    }
}
