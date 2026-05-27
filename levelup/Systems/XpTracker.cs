using System;
using LevelUp.Config;
using LevelUp.State;
using StardewModdingAPI;

namespace LevelUp.Systems;

/// <summary>
/// Centralizes XP awarding. Callers (event handlers, Harmony patches) pass in the amount and a
/// source label; this class adds to the lifetime total, recomputes level, and reports whether
/// the player leveled up so the caller can fire the notifier / reapply bonuses.
/// </summary>
public class XpTracker
{
    private readonly ModConfig _config;
    private readonly SaveDataManager _saveData;
    private readonly LevelCalculator _calculator;
    private readonly IMonitor _monitor;

    /// <summary>Raised whenever XP is actually awarded (amount &gt; 0, mod enabled). Args: (amount, source).</summary>
    public event Action<long, string>? XpAwarded;

    public XpTracker(
        ModConfig config,
        SaveDataManager saveData,
        LevelCalculator calculator,
        IMonitor monitor)
    {
        _config = config;
        _saveData = saveData;
        _calculator = calculator;
        _monitor = monitor;
    }

    /// <summary>
    /// Add XP and trigger a level-up if the threshold was crossed.
    /// Returns true if the player leveled up.
    /// </summary>
    public bool AwardXp(long amount, string source)
    {
        if (!_config.Enabled || amount <= 0) return false;

        long before = _saveData.Current.TotalXp;
        int oldLevel = _saveData.Current.Level;

        _saveData.Current.TotalXp = before + amount;
        int newLevel = _calculator.LevelForTotalXp(_saveData.Current.TotalXp);

        if (_config.DebugLogging)
            _monitor.Log($"+{amount} XP from {source} (total {_saveData.Current.TotalXp})", LogLevel.Debug);

        XpAwarded?.Invoke(amount, source);

        if (newLevel > oldLevel)
        {
            _saveData.Current.Level = newLevel;
            return true;
        }
        return false;
    }
}
