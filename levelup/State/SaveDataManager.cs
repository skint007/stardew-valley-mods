using System;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;

namespace LevelUp.State;

/// <summary>
/// Reads/writes <see cref="PlayerLevelData"/> on the local player's <see cref="Farmer.modData"/>.
///
/// modData (rather than SMAPI's Helper.Data save store) is used so the mod works in
/// multiplayer: it is per-player, network-synced, and persisted with each character —
/// SMAPI's ReadSaveData/WriteSaveData only works for the host. Each machine therefore
/// tracks its own <see cref="Game1.player"/> independently; no cross-player messaging
/// is needed for the per-player level model.
/// </summary>
public class SaveDataManager
{
    /// <summary>modData key (namespaced with the mod's UniqueID, per SMAPI convention).</summary>
    public const string ModDataKey = "skint007.LevelUp/player-data";

    private readonly IMonitor _monitor;

    public PlayerLevelData Current { get; private set; } = new();

    public SaveDataManager(IMonitor monitor)
    {
        _monitor = monitor;
    }

    /// <summary>Load the local player's data from modData, or defaults if none yet.</summary>
    public void Load()
    {
        Current = new PlayerLevelData();

        var player = Game1.player;
        if (player == null) return;

        if (player.modData.TryGetValue(ModDataKey, out string json) && !string.IsNullOrWhiteSpace(json))
        {
            try
            {
                Current = JsonSerializer.Deserialize<PlayerLevelData>(json) ?? new PlayerLevelData();
            }
            catch (Exception ex)
            {
                _monitor.Log($"Couldn't parse saved Level Up data; starting fresh. ({ex.Message})", LogLevel.Warn);
                Current = new PlayerLevelData();
            }
        }

        _monitor.Log(
            $"Loaded player data: level {Current.Level}, totalXp {Current.TotalXp}",
            LogLevel.Trace);
    }

    /// <summary>Persist the current data onto the local player's modData.</summary>
    public void Save()
    {
        // Only write when a save is fully loaded. Without this gate, an XP-award path that
        // happens to fire during the new-game / title-screen window (intro skill-XP, a
        // pre-SaveLoaded warp, etc.) would flush the previous save's in-memory state onto
        // the fresh Farmer's modData and "inherit" the old level into the new save.
        if (!Context.IsWorldReady) return;

        var player = Game1.player;
        if (player == null) return;

        player.modData[ModDataKey] = JsonSerializer.Serialize(Current);
    }

    /// <summary>
    /// Wipe the in-memory <see cref="Current"/> data without touching modData. Call when the
    /// player returns to title so a subsequent new-game / save-load starts from a clean slate
    /// even if anything writes before the next <see cref="Load"/>.
    /// </summary>
    public void Clear()
    {
        Current = new PlayerLevelData();
    }

    /// <summary>Reset XP and level back to defaults (keeps baselines).</summary>
    public void ResetProgress()
    {
        Current.TotalXp = 0;
        Current.Level = 1;
        Current.AreasAwardedXpFor.Clear();
        Save();
    }
}
