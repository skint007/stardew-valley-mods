using HarmonyLib;
using LevelUp.Config;
using LevelUp.Integration;
using LevelUp.Patches;
using LevelUp.State;
using LevelUp.Systems;
using LevelUp.Ui;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LevelUp;

/// <summary>
/// SMAPI entry point. Wires up the event handlers and orchestrates the systems.
/// </summary>
public class ModEntry : Mod
{
    private ModConfig _config = null!;
    private SaveDataManager _saveData = null!;
    private LevelCalculator _calculator = null!;
    private XpTracker _xpTracker = null!;
    private BonusApplier _bonusApplier = null!;
    private LevelUpNotifier _notifier = null!;
    private XpBarHud _xpBar = null!;
    private ConsoleCommands _consoleCommands = null!;
    private GmcmIntegration? _gmcm;

    private bool _attendedFestivalToday;

    public override void Entry(IModHelper helper)
    {
        // Load config and normalize milestone slot count (in case a user edited config.json).
        _config = helper.ReadConfig<ModConfig>();
        _config.NormalizeMilestoneSlots();
        helper.WriteConfig(_config);

        // Construct systems.
        _saveData = new SaveDataManager(Monitor);
        _calculator = new LevelCalculator(_config.Curve, _config.LevelCap);
        _xpTracker = new XpTracker(_config, _saveData, _calculator, Monitor);
        _bonusApplier = new BonusApplier(_config, _saveData, helper.Translation, Monitor);
        _notifier = new LevelUpNotifier(_config, helper.Translation, Monitor);
        _xpBar = new XpBarHud(_config, _saveData, _calculator, helper.Translation, Monitor);
        _xpTracker.XpAwarded += _xpBar.RegisterGain;
        _consoleCommands = new ConsoleCommands(_config, _saveData, _calculator, _xpTracker, _bonusApplier, _notifier, Monitor);
        _consoleCommands.Register(helper.ConsoleCommands);

        // Harmony patches.
        var harmony = new Harmony(ModManifest.UniqueID);
        MonsterPatches.Init(_config, _saveData, _xpTracker, _notifier, _bonusApplier, Monitor);
        QuestPatches.Init(_config, _saveData, _xpTracker, _notifier, _bonusApplier, Monitor);
        ObjectPatches.Init(_bonusApplier, Monitor);
        FarmerPatches.Init(_config, _saveData, _xpTracker, _notifier, _bonusApplier, Monitor);
        CropPatches.Init(_config, _bonusApplier, Monitor);
        StonePatches.Init(_bonusApplier, Monitor);
        MachinePatches.Init(_bonusApplier, Monitor);
        MonsterPatches.Apply(harmony);
        QuestPatches.Apply(harmony);
        ObjectPatches.Apply(harmony);
        FarmerPatches.Apply(harmony);
        CropPatches.Apply(harmony);
        StonePatches.Apply(harmony);
        MachinePatches.Apply(harmony);

        // Wire SMAPI events.
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.Saved += OnSaved;
        helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdate;
        helper.Events.Display.RenderingHud += OnRenderingHud;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.Player.LevelChanged += OnLevelChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        Monitor.Log("Level Up loaded.", LogLevel.Info);
    }

