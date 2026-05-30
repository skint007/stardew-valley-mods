using System;
using System.Collections.Generic;
using HarmonyLib;
using LevelUp.Systems;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace LevelUp.Patches;

/// <summary>
/// Harmony pre+postfix on <see cref="GameLocation.OnStoneDestroyed"/>. Snapshots the
/// location's debris count in the prefix, then in the postfix any debris added during the
/// call (the drops from this stone) is rolled against the player's accumulated
/// <c>ExtraOreChance</c>; a hit duplicates the drops at the same tile. Snapshot-and-diff
/// avoids needing a hardcoded stone-id → drop-id map, so it works for every node type
/// (stone, copper / iron / gold / iridium ore, coal, gems, geodes, bones, etc.) automatically.
/// </summary>
public static class StonePatches
{
    private static BonusApplier _bonusApplier = null!;
    private static IMonitor _monitor = null!;

    // SDV game logic is single-threaded, so a plain static is fine. [ThreadStatic] for safety
    // against any odd re-entry from another thread (no-op in practice).
    [ThreadStatic]
    private static int _debrisCountBefore;

    public static void Init(BonusApplier bonusApplier, IMonitor monitor)
    {
        _bonusApplier = bonusApplier;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        var target = AccessTools.Method(typeof(GameLocation), nameof(GameLocation.OnStoneDestroyed));
        if (target == null)
        {
            _monitor.Log("StonePatches: couldn't resolve OnStoneDestroyed; bonus-ore milestone bonus will not fire.", LogLevel.Warn);
            return;
        }
        _monitor.Log($"StonePatches: patched {target.DeclaringType?.Name}.{target.Name}", LogLevel.Info);
        harmony.Patch(target,
            prefix: new HarmonyMethod(typeof(StonePatches), nameof(OnStoneDestroyed_Prefix)),
            postfix: new HarmonyMethod(typeof(StonePatches), nameof(OnStoneDestroyed_Postfix)));
    }

    public static void OnStoneDestroyed_Prefix(GameLocation __instance)
    {
        try
        {
            _debrisCountBefore = __instance?.debris?.Count ?? 0;
        }
        catch { _debrisCountBefore = 0; }
    }

    public static void OnStoneDestroyed_Postfix(GameLocation __instance, int x, int y, Farmer who)
    {
        try
        {
            // Only credit the bonus to the local player's own strikes (multiplayer-safe: every
            // machine runs this postfix, but only the actor's machine gets the duplication).
            if (who == null || who != Game1.player) return;

            float chance = _bonusApplier.CurrentExtraOreChance;
            if (chance <= 0f) return;
            if (__instance?.debris == null) return;

            int extras = RollExtras(chance);
            if (extras <= 0) return;

            // Collect items added during this call (the drops from this stone). Iterate rather
            // than indexer-slice in case NetCollection<Debris> doesn't expose a stable indexer.
            var newDrops = new List<Item>();
            int seen = 0;
            int skipUpTo = Math.Min(_debrisCountBefore, __instance.debris.Count);
            foreach (var d in __instance.debris)
            {
                if (seen++ < skipUpTo) continue;
                if (d?.item != null)
                    newDrops.Add(d.item.getOne());
            }
            if (newDrops.Count == 0) return;

            var pos = new Vector2(x * 64f + 32f, y * 64f + 32f);
            for (int copy = 0; copy < extras; copy++)
                foreach (var drop in newDrops)
                    Game1.createItemDebris(drop.getOne(), pos, -1, __instance);
        }
        catch (Exception ex)
        {
            _monitor.Log($"StonePatches.OnStoneDestroyed_Postfix failed: {ex}", LogLevel.Error);
        }
    }

    private static int RollExtras(float chance)
    {
        int guaranteed = (int)Math.Floor(chance);
        float roll = chance - guaranteed;
        if (Game1.random.NextDouble() < roll) guaranteed++;
        return guaranteed;
    }
}
