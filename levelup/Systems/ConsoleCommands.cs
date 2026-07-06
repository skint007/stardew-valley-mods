using System.Linq;
using LevelUp.Config;
using LevelUp.State;
using StardewModdingAPI;

namespace LevelUp.Systems;

/// <summary>
/// SMAPI console commands for inspecting / poking the level system. Useful both for player
/// debugging and for verifying the mod works without grinding XP. Registered in
/// <see cref="ModEntry"/> at startup.
/// </summary>
public class ConsoleCommands
{
    private readonly ModConfig _config;
    private readonly SaveDataManager _saveData;
    private readonly LevelCalculator _calculator;
    private readonly XpTracker _xpTracker;
    private readonly BonusApplier _bonusApplier;
    private readonly LevelUpNotifier _notifier;
    private readonly IMonitor _monitor;

    public ConsoleCommands(
        ModConfig config,
        SaveDataManager saveData,
        LevelCalculator calculator,
        XpTracker xpTracker,
        BonusApplier bonusApplier,
        LevelUpNotifier notifier,
        IMonitor monitor)
    {
        _config = config;
        _saveData = saveData;
        _calculator = calculator;
        _xpTracker = xpTracker;
        _bonusApplier = bonusApplier;
        _notifier = notifier;
        _monitor = monitor;
    }

    public void Register(ICommandHelper commands)
    {
        commands.Add("levelup_show",
            "Show your current XP, level, and active milestones.",
            (_, _) => Show());

        commands.Add("levelup_addxp",
            "Usage: levelup_addxp <amount>\nAward XP. Negative amounts allowed (won't drop below 0).",
            (_, args) => AddXp(args));

        commands.Add("levelup_setlevel",
            "Usage: levelup_setlevel <level>\nJump straight to a level (sets TotalXp to that level's threshold).",
            (_, args) => SetLevel(args));

        commands.Add("levelup_reset",
            "Reset XP and level back to 1.",
            (_, _) => Reset());

        commands.Add("levelup_setbaseline",
            "Usage: levelup_setbaseline <maxHp> <maxEnergy>\n"
            + "Manually set the stored vanilla baselines. Use this to recover if a "
            + "pre-1.3.7 install inflated your max HP / energy. Vanilla defaults are "
            + "100 HP and 270 energy; add 34 energy per Stardrop eaten and 25 HP for "
            + "the Combat Mastery cave.",
            (_, args) => SetBaseline(args));
    }

    private void Show()
    {
        if (!RequireWorld()) return;

        long total = _saveData.Current.TotalXp;
        int level = _saveData.Current.Level;
        long into = _calculator.XpIntoCurrentLevel(total, level);
        long needed = _calculator.XpToNextLevel(level);
        string nextLine = level >= _calculator.LevelCap
            ? "(at level cap)"
            : $"{into:N0} / {needed:N0} into next";

        _monitor.Log($"Level {level}  —  TotalXp {total:N0}  —  {nextLine}", LogLevel.Info);

        var unlocked = _config.Milestones
            .Where(m => m.Enabled && m.Level <= level)
            .OrderBy(m => m.Level)
            .ToList();
        if (unlocked.Count == 0)
        {
            _monitor.Log("No milestones unlocked yet.", LogLevel.Info);
        }
        else
        {
            _monitor.Log($"Unlocked milestones ({unlocked.Count}):", LogLevel.Info);
            foreach (var m in unlocked)
                _monitor.Log($"  Lv {m.Level} — {m.Name}", LogLevel.Info);
        }
    }

    private void AddXp(string[] args)
    {
        if (!RequireWorld()) return;
        if (args.Length < 1 || !long.TryParse(args[0], out long amount))
        {
            _monitor.Log("Usage: levelup_addxp <amount>", LogLevel.Error);
            return;
        }

        if (amount >= 0)
        {
            int oldLevel = _saveData.Current.Level;
            bool leveled = _xpTracker.AwardXp(amount, "console");
            if (leveled)
            {
                _notifier.NotifyLevelUp(oldLevel, _saveData.Current.Level);
                _bonusApplier.ApplyAll();
            }
        }
        else
        {
            // Negative: subtract directly without going below 0; recompute level.
            long newTotal = System.Math.Max(0, _saveData.Current.TotalXp + amount);
            _saveData.Current.TotalXp = newTotal;
            _saveData.Current.Level = _calculator.LevelForTotalXp(newTotal);
            _bonusApplier.ApplyAll();
        }
        Show();
    }

    private void SetLevel(string[] args)
    {
        if (!RequireWorld()) return;
        if (args.Length < 1 || !int.TryParse(args[0], out int target))
        {
            _monitor.Log("Usage: levelup_setlevel <level>", LogLevel.Error);
            return;
        }

        target = System.Math.Clamp(target, 1, _calculator.LevelCap);
        _saveData.Current.TotalXp = _calculator.CumulativeXpForLevel(target);
        _saveData.Current.Level = target;
        _bonusApplier.ApplyAll();
        Show();
    }

    private void Reset()
    {
        if (!RequireWorld()) return;
        _saveData.ResetProgress();
        _bonusApplier.ApplyAll();
        _monitor.Log("Level Up: progress reset to level 1.", LogLevel.Info);
    }

    private void SetBaseline(string[] args)
    {
        if (!RequireWorld()) return;
        if (args.Length < 2 || !int.TryParse(args[0], out int hp) || !int.TryParse(args[1], out int energy))
        {
            _monitor.Log("Usage: levelup_setbaseline <maxHp> <maxEnergy>", LogLevel.Error);
            return;
        }
        if (hp < 1 || energy < 1)
        {
            _monitor.Log("Baseline values must be positive.", LogLevel.Error);
            return;
        }

        // Zero LastApplied too, so the next ApplyAll doesn't misread the new baseline as
        // an old-bonus offset and either wipe the vanilla max or refuse to grow it.
        _saveData.Current.BaselineMaxHp = hp;
        _saveData.Current.BaselineMaxEnergy = energy;
        _saveData.Current.LastAppliedMaxHpBonus = 0;
        _saveData.Current.LastAppliedMaxEnergyBonus = 0;

        // Load-bearing: write the new base BEFORE ApplyAll. ApplyAll's first step is
        // AbsorbVanillaIncreases, which reads the base and compares against
        // BaselineMax* + LastApplied*. If we left the inflated base in place, absorb
        // would see delta = inflatedBase - newBaseline and ratchet BaselineMax* right
        // back up, re-inflating the very state this command exists to undo (#20).
        // ResetToBaseline (which runs after Absorb) then no-ops on these writes since
        // base already equals the baseline, and the bonus is added cleanly on top.
        var player = StardewValley.Game1.player;
        if (player != null)
        {
            player.maxHealth = hp;
            player.maxStamina.Value = energy;
        }

        _saveData.Save();
        _bonusApplier.ApplyAll();

        // Clamp current health / stamina to the new max. Recovering users typically
        // sit near the old inflated max (e.g. stamina 500/500); without this, the HUD
        // shows current > max (450/270) until vanilla re-clamps on damage or the next
        // OnDayStarted top-up.
        if (player != null)
        {
            player.health = System.Math.Min(player.health, player.maxHealth);
            if (player.Stamina > player.MaxStamina)
                player.Stamina = player.MaxStamina;
        }

        _monitor.Log($"Level Up: baselines set to {hp} HP / {energy} energy.", LogLevel.Info);
    }

    private bool RequireWorld()
    {
        if (!Context.IsWorldReady)
        {
            _monitor.Log("Load a save first.", LogLevel.Error);
            return false;
        }
        return true;
    }
}
