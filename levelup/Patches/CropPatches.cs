using System;
using System.Linq;
using HarmonyLib;
using LevelUp.Config;
using LevelUp.Systems;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace LevelUp.Patches;

/// <summary>
/// Harmony postfix on <see cref="Crop.harvest"/>. When the local player successfully harvests
/// a crop, rolls the accumulated <c>ExtraCropChance</c> milestone bonus and, if it hits,
/// hands them an extra copy of the harvested crop. Values above 1.0 grant guaranteed extras
/// plus a roll for one more (1.20 = always +1, plus 20% chance of +2). Junimo harvests are
/// skipped (the bonus is a player reward, and the Junimo bag has its own logic).
/// </summary>
public static class CropPatches
{
    private static ModConfig _config = null!;
    private static BonusApplier _bonusApplier = null!;
    private static IMonitor _monitor = null!;

    public static void Init(ModConfig config, BonusApplier bonusApplier, IMonitor monitor)
    {
        _config = config;
        _bonusApplier = bonusApplier;
        _monitor = monitor;
    }

    public static void Apply(Harmony harmony)
    {
        // Match by name only (not signature) so a 1.6.x parameter tweak doesn't silently leave
        // the postfix unpatched. There's only one Crop.harvest in vanilla.
        var target = AccessTools.Method(typeof(Crop), nameof(Crop.harvest));
        if (target == null)
        {
            _monitor.Log("CropPatches: couldn't resolve Crop.harvest; bonus-crop milestone bonus will not fire.", LogLevel.Warn);
            return;
        }
        string sig = string.Join(", ", target.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        _monitor.Log($"CropPatches: patched {target.DeclaringType?.Name}.{target.Name}({sig})", LogLevel.Info);
        harmony.Patch(target,
            postfix: new HarmonyMethod(typeof(CropPatches), nameof(Harvest_Postfix)));
    }

    public static void Harvest_Postfix(
        Crop __instance,
        int xTile,
        int yTile,
        HoeDirt soil,
        JunimoHarvester junimoHarvester,
        bool __result)
    {
        try
        {
            // Spammy on purpose: log every postfix entry while DebugLogging is on so we can
            // tell at a glance whether the patch is firing, and what state it sees. Easy to
            // dial back to "extras > 0 only" once the bonus is verified working in the wild.
            if (_config.DebugLogging)
                _monitor.Log(
                    $"CropPatches.Harvest_Postfix: result={__result}, junimo={(junimoHarvester != null)}, " +
                    $"chance={_bonusApplier.CurrentExtraCropChance:F2}, itemId={__instance?.indexOfHarvest?.Value ?? "(null)"}",
                    LogLevel.Debug);

            if (!__result) return;
            if (junimoHarvester != null) return; // Junimo huts: don't pile bonus into the Junimo bag.

            float chance = _bonusApplier.CurrentExtraCropChance;
            if (chance <= 0f) return;

            int extras = RollExtras(chance);
            if (extras <= 0) return;

            string itemId = __instance.indexOfHarvest.Value;
            if (string.IsNullOrEmpty(itemId)) return;

            var item = ItemRegistry.Create(itemId, extras);
            if (item == null) return;

            var player = Game1.player;
            if (player == null) return;

            if (_config.DebugLogging)
                _monitor.Log($"+{extras} bonus crop ({itemId}) from harvest at ({xTile},{yTile})", LogLevel.Debug);

            // Try inventory first; if it doesn't all fit, drop the remainder at the tile.
            Item? leftover = player.addItemToInventory(item);
            if (leftover != null && leftover.Stack > 0)
            {
                var location = soil?.Location ?? Game1.currentLocation;
                if (location != null)
                {
                    var pos = new Vector2(xTile * 64f + 32f, yTile * 64f + 32f);
                    Game1.createItemDebris(leftover, pos, -1, location);
                }
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"CropPatches.Harvest_Postfix failed: {ex}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Resolve a fractional bonus chance into a concrete extra count: whole part is guaranteed,
    /// fractional part is rolled. So 0.10 = 10% chance of +1; 1.20 = always +1, plus 20% chance
    /// of +2.
    /// </summary>
    private static int RollExtras(float chance)
    {
        int guaranteed = (int)Math.Floor(chance);
        float roll = chance - guaranteed;
        if (Game1.random.NextDouble() < roll) guaranteed++;
        return guaranteed;
    }
}
