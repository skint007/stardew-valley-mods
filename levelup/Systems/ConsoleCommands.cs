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
