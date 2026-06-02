using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using TrainUp.Config;
using TrainUp.Integration;
using TrainUp.Patches;
using TrainUp.Systems;

namespace TrainUp;

/// <summary>
/// SMAPI entry point for Train Up. Registers the custom skills with SpaceCore, wires the XP
/// hooks (Defense via a takeDamage patch; Vitality/Stamina via per-second polling), the training
/// dummy, and the profession perks (save-safe stat buffs + Harmony-patched effects).
/// </summary>
public class ModEntry : Mod
{
    /// <summary>Singleton handle so skill/profession classes can reach the helper and translations.</summary>
    public static ModEntry Instance { get; private set; } = null!;

    private ModConfig _config = null!;
    private SkillRegistry _skills = null!;
    private XpAwarder _xp = null!;
    private VitalsTracker _vitals = null!;
    private BonusApplier _bonuses = null!;
    private DummyContent _dummy = null!;
    private ConsoleCommands _commands = null!;
    private GmcmIntegration? _gmcm;

    public ModConfig Config => _config;

    public override void Entry(IModHelper helper)
    {
        Instance = this;

        _config = helper.ReadConfig<ModConfig>();

        _skills = new SkillRegistry(Monitor);
        // Register skills in Entry (not GameLaunched): SpaceCore loads before us, so its own
        // GameLaunched — where it wires up the skills page — runs before ours would. Registering
        // here ensures our skills exist before SpaceCore sets that up. (SpaceCore is a required
        // dependency, so its assembly is already loaded.)
        _skills.Register();

        _xp = new XpAwarder(_config, _skills, Monitor);
        _vitals = new VitalsTracker(_config, _xp);
        _bonuses = new BonusApplier(_config, _skills, Monitor);
        Perks.Init(_config, _skills);

        _dummy = new DummyContent(helper);
        _dummy.RegisterEvents();
        _commands = new ConsoleCommands(_skills, Monitor);
        _commands.Register(helper.ConsoleCommands);

        // Harmony patches.
        var harmony = new Harmony(ModManifest.UniqueID);
        FarmerPatches.Init(_xp, Monitor);
        CombatPatches.Init(Monitor);
        ConsumptionPatches.Init(Monitor);
        StaminaPatches.Init(Monitor);
        DummyHitPatches.Init(_config, _xp, Monitor);
        FarmerPatches.Apply(harmony);
        CombatPatches.Apply(harmony);
        ConsumptionPatches.Apply(harmony);
        StaminaPatches.Apply(harmony);
        DummyHitPatches.Apply(harmony);

        // SMAPI events.
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.Saved += OnSaved;
        helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdate;

        Monitor.Log("Train Up loaded.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _gmcm = new GmcmIntegration(
            helper: Helper,
            manifest: ModManifest,
            monitor: Monitor,
            getConfig: () => _config,
            setConfig: newConfig => _config.CopyFrom(newConfig), // copy in place; systems share this instance
            onSave: () => Helper.WriteConfig(_config));
        _gmcm.Register();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _vitals.Prime();
        _xp.ResetDaily();
        _bonuses.Apply();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // Vanilla refills HP/energy before this fires; re-baseline so the refill isn't read as a change.
        _vitals.Prime();
        _xp.ResetDaily();
        _bonuses.Apply();
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        // Strip bonuses before the overnight save serializes the farmer (host + farmhands).
        _bonuses.Strip();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        _bonuses.Strip();
    }

    private void OnSaved(object? sender, SavedEventArgs e)
    {
        _bonuses.Apply();
    }

    private void OnOneSecondUpdate(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        _vitals.Tick();
        _bonuses.RefreshIfProfessionsChanged();
        _bonuses.TickRegen();
    }
}