    // ── Event handlers ──────────────────────────────────────────────────────

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _gmcm = new GmcmIntegration(
            helper: Helper,
            manifest: ModManifest,
            monitor: Monitor,
            getConfig: () => _config,
            setConfig: newConfig =>
            {
                _config = newConfig;
                _config.NormalizeMilestoneSlots();
            },
            onSave: () =>
            {
                // If a milestone preset was picked, apply it then revert the selector so the
                // resolved slots (now editable) are what gets persisted.
                var preset = Config.MilestonePresets.Build(_config.ApplyMilestonePreset);
                if (preset != null)
                {
                    _config.Milestones = preset;
                    _config.NormalizeMilestoneSlots();
                }
                _config.ApplyMilestonePreset = Config.MilestonePresets.KeepCurrent;

                Helper.WriteConfig(_config);

                // Reconfigure the shared calculator in place (not a new instance) so XpTracker,
                // the HUD, and console commands all pick up the new curve/cap instead of holding
                // a stale one.
                _calculator.Reconfigure(_config.Curve, _config.LevelCap);

                // Re-derive level from lifetime XP under the (possibly changed) curve/cap right
                // away, so it's consistent immediately instead of lurching on the next XP gain.
                if (Context.IsWorldReady)
                    _saveData.Current.Level = _calculator.LevelForTotalXp(_saveData.Current.TotalXp);

                _bonusApplier.ApplyAll();
            });
        _gmcm.Register();
    }

    private void OnOneSecondUpdate(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        _bonusApplier.TickRegen();
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        // Only run from "free" contexts: no menu already active (the activeClickableMenu null
        // check also covers text-entry / chat-box overlays) and no event/cutscene.
        if (Game1.activeClickableMenu != null) return;
        if (Game1.eventUp || Game1.farmEvent != null) return;

        if (_config.OpenMenuHotkey != SButton.None && e.Button == _config.OpenMenuHotkey)
        {
            if (_gmcm == null) return;
            _gmcm.OpenMenu();
            Helper.Input.Suppress(e.Button);
            return;
        }

        if (_config.ShowXpBarHotkey != SButton.None && e.Button == _config.ShowXpBarHotkey)
        {
            _config.ShowXpBar = !_config.ShowXpBar;
            Helper.WriteConfig(_config);
            Helper.Input.Suppress(e.Button);
            return;
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        // Wipe in-memory progress so the next save load starts from a clean slate. Without
        // this, if the player goes title -> new save without restarting the game, the old
        // save's data sits in _saveData.Current and any pre-SaveLoaded write path leaks it
        // into the new Farmer's modData.
        _saveData.Clear();
        _attendedFestivalToday = false;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _saveData.Load();
        // First-run: capture vanilla baselines so we don't compound HP/energy bonuses on subsequent loads.
        // If the current values look like a stale bonus-inflated number, prefer the lower of {existing, vanilla floor}.
        if (_saveData.Current.BaselineMaxHp == 0)
            _saveData.Current.BaselineMaxHp = Game1.player.maxHealth;
        if (_saveData.Current.BaselineMaxEnergy == 0)
            _saveData.Current.BaselineMaxEnergy = Game1.player.maxStamina.Value;

        // Recompute level in case curve config changed since last save.
        int recomputed = _calculator.LevelForTotalXp(_saveData.Current.TotalXp);
        if (recomputed != _saveData.Current.Level)
            _saveData.Current.Level = recomputed;

        _attendedFestivalToday = false;
        _bonusApplier.ApplyAll();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        _attendedFestivalToday = false;
        _bonusApplier.ApplyAll();

        // Vanilla heals the player to maxHealth / MaxStamina before SMAPI's DayStarted fires,
        // using the pre-bonus values. Top them off so they wake up at the (new) full.
        if (Context.IsWorldReady && _config.Enabled)
        {
            Game1.player.health = Game1.player.maxHealth;
            Game1.player.stamina = Game1.player.MaxStamina;
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!_config.Enabled) return;
        // Runs for every player (host + farmhands); each tracks their own progress.

        var src = _config.XpSources;

        // Day survived.
        if (src.DaySurvivedEnabled)
        {
            bool passedOut = Game1.player.passedOut || Game1.player.stamina <= 0f;
            if (!(src.DaySurvivedSkipOnPassout && passedOut))
            {
                int oldLevel = _saveData.Current.Level;
                bool leveled = _xpTracker.AwardXp(src.DaySurvivedXp, "day-survived");
                if (leveled)
                {
                    _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                    _bonusApplier.ApplyAll();
                }
            }
        }

        // Festival attendance (one award per festival day).
        if (src.FestivalEnabled && _attendedFestivalToday)
        {
            int oldLevel = _saveData.Current.Level;
            bool leveled = _xpTracker.AwardXp(src.FestivalXp, "festival");
            if (leveled)
            {
                _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                _bonusApplier.ApplyAll();
            }
        }

        // Flush progress to modData and strip stat mutations before the host serializes
        // every farmer for the overnight save. DayEnding fires for host *and* farmhands
        // (Saving/Saved only fire on the host), so this is what keeps farmhand progress
        // and keeps inflated maxHealth/maxStamina out of the save. Re-applied on DayStarted.
        _saveData.Save();
        _bonusApplier.Strip();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        // Host-only redundancy on top of the DayEnding flush/strip above.
        _saveData.Save();
        _bonusApplier.Strip();
    }

    private void OnSaved(object? sender, SavedEventArgs e)
    {
        _bonusApplier.ApplyAll();
    }

    private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
        // Draw before the vanilla HUD so the toolbar's item tooltip renders on top of us.
        _xpBar.Draw();
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        // Draw our own hover tooltip after the HUD so it sits above the toolbar.
        _xpBar.DrawTooltip();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!_config.Enabled) return;
        if (e.Player != Game1.player) return; // only the local player's own warps

        // Festival detection: any time the player is at a festival location today, mark it.
        if (Game1.CurrentEvent != null && Game1.CurrentEvent.isFestival)
            _attendedFestivalToday = true;

        // New area XP.
        var src = _config.XpSources;
        if (!src.NewAreaEnabled) return;
        if (e.NewLocation == null) return;

        string name = e.NewLocation.NameOrUniqueName;
        if (string.IsNullOrEmpty(name)) return;

        if (_saveData.Current.AreasAwardedXpFor.Add(name))
        {
            int oldLevel = _saveData.Current.Level;
            bool leveled = _xpTracker.AwardXp(src.NewAreaXp, $"new-area:{name}");
            if (leveled)
            {
                _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                _bonusApplier.ApplyAll();
            }
        }
    }

    private void OnLevelChanged(object? sender, LevelChangedEventArgs e)
    {
        if (!_config.Enabled) return;
        if (e.Player != Game1.player) return; // per-player: only the local farmer

        var src = _config.XpSources;
        if (!src.SkillLevelUpEnabled) return;

        int gained = e.NewLevel - e.OldLevel;
        if (gained <= 0) return; // ignore level decreases (e.g. debuffs)

        int oldLevel = _saveData.Current.Level;
        bool leveled = _xpTracker.AwardXp((long)src.SkillLevelUpXp * gained, $"skill-levelup:{e.Skill}");
        if (leveled)
        {
            _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
            _bonusApplier.ApplyAll();
        }
    }
}
