using System.Linq;
using LevelUp.Config;
using StardewModdingAPI;
using StardewValley;

namespace LevelUp.Systems;

/// <summary>
/// Shows feedback when the player crosses a level threshold.
/// </summary>
public class LevelUpNotifier
{
    private readonly ModConfig _config;
    private readonly IMonitor _monitor;

    public LevelUpNotifier(ModConfig config, IMonitor monitor)
    {
        _config = config;
        _monitor = monitor;
    }

    /// <summary>
    /// Called whenever the player's level increased. Shows a HUD message and plays a sound.
    /// If the new level crosses one (or more) enabled milestones, the highest such milestone's
    /// name is used in the toast.
    /// </summary>
    public void NotifyLevelUp(int oldLevel, int newLevel)
    {
        if (!_config.Enabled || newLevel <= oldLevel) return;

        // Find the highest milestone the player crossed on this level-up (if any).
        var crossed = _config.Milestones
            .Where(m => m.Enabled && m.Level > oldLevel && m.Level <= newLevel)
            .OrderByDescending(m => m.Level)
            .FirstOrDefault();

        if (_config.ShowLevelUpNotification)
        {
            string text = crossed != null && !string.IsNullOrWhiteSpace(crossed.Name)
                ? $"Level {newLevel}! You are now a {crossed.Name}."
                : $"Level Up! You are now level {newLevel}.";

            Game1.addHUDMessage(new HUDMessage(text, HUDMessage.achievement_type)
            {
                noIcon = true,
            });
        }

        if (_config.PlayLevelUpSound)
        {
            // "newArtifact" is the vanilla artifact discovery jingle — short and celebratory.
            Game1.playSound(crossed != null ? "yoba" : "newArtifact");
        }

        if (_config.DebugLogging)
            _monitor.Log($"Level-up: {oldLevel} → {newLevel}" + (crossed != null ? $" ({crossed.Name})" : ""), LogLevel.Trace);
    }
}
